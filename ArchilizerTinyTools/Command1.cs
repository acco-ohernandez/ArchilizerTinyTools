#region Namespaces
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

using ArchilizerTinyTools.Forms;

using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

using Forms = System.Windows.Forms;

#endregion

namespace ArchilizerTinyTools
{
    [Transaction(TransactionMode.Manual)]
    public class Command1 : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            // this is a variable for the Revit application
            UIApplication uiapp = commandData.Application;

            // this is a variable for the current Revit model
            Document doc = uiapp.ActiveUIDocument.Document;

            #region FocusedCode
            try
            {
                // Collect all views in the document
                var views = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Views).Cast<View>().OrderBy(x => x.ViewType).ToList();

                // Allow user to dynamically sellect views from a list 
                List<View> dynamicViewsList = new List<View>();

                // dynamicViewsList = GetSelectedViewsList(doc); // C Sharp Form testing

                // Collect all title blocks that are element types
                var titleBlocksCollector = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_TitleBlocks).WhereElementIsElementType().Cast<FamilySymbol>().ToList();

                ////Find the "30x42" title block by its name
                //var titleBlock = titleBlocksCollector.Where(t => t.Name == "30x42").First().Id;

                FilterRule rule = ParameterFilterRuleFactory.CreateEqualsRule(new ElementId((int)BuiltInParameter.SYMBOL_FAMILY_NAME_PARAM), "Viewport", false);
                ElementParameterFilter filter = new ElementParameterFilter(rule);
                IEnumerable<Element> viewPortFamilyTypes = new FilteredElementCollector(doc)
                                    .WhereElementIsElementType()
                                    .WherePasses(filter);

                // Selections Form
                var viewsForm = new ViewsToSheets_Form(views, titleBlocksCollector);

                viewsForm.dgViews.ItemsSource = views.Cast<View>().Select(view => view.Name).ToList();
                viewsForm.dgTitleBlocks.ItemsSource = titleBlocksCollector.Cast<FamilySymbol>().OrderBy(x => x.Name).Select(tblock => tblock.Name).ToList();
                viewsForm.dgTitleText.ItemsSource = viewPortFamilyTypes.Cast<Element>().OrderBy(x => x.Name).Select(vpt => vpt.Name).ToList();
                // Show the form
                viewsForm.ShowDialog();

                //var sheetsCreated = new List<ViewSheet>();
                var sheetsCreated = new Dictionary<ViewSheet, List<Viewport>>();

                // Check if the user doesn't click OK
                if (viewsForm.DialogResult != true)
                    return Result.Cancelled;

                // Get the selected views
                List<View> selectedViews = viewsForm.SelectedViews;

                // Get the selected title block
                FamilySymbol selectedTitleBlock = viewsForm.SelectedTitleBlock;

                // Get the selected title text
                var selectedTitleText = viewsForm.SelectedTitleText;
                var elem = viewPortFamilyTypes.Cast<Element>().First(i => i.Name == selectedTitleText);


                double txt_X = ParseTxtToDouble(viewsForm.txt_X.Text);
                double txt_Y = ParseTxtToDouble(viewsForm.txt_Y.Text);

                // Location to place the view port
                var xyzPoint = new XYZ(txt_X, txt_Y, 0);

                // This bool "oneToOne" will determine if one sheet should be created per each curView
                // if set to false, all selected views are going to be put into one sheet.
                var oneToOne = viewsForm.OneOrManySheets();
                var MultipleViewsSheetName = viewsForm.GetTheMultipleViewsSheetName();

                // If the user clicked OK, proceed with creating sheets and viewports
                using (Transaction t = new Transaction(doc))
                {
                    t.Start("Create Sheets and Viewports");
                    //sheetsCreated = CreateSheetsFromViews(doc, selectedViews, titleBlock, xyzPoint, oneToOne);
                    //sheetsCreated = CreateSheetsFromViews(doc, selectedViews, selectedTitleBlock.Id, xyzPoint, oneToOne, MultipleViewsSheetName, elem.Id);
                    sheetsCreated = CreateSheetsFromViews(doc, selectedViews, selectedTitleBlock, xyzPoint, oneToOne, MultipleViewsSheetName, elem.Id);
                    t.Commit();
                }

                //place a the new Viewports on each Sheet at a specific position relative to the title block's bounding box
                //RelocateViewportsOnSheets(doc, sheetsCreated, xyzPoint); // Method not implemented

                // Display the sheet names
                //TaskDialog.Show("Info", $"View Sheets Created: {sheetsCreated.Count()}\n" +
                //                        $"{string.Join("\n", sheetsCreated.Select(s => s.Name))}");
                TaskDialog.Show("Info", $"View Sheets Created: {sheetsCreated.Count()}");
                #endregion

            }
            catch (Exception e) { TaskDialog.Show("Error", $"Error: ==> {e.Message}"); }

            return Result.Succeeded;
        }
        Dictionary<ViewSheet, List<Viewport>> CreateSheetsFromViews(Document doc, List<View> viewList, FamilySymbol titleBlock, XYZ xyzPoint, bool oneToOne, string multipleViewsSheetName, ElementId textTypeId)
        {
            // Check if a new location is set
            bool newLocationSet = xyzPoint != XYZ.Zero;
            XYZ xyzInchesPoint = newLocationSet ? xyzPoint / 12 : null;

            // Dictionary to store created ViewSheets and associated Viewports
            var viewSheetCreated = new Dictionary<ViewSheet, List<Viewport>>();

            // Create sheets based on the specified mode (one-to-one or one sheet for all views)
            if (oneToOne)
            {
                foreach (var curView in viewList)
                {
                    try
                    {
                        // Temporary list for viewports to be returned
                        var viewPortsCreated = new List<Viewport>();

                        // Create a new sheet
                        var newViewSheet = ViewSheet.Create(doc, titleBlock.Id);
                        newViewSheet.Name = curView.Name;

                        // Get the center of the title block's bounding box
                        XYZ titleBlockCenter = GetCenterOfBoundingBox(titleBlock.get_BoundingBox(newViewSheet));

                        // Adjust the title block center if a new location is set
                        if (newLocationSet)
                            titleBlockCenter += xyzInchesPoint;

                        // Create a new viewport on the new sheet and place the curView
                        var newViewPort = Viewport.Create(doc, newViewSheet.Id, curView.Id, titleBlockCenter);
                        if (newViewPort != null)
                        {
                            // If the viewPort is not null, change its type to the specified text type
                            newViewPort.ChangeTypeId(textTypeId);
                            viewPortsCreated.Add(newViewPort);
                        }

                        // Add the new sheet and associated viewports to the dictionary
                        viewSheetCreated.Add(newViewSheet, viewPortsCreated);
                    }
                    catch (Exception e)
                    {
                        Debug.Print($"Error: ==> {curView.Name} {curView.Id} \n{e.Message}");
                    }
                }
            }
            else // Create a single View Sheet for all views
            {
                var newViewSheet = ViewSheet.Create(doc, titleBlock.Id);
                newViewSheet.Name = multipleViewsSheetName;

                // Get the center of the title block's bounding box
                XYZ titleBlockCenter = GetCenterOfBoundingBox(titleBlock.get_BoundingBox(newViewSheet));

                // Adjust the title block center if a new location is set
                if (newLocationSet)
                    titleBlockCenter += xyzInchesPoint;

                // Temporary list for viewports to be returned
                var viewPortsCreated = new List<Viewport>();

                foreach (var curView in viewList)
                {
                    try
                    {
                        // Create a new viewport on the new sheet for each view
                        var newViewPort = Viewport.Create(doc, newViewSheet.Id, curView.Id, titleBlockCenter);
                        if (newViewPort != null)
                        {
                            // If the viewPort is not null, change its type to the specified text type
                            newViewPort.ChangeTypeId(textTypeId);
                            viewPortsCreated.Add(newViewPort);
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.Print($"Error: ==> {curView.Name} {curView.Id} \n{e.Message}");
                    }
                }

                // Add the new sheet and associated viewports to the dictionary
                viewSheetCreated.Add(newViewSheet, viewPortsCreated);
            }

            return viewSheetCreated;
        }

        // Helper method to get the center of a bounding box
        XYZ GetCenterOfBoundingBox(BoundingBoxXYZ boundingBox)
        {
            return 0.5 * (boundingBox.Min + boundingBox.Max);
        }


        Dictionary<ViewSheet, List<Viewport>> CreateSheetsFromViews3(Document doc, List<View> viewList, FamilySymbol titleBlock, XYZ xyzPoint, bool oneToOne, string multipleViewsSheetName, ElementId textTypeId)
        {
            bool newLocationSet = false;
            XYZ xyzInchesPoint = null;
            if (xyzPoint != new XYZ(0.0, 0.0, 0.0))
            {
                newLocationSet = true;
                xyzInchesPoint = xyzPoint / 12;
            }

            //var viewSheetCreated = new List<ViewSheet>();
            var viewSheetCreated = new Dictionary<ViewSheet, List<Viewport>>();
            if (oneToOne) // Create a one sheet for each view
            {
                foreach (var curView in viewList)
                {
                    try
                    {
                        // Temporary list for viewports to be retruned
                        var viewPortsCreated = new List<Viewport>();

                        // Create a new sheet
                        var newViewSheet = ViewSheet.Create(doc, titleBlock.Id);
                        newViewSheet.Name = curView.Name;
                        //viewSheetCreated.Add(newViewSheet);

                        XYZ titleBlockCenter = GetCenterOfBoundingBox(titleBlock.get_BoundingBox(newViewSheet));
                        // if new location has been enter add it to the titleBlockCenter
                        if (newLocationSet)
                            titleBlockCenter = titleBlockCenter + xyzInchesPoint;

                        // Create a new viewport on the new sheet and place the curView
                        //var newViewPort = Viewport.Create(doc, newViewSheet.Id, curView.Id, xyzPoint);
                        var newViewPort = Viewport.Create(doc, newViewSheet.Id, curView.Id, titleBlockCenter);
                        List<ElementId> newElemId = new List<ElementId>() { textTypeId };
                        if (newViewPort != null) // If the viewPort is null, doesnt have any drawings, don't attempt to add a family type
                        {
                            newViewPort.ChangeTypeId(textTypeId);
                            viewPortsCreated.Add(newViewPort);
                        }

                        viewSheetCreated.Add(newViewSheet, viewPortsCreated);

                    }
                    catch (Exception e)
                    {
                        Debug.Print($"Error: ==> {curView.Name} {curView.Id} \n{e.Message}");
                    }

                }
            }
            else
            {
                // Create a single View Sheet for all views
                var newViewSheet = ViewSheet.Create(doc, titleBlock.Id);
                newViewSheet.Name = multipleViewsSheetName; // You can set any desired name

                XYZ titleBlockCenter = GetCenterOfBoundingBox(titleBlock.get_BoundingBox(newViewSheet));
                if (newLocationSet)
                    titleBlockCenter = titleBlockCenter + xyzInchesPoint;

                // Temporary list for viewports to be retruned
                var viewPortsCreated = new List<Viewport>();

                foreach (var curView in viewList)
                {
                    try
                    {

                        // Create a new viewport on the new sheet for each view
                        //var newViewPort = Viewport.Create(doc, newViewSheet.Id, curView.Id, xyzPoint);
                        var newViewPort = Viewport.Create(doc, newViewSheet.Id, curView.Id, titleBlockCenter);
                        if (newViewPort != null) // If the viewPort is null, doesnt have any drawings, don't attempt to add a family type
                        {
                            newViewPort.ChangeTypeId(textTypeId);
                            viewPortsCreated.Add(newViewPort);
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.Print($"Error: ==> {curView.Name} {curView.Id} \n{e.Message}");
                    }
                }

                viewSheetCreated.Add(newViewSheet, viewPortsCreated);
            }

            return viewSheetCreated;
        }

        Dictionary<ViewSheet, List<Viewport>> CreateSheetsFromViews2(Document doc, List<View> viewList, ElementId titleBlockId, XYZ xyzPoint, bool oneToOne, string multipleViewsSheetName, ElementId textTypeId)
        {
            //var viewSheetCreated = new List<ViewSheet>();
            var viewSheetCreated = new Dictionary<ViewSheet, List<Viewport>>();
            if (oneToOne)
            {
                foreach (var curView in viewList)
                {
                    try
                    {
                        // Temporary list for viewports to be retruned
                        var viewPortsCreated = new List<Viewport>();

                        // Create a new sheet
                        var newViewSheet = ViewSheet.Create(doc, titleBlockId);
                        newViewSheet.Name = curView.Name;
                        //viewSheetCreated.Add(newViewSheet);

                        // Create a new viewport on the new sheet and place the curView
                        var newViewPort = Viewport.Create(doc, newViewSheet.Id, curView.Id, xyzPoint);
                        List<ElementId> newElemId = new List<ElementId>() { textTypeId };
                        if (newViewPort != null) // If the viewPort is null, doesnt have any drawings, don't attempt to add a family type
                        {
                            newViewPort.ChangeTypeId(textTypeId);
                            viewPortsCreated.Add(newViewPort);
                        }

                        viewSheetCreated.Add(newViewSheet, viewPortsCreated);

                    }
                    catch (Exception e)
                    {
                        Debug.Print($"Error: ==> {curView.Name} {curView.Id} \n{e.Message}");
                    }

                }
            }
            else
            {
                // Create a single View Sheet for all views
                var newViewSheet = ViewSheet.Create(doc, titleBlockId);
                newViewSheet.Name = multipleViewsSheetName; // You can set any desired name

                // Temporary list for viewports to be retruned
                var viewPortsCreated = new List<Viewport>();

                foreach (var curView in viewList)
                {
                    try
                    {

                        // Create a new viewport on the new sheet for each view
                        var newViewPort = Viewport.Create(doc, newViewSheet.Id, curView.Id, xyzPoint);
                        if (newViewPort != null) // If the viewPort is null, doesnt have any drawings, don't attempt to add a family type
                        {
                            newViewPort.ChangeTypeId(textTypeId);
                            viewPortsCreated.Add(newViewPort);
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.Print($"Error: ==> {curView.Name} {curView.Id} \n{e.Message}");
                    }
                }

                viewSheetCreated.Add(newViewSheet, viewPortsCreated);
            }

            return viewSheetCreated;
        }

        void RelocateViewportsOnSheets(Document doc, Dictionary<ViewSheet, List<Viewport>> sheetsCreated, XYZ location)
        {
            foreach (var kvp in sheetsCreated)
            {
                ViewSheet sheet = kvp.Key;
                List<Viewport> viewports = kvp.Value;

                // Get the title block on the sheet
                FamilyInstance titleBlockInstance = new FilteredElementCollector(doc, sheet.Id)
                    .OfCategory(BuiltInCategory.OST_TitleBlocks)
                    .Cast<FamilyInstance>()
                    .FirstOrDefault();

                if (titleBlockInstance != null)
                {
                    // Get the title block's bounding box
                    BoundingBoxXYZ titleBlockBoundingBox = titleBlockInstance.get_BoundingBox(sheet);


                    if (titleBlockBoundingBox != null)
                    {
                        var titleBlockBoundingBoxMin = titleBlockBoundingBox.Min;
                        var titleBlockBoundingBoxMax = titleBlockBoundingBox.Max;
                        XYZ titleBlockCenter = 0.5 * (titleBlockBoundingBoxMin + titleBlockBoundingBoxMax);
                        //XYZ titleBlockCenter = 0.5 * (titleBlockBoundingBox.Min + titleBlockBoundingBox.Max);

                        // Calculate the offset for moving viewports to the center of the title block
                        XYZ offset = location - titleBlockCenter;

                        using (Transaction t = new Transaction(doc))
                        {
                            t.Start("Relocate Viewports");

                            // Move each viewport to the center of the title block
                            foreach (Viewport viewport in viewports)
                            {
                                XYZ viewportLocation = viewport.GetBoxCenter();

                                // Move the viewport to the new location
                                //ElementTransformUtils.MoveElement(doc, viewport.Id, offset);
                                ElementTransformUtils.MoveElement(doc, viewport.Id, titleBlockCenter);
                            }

                            t.Commit();
                        }
                    }
                }
            }
        }


        double ParseTxtToDouble(string stringNumber)
        {
            string numberString = stringNumber; // Replace with your string
            double number;
            Double.TryParse(numberString, out number);

            // 'number' now contains the double value.
            return number;

        }

        internal static PushButtonData GetButtonData()
        {
            // use this method to define the properties for this command in the Revit ribbon
            string buttonInternalName = "btnSheetsFromViews";
            string buttonTitle = "Sheets From Views";

            ButtonDataClass myButtonData1 = new ButtonDataClass(
                buttonInternalName,
                buttonTitle,
                MethodBase.GetCurrentMethod().DeclaringType?.FullName,
                Properties.Resources.Blue_32,
                Properties.Resources.Blue_16,
                "Create new sheets from selected views");

            return myButtonData1.Data;
        }
    }
}
