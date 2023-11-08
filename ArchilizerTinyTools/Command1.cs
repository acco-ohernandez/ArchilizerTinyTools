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
            // Collect all views in the document
            var views = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Views).Cast<View>().ToList();

            // Allow user to dynamically sellect views from a list 
            List<View> dynamicViewsList = new List<View>();

            // dynamicViewsList = GetSelectedViewsList(doc); // C Sharp Form testing

            // Collect all title blocks that are element types
            var titleBlocksCollector = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_TitleBlocks).WhereElementIsElementType();

            // Find the "30x42" title block by its name
            var titleBlockId = titleBlocksCollector.Where(t => t.Name == "30x42").First().Id;

            FilterRule rule = ParameterFilterRuleFactory.CreateEqualsRule(new ElementId((int)BuiltInParameter.SYMBOL_FAMILY_NAME_PARAM), "Viewport", false);
            ElementParameterFilter filter = new ElementParameterFilter(rule);
            IEnumerable<Element> viewPortFamilyTypes = new FilteredElementCollector(doc)
                                .WhereElementIsElementType()
                                .WherePasses(filter);



            var xyzPoint = new XYZ(0, 0, 0);
            // This bool "oneToOne" will determine if one curView sheet should be created per each curView
            // if set to false, all selected views are going to be put into one sheet.
            var oneToOne = false;

            // Selections Form
            // Selections Form
            var viewsForm = new ViewsToSheets_Form(views);
            //var viewsForm = new ViewsToSheets_Form();
            viewsForm.dgViews.ItemsSource = views.Cast<View>().Select(view => view.Name).ToList();
            //viewsForm.dgTitleBlocks.ItemsSource = titleBlocksCollector.Cast<Element>().Select(tblock => tblock.Name).ToList();
            viewsForm.dgTitleBlocks.ItemsSource = titleBlocksCollector.Cast<FamilySymbol>().Select(tblock => tblock.Name).ToList();
            viewsForm.dgTitleText.ItemsSource = viewPortFamilyTypes.Cast<Element>().Select(vpt => vpt.Name).ToList();
            viewsForm.ShowDialog();

            // get the list of view selected by the user from the viewsForm
            // Code Here

            // get the TitleBlock Selected by the user
            // Code Here

            // get the TitleText selected by the user
            // Code Here


            if (true) return Result.Cancelled;


            var sheetsCreated = new List<ViewSheet>();
            // Start a new transaction to create new sheets and viewports
            using (Transaction t = new Transaction(doc))
            {
                t.Start("Create Sheets and Viewports");

                sheetsCreated = CreateSheetsFromViews(doc, dynamicViewsList, titleBlockId, xyzPoint, oneToOne);

                // Commit the transaction to save the changes
                t.Commit();
            }
            #endregion

            // display the sheet names one per line under "View Sheets Created"
            TaskDialog.Show("Info", $"View Sheets Created: \n{string.Join("\n", sheetsCreated.Cast<ViewSheet>().Select(s => s.Name))}");

            return Result.Succeeded;
        }

        public List<View> GetSelectedViewsList(Document doc)
        {
            List<View> selectedViews = new List<View>();

            // Create a new WPF window
            Window viewSelectionWindow = new Window
            {
                Title = "Select Views for Sheets",
                Width = 300,
                Height = 300
            };

            // Create a ListBox for view selection
            ListBox viewListBox = new ListBox
            {
                SelectionMode = SelectionMode.Multiple
            };

            // Add views to the ListBox
            //This code filters out views that are of type ViewDrafting, which includes view templates, before sorting the remaining views by ViewType and then by Name.
            //var views = new FilteredElementCollector(doc)
            //                .OfCategory(BuiltInCategory.OST_Views)
            //                .Cast<View>()
            //                .Where(v => !(v is ViewDrafting))
            //                .OrderBy(v => v.ViewType)
            //                .ThenBy(v => v.Name);

            // This code filters out views that are of type ViewTemplate
            var views = new FilteredElementCollector(doc)
                           .OfCategory(BuiltInCategory.OST_Views)
                           .Cast<View>()
                           .Where(v => !v.IsTemplate)
                           .OrderBy(v => v.ViewType)
                           .ThenBy(v => v.Name);


            foreach (var view in views)
            {
                viewListBox.Items.Add(view.Name);
            }

            // Create a button for confirming the selection
            Button okButton = new Button
            {
                Content = "OK",
                Width = 80
            };

            // Handle button click
            okButton.Click += (sender, e) =>
            {
                foreach (var item in viewListBox.SelectedItems)
                {
                    string viewName = item.ToString();
                    var selectedView = views.Cast<View>().FirstOrDefault(v => v.Name == viewName);
                    if (selectedView != null)
                    {
                        selectedViews.Add(selectedView);
                    }
                }

                // Close the window
                viewSelectionWindow.Close();
            };

            // Create a StackPanel to arrange controls
            StackPanel stackPanel = new StackPanel();
            stackPanel.Children.Add(viewListBox);
            stackPanel.Children.Add(okButton);

            // Set the content of the window to the stack panel
            viewSelectionWindow.Content = stackPanel;

            // Show the window as a dialog
            viewSelectionWindow.ShowDialog();

            return selectedViews;
        }

        public List<View> GetSelectedViewsList2(Document doc)
        {
            List<View> selectedViews = new List<View>();

            // Create and configure a Windows Forms dialog
            System.Windows.Forms.Form viewSelectionDialog = new System.Windows.Forms.Form();
            viewSelectionDialog.Text = "Select Views for Sheets";

            // Create a CheckedListBox control to list views
            var viewListBox = new System.Windows.Forms.CheckedListBox();
            viewListBox.Dock = System.Windows.Forms.DockStyle.Fill;

            // Add views to the CheckedListBox
            var views = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Views);
            foreach (var view in views)
            {
                viewListBox.Items.Add(view.Name);
            }

            // Add the CheckedListBox to the dialog
            viewSelectionDialog.Controls.Add(viewListBox);

            // Add an "OK" button for the user to confirm their selection
            var okButton = new System.Windows.Forms.Button();
            okButton.Text = "OK";
            okButton.Click += (sender, e) =>
            {
                foreach (var item in viewListBox.CheckedItems)
                {
                    string viewName = item.ToString();
                    var selectedView = views.Cast<View>().FirstOrDefault(v => v.Name == viewName);
                    if (selectedView != null)
                    {
                        selectedViews.Add(selectedView);
                    }
                }

                // Close the dialog
                viewSelectionDialog.Close();
            };

            viewSelectionDialog.Controls.Add(okButton);

            // Show the dialog to the user
            viewSelectionDialog.ShowDialog();

            return selectedViews;
        }

        List<ViewSheet> CreateSheetsFromViews(Document doc, List<View> viewList, ElementId titleBlockId, XYZ xyzPoint, bool oneToOne)
        {
            var viewSheetCreated = new List<ViewSheet>();
            if (oneToOne)
            {
                foreach (var curView in viewList)
                {
                    // Create a new sheet
                    var newViewSheet = ViewSheet.Create(doc, titleBlockId);
                    newViewSheet.Name = curView.Name;
                    viewSheetCreated.Add(newViewSheet);

                    // Create a new viewport on the new sheet and place the curView
                    var newViewPort = Viewport.Create(doc, newViewSheet.Id, curView.Id, xyzPoint);
                }
            }
            else
            {
                // Create a single View Sheet for all views
                var newViewSheet = ViewSheet.Create(doc, titleBlockId);
                newViewSheet.Name = "Multiple Views"; // You can set any desired name

                foreach (var curView in viewList)
                {
                    // Create a new viewport on the new sheet for each view
                    var newViewPort = Viewport.Create(doc, newViewSheet.Id, curView.Id, xyzPoint);
                }

                viewSheetCreated.Add(newViewSheet);
            }

            return viewSheetCreated;
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
