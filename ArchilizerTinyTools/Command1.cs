#region Namespaces
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Forms;
using System.Windows.Media;

using ArchilizerTinyTools.Forms;

using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

using Forms = System.Windows.Forms;
using View = Autodesk.Revit.DB.View;
#endregion

namespace ArchilizerTinyTools
{
    [Transaction(TransactionMode.Manual)]
    public class Command1 : IExternalCommand
    {
        private List<string> failedViewsToSheets;
        public static Document Doc;

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            // this is a variable for the Revit application
            UIApplication uiapp = commandData.Application;

            // this is a variable for the current Revit model
            Document doc = uiapp.ActiveUIDocument.Document;
            Doc = doc;

            // if the failedViewsToSheets is null, create a new list, if not clear the list
            failedViewsToSheets = failedViewsToSheets ?? new List<string>();
            failedViewsToSheets.Clear();

            #region FocusedCode
            try
            {

                // Collect all views in the document
                var views = new FilteredElementCollector(doc)
                    .OfCategory(BuiltInCategory.OST_Views)
                    .Cast<View>()
                    .OrderBy(x => x.ViewType)
                    .ToList();

                // Allow user to dynamically sellect views from a list 
                List<View> dynamicViewsList = new List<View>();

                // dynamicViewsList = GetSelectedViewsList(doc); // C Sharp Form testing

                // Collect all title blocks that are element types
                var titleBlocksCollector = new FilteredElementCollector(doc)
                    .OfCategory(BuiltInCategory.OST_TitleBlocks)
                    .WhereElementIsElementType()
                    .Cast<FamilySymbol>()
                    .ToList();

                ////Find the "30x42" title block by its name
                //var titleBlock = titleBlocksCollector.Where(t => t.Name == "30x42").First().Id;

                FilterRule rule = ParameterFilterRuleFactory.CreateEqualsRule(new ElementId((int)BuiltInParameter.SYMBOL_FAMILY_NAME_PARAM), "Viewport", false);
                ElementParameterFilter filter = new ElementParameterFilter(rule);
                IEnumerable<Element> viewPortFamilyTypes = new FilteredElementCollector(doc)
                                    .WhereElementIsElementType()
                                    .WherePasses(filter);

                // Selections Form
                var viewsForm = new ViewsToSheets_Form(views, titleBlocksCollector);

                viewsForm.cmb_SheetNameStandards.ItemsSource = GetSheetNameStandardsList();

                var viewInfos = views.OrderBy(x => x.ViewType)
                                     .Select(view => new ViewInfo(view.Name, view.ViewType, view.Id))
                                     .ToList();

                viewsForm.dgViews.ItemsSource = viewInfos;

                // Get list of unique "Sheet Type" values from ViewSheet parameters and send to the viewsForm
                viewsForm.cmb_SheetTypes.ItemsSource = GetSheetTypesList(doc);

                //viewsForm.dgViews.ItemsSource = views.Cast<View>().Select(view => view.Name).ToList();

                //viewsForm.dgTitleBlocks.ItemsSource = titleBlocksCollector.Cast<FamilySymbol>().OrderBy(x => x.Name).Select(tblock => tblock.Name).ToList();

                var titleBlocksInfo = titleBlocksCollector.OrderBy(tb => tb.FamilyName)
                                                                          .Select(titleBlock => new TitleBlockInfo(titleBlock))
                                                                          .ToList();
                viewsForm.dgTitleBlocks.ItemsSource = titleBlocksInfo;

                viewsForm.dgTitleText.ItemsSource = viewPortFamilyTypes.Cast<Element>().OrderBy(x => x.Name).Select(vpt => vpt.Name).ToList();
                // Show the form
                viewsForm.ShowDialog(); // <----------------------------------- The Form is shown here

                //var sheetsCreated = new List<ViewSheet>();
                var sheetsCreated = new Dictionary<ViewSheet, List<Viewport>>();

                // Check if the user doesn't click OK
                if (viewsForm.DialogResult != true)
                    return Result.Cancelled;

                // Get the selected views
                //List<View> selectedViews = viewsForm.SelectedViews;
                // Get the selected ViewInfo objects
                List<ViewInfo> selectedViewInfos = viewsForm.dgViews.SelectedItems.Cast<ViewInfo>().ToList();

                // Extract the corresponding View objects from the original list
                List<View> selectedViews = selectedViewInfos
                    .Select(viewInfo => views.FirstOrDefault(view => view.Id == viewInfo.Id))
                    .Where(view => view != null)
                    .ToList();


                // Get the selected title block
                var stb = viewsForm.dgTitleBlocks.SelectedItem as FamilySymbol;

                //FamilySymbol selectedTitleBlock = viewsForm.SelectedTitleBlock;
                // Assuming you have a DataGrid named dgTitleBlocks in your form
                //TitleBlockInfo selectedTitleBlockInfo = viewsForm.dgTitleBlocks.SelectedItem as TitleBlockInfo;
                FamilySymbol selectedTitleBlock = (viewsForm.dgTitleBlocks.SelectedItem as TitleBlockInfo).TitleBlockSymbol;

                // Get the selected Sheet Name Standard
                string selectedSheetNameStandard = viewsForm.cmb_SheetNameStandards.Text;

                // Get the Sheet Type to use for new sheets
                var selectedSheetType = viewsForm.cmb_SheetTypes.Text;
                //if (selectedSheetType == "")
                //    selectedSheetType = "NEW SHEETS CREATED"; // This would create a new Sheet Type the user can use later.


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
                    sheetsCreated = CreateSheetsFromViews(doc, selectedViews, selectedTitleBlock, xyzPoint, oneToOne, MultipleViewsSheetName, elem.Id, selectedSheetType, selectedSheetNameStandard);
                    t.Commit();
                }

                //place a the new Viewports on each Sheet at a specific position relative to the title block's bounding box
                //RelocateViewportsOnSheets(doc, sheetsCreated, xyzPoint); // Method not implemented

                // Display the sheet names
                //TaskDialog.Show("Info", $"View Sheets Created: {sheetsCreated.Count()}\n" +
                //                        $"{string.Join("\n", sheetsCreated.Select(s => s.Name))}");

                // if the failedViewsToSheets list is not empty, add each entry to a string new line

                string failedViewsToSheetsString = failedViewsToSheets.Count > 0 ? $"\n--- {failedViewsToSheets.Count} Views not added to sheets ---\n" + string.Join("\n", failedViewsToSheets) : "\nSuccess!";


                TaskDialog.Show("Info", $"View Sheets Created: {sheetsCreated.Count()}" +
                                             $"{failedViewsToSheetsString}");
                #endregion

            }
            catch (Exception e) { TaskDialog.Show("Error", $"Error: ==> {e.Message}"); }

            return Result.Succeeded;
        }

        private static List<string> GetSheetNameStandardsList()
        {
            // create a list of sheet name standards
            return new List<string>
            { "Construction",
              "Engineering - Mechanical",
              "Engineering - Plumbing",
              "Engineering - Process Pipe",
              "Engineering - Refrigeration"
            };
        }

        //write a Method that takes a view and returns the "Browser Category" and "Browser Sub-Category"
        public static Tuple<string, string> GetBrowserCategoryAndSubCategory(View view)
        {
            // Attempt to retrieve the "Browser Category" parameter
            Parameter categoryParam = view.LookupParameter("Browser Category");
            string browserCategory = categoryParam != null && categoryParam.HasValue ? categoryParam.AsString() : "Uncategorized";

            // Attempt to retrieve the "Browser Sub-Category" parameter
            Parameter subCategoryParam = view.LookupParameter("Browser Sub-Category");
            string browserSubCategory = subCategoryParam != null && subCategoryParam.HasValue ? subCategoryParam.AsString() : "No Sub-Category";

            return new Tuple<string, string>(browserCategory, browserSubCategory);
        }

        public static string GenerateSheetName(View view, string sheetNameStandard)
        {
            // GENERATE A NAME FOR THE SHEET BASED ON THE SELECTED STANDARD
            // Get the "Trade" parameter value from the view
            string trade = view.LookupParameter("Trade").AsString();

            // get the associated level of the view
            var levelName = view.GenLevel.Name;

            // Get the "Scope Box" parameter value from the view
            var scopeBoxName = view.LookupParameter("Scope Box").AsValueString();

            // Get the "Sheet Series" parameter value from the view
            string sheetSeries = view.LookupParameter("Sheet Series").AsString();

            // [TRADE]<space>[LEVEL<ALT+255>NAME]<ALT+255>[SCOPE<ALT+255>BOX<ALT+255>NAME]<space>[SHEET SERIES]
            string sheetName = $"{trade} {ConvertSpaceToAlt255(levelName)} {ConvertSpaceToAlt255(scopeBoxName)} {sheetSeries}";

            return sheetName;
        }
        //public static string GenerateSheetName(View view, string sheetNameStandard)
        //{
        //    string sheetName = view.Name;
        //    // get the associated level of the view
        //    var associatedViewLevel = view.GenLevel.Name;

        //    // Append the standard string
        //    string standardChars = GetStandardChars(sheetNameStandard);



        //    // Get the "Browser Category" and "Browser Sub-Category" for the view
        //    var browserCatAndSubCat = GetBrowserCategoryAndSubCategory(view);
        //    string browserCategory = browserCatAndSubCat.Item1;
        //    string browserSubCategory = browserCatAndSubCat.Item2;

        //    return sheetName;
        //}
        public static string ConvertSpaceToAlt255(string input)
        {
            if (input == null)
            {
                throw new ArgumentNullException(nameof(input), "Input string cannot be null");
            }

            // Replace spaces with Alt+255 " " (non-breaking space)
            string result = input.Replace(' ', ' ');
            //string result = input.Replace(' ', '\u00A0');
            //string result = input.Replace(' ', '8');


            return result;
        }
        private static string GetStandardChars(string selectedStandard)
        {
            var listOfStandards = GetSheetNameStandardsList();

            return "sCHARS"; // Not done
        }

        public static List<string> GetSheetTypesList(Document doc)
        {
            // Get list of unique "Sheet Type" values from ViewSheet parameters.
            var sheetTypeDefinitionsList = new FilteredElementCollector(doc)
                                            .OfClass(typeof(ViewSheet))
                                            .WhereElementIsNotElementType()
                                            .Cast<ViewSheet>()
                                            .SelectMany(sheet => sheet.Parameters.Cast<Parameter>())
                                            .Where(parameter => parameter.Definition.Name == "Sheet Type")
                                            .Select(parameter => parameter.AsString())
                                            .Distinct()
                                            .ToList();
            return sheetTypeDefinitionsList;
        }

        Dictionary<ViewSheet, List<Viewport>> CreateSheetsFromViews(
            Document doc,
            List<View> viewList,
            FamilySymbol titleBlock,
            XYZ xyzPoint,
            bool oneToOne,
            string multipleViewsSheetName,
            ElementId textTypeId,
            string sheetType,
            string selectedSheetNameStandard)
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

                        Tuple<string, string> sheetNameAndNumber = GenerateSheetNameAndNumber(curView, selectedSheetNameStandard);
                        // Check if there is an existing sheet with the same number, if so throw an exception
                        CheckForExistingSheetNumber(doc, sheetNameAndNumber.Item2);

                        // Create a new sheet
                        var newViewSheet = ViewSheet.Create(doc, titleBlock.Id);



                        newViewSheet.SheetNumber = sheetNameAndNumber.Item2;
                        newViewSheet.Name = sheetNameAndNumber.Item1;

                        // Set the Sheet Type for the newViewSheet
                        SetViewSheetParameterByParameterName(sheetType, newViewSheet);


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

                        //LeftAlignViewPortToViewSheet(doc, newViewSheet, newViewPort, titleBlockCenter);

                        // Add the new sheet and associated viewports to the dictionary
                        viewSheetCreated.Add(newViewSheet, viewPortsCreated);
                    }
                    catch (Exception e)
                    {
                        // List of views that failed to create a sheet
                        failedViewsToSheets.Add($"{curView.Id} {curView.Name}");

                        Debug.Print($"Error: ==> {curView.Name} {curView.Id} \n{e.Message}");
                    }
                }
            }
            else // if oneToOne==false Create a single View Sheet for all views
            {
                var newViewSheet = ViewSheet.Create(doc, titleBlock.Id);
                newViewSheet.Name = multipleViewsSheetName;

                // Set the Sheet Type for the newViewSheet
                SetViewSheetParameterByParameterName(sheetType, newViewSheet);

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

        private Tuple<string, string> GenerateSheetNameAndNumber(View curView, string selectedSheetNameStandard)
        {
            // Generate the sheet name and number based on the selected standard
            string newsheetName;
            if (selectedSheetNameStandard == "Construction")
                newsheetName = GenerateSheetName(curView, selectedSheetNameStandard);
            else
                newsheetName = curView.Name;

            var sheetNumber = GenerateSheetNumber(curView);

            // return the Tuple with the sheet name and number
            return new Tuple<string, string>(newsheetName, sheetNumber);
        }

        private void CheckForExistingSheetNumber(Document doc, string sheetNumber)
        {
            // Check if there is an existing sheet with the same number
            var existingSheet = new FilteredElementCollector(doc)
                .OfClass(typeof(ViewSheet))
                .Cast<ViewSheet>()
                .FirstOrDefault(sheet => sheet.SheetNumber == sheetNumber);

            if (existingSheet == null)
                return;

            // else throw an exception
            throw new Exception($"Sheet with number {sheetNumber} already exists.");
        }

        private string GenerateSheetNumber(View curView)
        {
            // Get the "Browser Category" and "Browser Sub-Category" for the view
            var browserCatAndSubCat = GetBrowserCategoryAndSubCategory(curView);
            string browserCategory = browserCatAndSubCat.Item1;
            string browserSubCategory = browserCatAndSubCat.Item2;

            // split the browserCategory and get the [0] to get the prefix
            string SheetNumPrefix = browserSubCategory.Split(' ')[0];

            // get the LevelSuffix from the View's Level, split the name and get the last integers
            //string LevelSuffix = curView.GenLevel.Name.Split(' ').Last();
            string LevelSuffix = GetBOMParam(curView);

            // get the ScopeBoxSuffix from the View's Scope Box, split the name and get the last integers
            var ScopeBoxSuffix = curView.LookupParameter("Scope Box").AsValueString();
            // split the scope box name and get the last integers
            ScopeBoxSuffix = ScopeBoxSuffix.Split(' ').Last();

            var standardAbbriviation = GetSheetStandardNamingTable(curView.GenLevel.Name);
            var abbr = "";
            if (standardAbbriviation != "") { abbr = standardAbbriviation; }



            // Generate the sheet number "[]"
            //string sheetNumber = $"{SheetNumPrefix}-{abbr}{LevelSuffix}.{ScopeBoxSuffix}";
            string sheetNumber = $"{SheetNumPrefix}-{LevelSuffix}.{ScopeBoxSuffix}";

            return sheetNumber;
        }

        private string GetBOMParam(View curView)
        {

            var _LEVEL = curView.GenLevel.Name;
            // GET THE LEVEL
            Level viewLevel = new FilteredElementCollector(Doc)
                .OfClass(typeof(Level))
                .Cast<Level>()
                .FirstOrDefault(l => l.Name == _LEVEL);

            // Get the "BOM" parameter value from the view
            var _paramName = "ACCO Level for BOM";
            var _param = viewLevel.LookupParameter(_paramName).AsString();

            return _param;
        }

        // Write a method called GetSheetStandardNamingTable that will import a CSV file with the sheet standard naming table into a dictionary. the csv will be located in the same location of this assembly
        // The CSV file will have the following columns: "Sheet Type", "Sheet Name Standard"
        // it will take in a view string name and return the sheet name standard
        public static string GetSheetStandardNamingTable(string viewName)
        {
            viewName = viewName.Split()[0];

            // Get the path of the CSV file
            string csvFilePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "SheetStandardsNamingTable.csv");

            // Read the CSV file into a dictionary
            Dictionary<string, string> sheetStandards = ReadCSVFile(csvFilePath);

            // if the view name contains any of the keys in the dictionary, return the value
            var _value = sheetStandards.Keys.Contains(viewName) ? sheetStandards[viewName] : "";

            return _value;
        }
        //public static string GetSheetStandardNamingTable(string viewName)
        //{
        //    viewName = viewName.Split()[0];

        //    // Get the path of the CSV file
        //    string csvFilePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "SheetStandardsNamingTable.csv");

        //    // Read the CSV file into a dictionary
        //    Dictionary<string, string> sheetStandards = ReadCSVFile(csvFilePath);

        //    // if the view name contains any of the keys in the dictionary, return the value
        //    foreach (var name in sheetStandards)
        //    {
        //        if (viewName.Contains(name.Key))
        //        {
        //            var _value = sheetStandards[name.Value];
        //            return _value;
        //        }
        //    }


        //    return "";
        //}
        public static Dictionary<string, string> ReadCSVFile(string filePath)
        {
            Dictionary<string, string> columnData = new Dictionary<string, string>();

            using (StreamReader reader = new StreamReader(filePath))
            {
                // Skip the first line (header row)
                reader.ReadLine();

                string line = reader.ReadLine(); // Read the second line
                while (line != null)
                {
                    string[] fields = line.Split(',');

                    // Assuming the first column is the key and the second column is the value
                    if (fields.Length >= 2)
                    {
                        string key = fields[0];
                        string value = fields[1];

                        columnData[key] = value;
                    }

                    line = reader.ReadLine(); // Read the next line
                }
            }

            return columnData;
        }


        public static void SetViewSheetParameterByParameterName(string sheetType, ViewSheet newViewSheet)
        {
            // Set the Sheet Type for the newViewSheet
            if (!string.IsNullOrEmpty(sheetType))
            {
                Parameter sheetTypeParameter = newViewSheet.LookupParameter("Sheet Type");
                if (sheetTypeParameter != null)
                {
                    sheetTypeParameter.Set(sheetType);
                }
            }
        }

        Dictionary<ViewSheet, List<Viewport>> CreateSheetsFromViews4(Document doc, List<View> viewList, FamilySymbol titleBlock, XYZ xyzPoint, bool oneToOne, string multipleViewsSheetName, ElementId textTypeId, string sheetType)
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

                        // Set the Sheet Type for the newViewSheet

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

                // Set the Sheet Type for the newViewSheet


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

        XYZ GetCenterOfBoundingBox(BoundingBoxXYZ boundingBox)
        {
            // Helper method to get the center of a bounding box
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


    public class ViewInfo
    {
        public string Name { get; set; }
        public ViewType ViewType { get; set; }
        public ElementId Id { get; set; } // Add this property

        public ViewInfo(string name, ViewType viewType, ElementId id)
        {
            Name = name;
            ViewType = viewType;
            Id = id;
        }
    }

    public class TitleBlockInfo
    {
        public FamilySymbol TitleBlockSymbol { get; set; }
        public string TitleBlockName { get; set; }
        public string FamilyName { get; set; }

        public TitleBlockInfo(FamilySymbol titleBlockName)
        {
            TitleBlockSymbol = titleBlockName;
            TitleBlockName = titleBlockName.Name;
            FamilyName = titleBlockName.FamilyName;
        }
    }
}
