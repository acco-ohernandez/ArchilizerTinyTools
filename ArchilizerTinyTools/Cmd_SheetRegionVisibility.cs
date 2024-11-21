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
    public class Cmd_SheetRegionVisibility : IExternalCommand
    {
        public static List<YesNoParamInfo> YesNoParamsResults { get; set; }

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            // Revit application
            UIApplication uiapp = commandData.Application;

            // Revit model
            Document doc = uiapp.ActiveUIDocument.Document;

            // Get all the viewsheet instances that contain a viewport
            //List<ViewSheet> sheetsWithViewports = GetSheetsWithViewPorts(doc);
            List<ViewSheet> sheetsWithViewports = GetSheetsWithViewPortsAndAssociatedScopeBoxes(doc);

            if (!VerifyRequirements(sheetsWithViewports))
                return Result.Cancelled;

            // Load list of ViewSheetInfo objects into ViewSheets_Form
            ViewSheets_Form viewSheetsForm = new ViewSheets_Form(sheetsWithViewports);
            viewSheetsForm.ShowDialog();
            if (viewSheetsForm.DialogResult != true)
                return Result.Cancelled;

            // Get the selected ViewSheetInfo objects from the DataGrid
            List<ViewSheet> selectedViewSheets = viewSheetsForm.GetSelectedViewSheets();

            //if (true) return Result.Cancelled;

            // Set the value of the Visibility Yes/No parameters of the title block regions to 1
            using (Transaction transaction = new Transaction(doc))
            {
                transaction.Start("Set the value of the Visibility Yes/No parameters of the title block regions");

                // Initialize the YesNoParamsResults
                YesNoParamsResults = new List<YesNoParamInfo>();

                foreach (var curSheet in selectedViewSheets)
                {
                    FamilyInstance titleBlockInstance = GetTitleBlockInstanceFromViewSheet(doc, curSheet);
                    if (titleBlockInstance == null)
                    {
                        Debug.WriteLine($"No title block instance found on sheet {curSheet.SheetNumber} - {curSheet.Name}");
                        //// if there is no title block instance on the sheet, skip the sheet
                        //continue;
                        // Rollback the transaction if there is no title block instance on the sheet
                        transaction.RollBack();
                        TaskDialog.Show("Error", $"No title block instance found on sheet {curSheet.SheetNumber} - {curSheet.Name}");
                        return Result.Failed;
                    }

                    var viewOnSheet = GetViewsOnSheet(doc, curSheet).FirstOrDefault();

                    // get the associated scope box name of the view
                    var scopeBoxName = GetViewAssociatedScopeBoxName(doc, viewOnSheet);
                    if (string.IsNullOrEmpty(scopeBoxName))
                    {
                        Debug.WriteLine($"No scope box associated with the view on sheet {curSheet.SheetNumber} - {curSheet.Name}");
                        // if there is no scope box associated with the view, skip the sheet
                        continue;
                    }

                    // Get the associated YesNo visibility parameters of the title block instance
                    List<Parameter> yesNoVisibilityParameters = GetAssociatedYesNoVisibilityParameters(titleBlockInstance);
                    if (yesNoVisibilityParameters.Any()) // if there are any YesNo parameters
                        TurnOnYesNoParamsByScopeBoxName(scopeBoxName, yesNoVisibilityParameters, curSheet.SheetNumber, curSheet.Name);
                }
                transaction.Commit();
            }

            ShowResults();

            return Result.Succeeded;
        }

        private void ShowResults()
        {
            if (YesNoParamsResults.Count == 0)
            {
                TaskDialog.Show("Info", "No YesNo parameters changed.");
                return;
            }

            // sort YesNoParamsResults by SheetNumber
            YesNoParamsResults = YesNoParamsResults.OrderBy(x => x.SheetNumber).ToList();

            // Show the results in a new ResultYesNoParams_Form
            ResultYesNoParams_Form resultYesNoParamsForm = new ResultYesNoParams_Form(YesNoParamsResults);
            resultYesNoParamsForm.ShowDialog();

        }

        private static void TurnOnYesNoParamsByScopeBoxName(
            string scopeBoxName,
            List<Parameter> yesNoVisibilityParameters,
            string sheetNumber,
            string sheetName)
        {
            foreach (var parameter in yesNoVisibilityParameters)
            {
                if (parameter.Definition.Name.Contains(scopeBoxName))
                {
                    parameter.Set(1);
                    YesNoParamsResults.Add(new YesNoParamInfo
                    {
                        ParamName = parameter.Definition.Name,
                        Value = parameter.Id,
                        SheetNumber = sheetNumber,
                        SheetName = sheetName
                    });
                }
                else
                    parameter.Set(0);
            }
        }

        private static bool VerifyRequirements(List<ViewSheet> sheetsWithViewports)
        {
            if (sheetsWithViewports.Count == 0)
            {
                TaskDialog.Show("Info", "No Sheets meeting the required crateria found.");
                return false;
            }
            return true;
        }

        private static FamilyInstance GetTitleBlockInstanceFromViewSheet(Document doc, ViewSheet viewSheet)
        {
            FamilyInstance titleBlockFamilyInstance = null;
            // get the title block family instance of the view sheet
            titleBlockFamilyInstance = new FilteredElementCollector(doc, viewSheet.Id)
                .OfCategory(BuiltInCategory.OST_TitleBlocks)
                .WhereElementIsNotElementType()
                .Cast<FamilyInstance>()
                .FirstOrDefault();

            return titleBlockFamilyInstance;
        }

        private static List<ViewSheet> GetSheetsWithViewPortsAndAssociatedScopeBoxes(Document doc)
        {
            // Collect all ViewSheet elements from the document
            var viewSheetCollector = new FilteredElementCollector(doc)
                .OfClass(typeof(ViewSheet))
                .WhereElementIsNotElementType()
                .Cast<ViewSheet>()
                .Where(sheet =>
                {
                    // Check for a valid title block instance
                    var curSheetTitleBlock = GetTitleBlockInstanceFromViewSheet(doc, sheet);
                    if (curSheetTitleBlock == null)
                        return false;

                    // Check for Yes/No visibility parameters
                    var yesNoParams = GetAssociatedYesNoVisibilityParameters(curSheetTitleBlock);
                    if (!yesNoParams.Any())
                        return false;

                    // Check if any view on the sheet has an associated scope box
                    var viewOnSheet = GetViewsOnSheet(doc, sheet).FirstOrDefault();
                    if (viewOnSheet == null)
                        return false;

                    var scopeBoxName = GetViewAssociatedScopeBoxName(doc, viewOnSheet);
                    return !string.IsNullOrEmpty(scopeBoxName);
                })
                .ToList();

            // Filter collected sheets for those with viewports having associated scope boxes
            var sheetsWithViewportsAndScopeBoxes = viewSheetCollector
                .Where(sheet =>
                    new FilteredElementCollector(doc)
                        .OfClass(typeof(Viewport))
                        .WhereElementIsNotElementType()
                        .Cast<Viewport>()
                        .Any(viewport =>
                        {
                            // Check if the viewport belongs to the current sheet
                            if (viewport.SheetId != sheet.Id)
                                return false;

                            // Retrieve the view associated with the viewport
                            View view = doc.GetElement(viewport.ViewId) as View;
                            if (view == null)
                                return false;

                            // Check if the view has a valid ScopeBox parameter
                            var scopeBoxId = view.get_Parameter(BuiltInParameter.VIEWER_VOLUME_OF_INTEREST_CROP)?.AsElementId() ?? ElementId.InvalidElementId;
                            return scopeBoxId != ElementId.InvalidElementId;
                        })
                )
                // Order by "Sheet Type" and then "Name"
                .OrderBy(sheet => sheet.LookupParameter("Sheet Type")?.AsString())
                .ThenBy(sheet => sheet.Name)
                .ToList();

            // Return the filtered list of sheets
            return sheetsWithViewportsAndScopeBoxes;
        }

        private static List<ViewSheet> GetSheetsWithViewPortsAndAssociatedScopeBoxes3(Document doc)
        {
            // Create a FilteredElementCollector for collecting all ViewSheet elements from the given document
            var viewSheetCollector = new FilteredElementCollector(doc)
                .OfClass(typeof(ViewSheet))
                .WhereElementIsNotElementType()
                .Cast<ViewSheet>()
                .Where(sheet =>
                {
                    // Get the title block instance from the view sheet
                    var curSheetTitleBlock = GetTitleBlockInstanceFromViewSheet(doc, sheet);
                    if (curSheetTitleBlock == null)
                        return false;

                    // Check if the title block instance has associated Yes/No visibility parameters
                    var yesNoParams = GetAssociatedYesNoVisibilityParameters(curSheetTitleBlock);
                    if (!yesNoParams.Any())
                        return false;

                    // Get a view on the sheet
                    var viewOnSheet = GetViewsOnSheet(doc, sheet).FirstOrDefault();
                    if (viewOnSheet == null)
                        return false;

                    // Check if the view has an associated scope box
                    var scopeBoxName = GetViewAssociatedScopeBoxName(doc, viewOnSheet);
                    return !string.IsNullOrEmpty(scopeBoxName);
                })
                .ToList();


            // Filter the collected sheets to only include those that have associated viewports with views containing a scope box
            var sheetsWithViewportsAndScopeBoxes = viewSheetCollector
                .Where(sheet =>
                    // Create a new FilteredElementCollector for collecting all Viewport elements in the document
                    new FilteredElementCollector(doc)
                        .OfClass(typeof(Viewport))
                        .WhereElementIsNotElementType()
                        .Cast<Viewport>()
                        // Check if any viewport on the sheet has an associated view with a valid scope box
                        .Any(viewport =>
                        {
                            if (viewport.SheetId != sheet.Id)
                                return false;

                            // Get the view associated with this viewport
                            View view = doc.GetElement(viewport.ViewId) as View;

                            // Check if the view has an associated scope box
                            if (view != null)
                            {
                                // Retrieve the ScopeBox parameter from the view
                                ElementId scopeBoxId = view.get_Parameter(BuiltInParameter.VIEWER_VOLUME_OF_INTEREST_CROP)?.AsElementId() ?? ElementId.InvalidElementId;

                                // Verify if the scope box ID is valid
                                return scopeBoxId != ElementId.InvalidElementId;
                            }
                            return false;
                        })
                )
                // order by sheet type and sheet name
                .OrderBy(x => x.LookupParameter("Sheet Type")?.AsString())
                .ThenBy(x => x.Name)
                .ToList();

            // Return the list of ViewSheet objects that contain viewports with views that have associated scope boxes
            return sheetsWithViewportsAndScopeBoxes;
        }

        private static List<ViewSheet> GetSheetsWithViewPortsAndAssociatedScopeBoxes2(Document doc)
        {
            // Create a FilteredElementCollector for collecting all ViewSheet elements from the given document
            var viewSheetCollector = new FilteredElementCollector(doc)
                .OfClass(typeof(ViewSheet))
                .WhereElementIsNotElementType()
                .Cast<ViewSheet>();

            // Filter the collected sheets to only include those that have associated viewports with views containing a scope box
            var sheetsWithViewportsAndScopeBoxes = viewSheetCollector
                .Where(sheet =>
                    // Create a new FilteredElementCollector for collecting all Viewport elements in the document
                    new FilteredElementCollector(doc)
                        .OfClass(typeof(Viewport))
                        .WhereElementIsNotElementType()
                        .Cast<Viewport>()
                        // Check if any viewport on the sheet has an associated view with a valid scope box
                        .Any(viewport =>
                        {
                            if (viewport.SheetId != sheet.Id)
                                return false;

                            // Get the view associated with this viewport
                            View view = doc.GetElement(viewport.ViewId) as View;

                            // Check if the view has an associated scope box
                            if (view != null)
                            {
                                // Retrieve the ScopeBox parameter from the view
                                ElementId scopeBoxId = view.get_Parameter(BuiltInParameter.VIEWER_VOLUME_OF_INTEREST_CROP)?.AsElementId() ?? ElementId.InvalidElementId;

                                // Verify if the scope box ID is valid
                                return scopeBoxId != ElementId.InvalidElementId;
                            }
                            return false;
                        })
                )
                // order by sheet type and sheet name
                .OrderBy(x => x.LookupParameter("Sheet Type")?.AsString())
                .ThenBy(x => x.Name)
                .ToList();

            // Return the list of ViewSheet objects that contain viewports with views that have associated scope boxes
            return sheetsWithViewportsAndScopeBoxes;
        }

        // Method to retrieve a list of ViewSheet objects that contain viewports
        private static List<ViewSheet> GetSheetsWithViewPorts(Document doc)
        {
            // Create a FilteredElementCollector for collecting all ViewSheet elements from the given document
            var viewSheetCollector = new FilteredElementCollector(doc)
                // Filter the collector to only include elements of type ViewSheet
                .OfClass(typeof(ViewSheet))
                // Exclude element types to only retrieve instances of ViewSheet
                .WhereElementIsNotElementType()
                // Cast the filtered elements to ViewSheet type for easier handling
                .Cast<ViewSheet>();

            // Filter the collected sheets to only include those that have associated viewports
            var sheetsWithViewports = viewSheetCollector
                // For each sheet, check if it contains at least one viewport
                .Where(sheet =>
                    // Create a new FilteredElementCollector for collecting all Viewport elements in the document
                    new FilteredElementCollector(doc)
                        // Filter the collector to only include elements of type Viewport
                        .OfClass(typeof(Viewport))
                        // Exclude element types to only retrieve instances of Viewport
                        .WhereElementIsNotElementType()
                        // Cast the filtered elements to Viewport type for easier handling
                        .Cast<Viewport>()
                        // Check if any viewport has a SheetId matching the current sheet's Id
                        .Any(viewport => viewport.SheetId == sheet.Id))
                // Convert the filtered collection to a list of ViewSheet objects
                .ToList();

            // Return the list of ViewSheet objects that contain viewports
            return sheetsWithViewports;
        }

        // Method to retrieve all visibility YesNo parameters from a ViewSheet
        public static List<Parameter> GetVisibilityYesNoParameters(ViewSheet viewSheet)
        {
            // Initialize a list to store the visibility YesNo parameters
            List<Parameter> visibilityYesNoParameters = new List<Parameter>();

            // Iterate over each parameter of the ViewSheet
            foreach (Parameter param in viewSheet.Parameters)
            {
                // Check if the parameter is of YesNo type (built-in or user-defined)
#if REVIT2020 || REVIT2021
                if (param.Definition.ParameterType == ParameterType.YesNo)
                {
                    // Add the parameter to the list if it matches the YesNo type
                    visibilityYesNoParameters.Add(param);
                }
#else
                if (param.Definition.GetDataType() == SpecTypeId.Boolean.YesNo)
                {
                    // Add the parameter to the list if it matches the YesNo type
                    visibilityYesNoParameters.Add(param);
                }
#endif
            }

            // Return the list of visibility YesNo parameters
            return visibilityYesNoParameters;
        }

        // Method to check if a title block instance contains associated YesNo visibility parameters
        public static List<Parameter> GetAssociatedYesNoVisibilityParameters(FamilyInstance titleBlockInstance)
        {
            // Initialize a list to store the associated YesNo visibility parameters
            List<Parameter> yesNoVisibilityParameters = new List<Parameter>();

            // Check if the provided FamilyInstance is a title block
            if (titleBlockInstance.Symbol.Family.FamilyCategory.Name == "Title Blocks")
            {
                // Iterate over each parameter of the title block instance
                foreach (Parameter param in titleBlockInstance.Parameters)
                {
                    // Check if the parameter is of YesNo type and controls visibility
#if REVIT2020 || REVIT2021
                    if (param.Definition.ParameterType == ParameterType.YesNo &&
                        param.IsReadOnly == false &&
                        param.Definition.ParameterGroup == BuiltInParameterGroup.PG_VISIBILITY
                        )
                    {
                        // Add the parameter to the list if it meets the criteria
                        yesNoVisibilityParameters.Add(param);
                    }
#else
                    if (param.Definition.GetDataType() == SpecTypeId.Boolean.YesNo &&
                        param.IsReadOnly == false &&
                        param.Definition.ParameterGroup == BuiltInParameterGroup.PG_VISIBILITY
                        )
                    {
                        // Add the parameter to the list if it meets the criteria
                        yesNoVisibilityParameters.Add(param);
                    }
#endif
                }
            }

            // Return the list of associated YesNo visibility parameters, if any
            return yesNoVisibilityParameters;
        }

        public static List<View> GetViewsOnSheet(Document doc, ViewSheet sheet)
        {
            // Initialize a list to store the views found on the sheet
            List<View> viewsOnSheet = new List<View>();

            // Collect all Viewport elements associated with the specified sheet
            var viewports = new FilteredElementCollector(doc)
                .OfClass(typeof(Viewport))
                .WhereElementIsNotElementType()
                .Cast<Viewport>()
                .Where(viewport => viewport.SheetId == sheet.Id);

            // For each viewport found, retrieve the associated view
            foreach (var viewport in viewports)
            {
                // Get the view associated with the viewport
                View view = doc.GetElement(viewport.ViewId) as View;
                if (view != null)
                {
                    // Add the view to the list
                    viewsOnSheet.Add(view);
                }
            }

            // Return the list of views placed on the sheet
            return viewsOnSheet;
        }



        // Method to get the name of the scope box associated with a view
        public static string GetViewAssociatedScopeBoxName(Document doc, View view)
        {
            // Get the scope box parameter
            Parameter scopeBoxParam = view.get_Parameter(BuiltInParameter.VIEWER_VOLUME_OF_INTEREST_CROP);

            // Check if the scope box parameter exists and is not null
            if (scopeBoxParam != null && scopeBoxParam.StorageType == StorageType.ElementId)
            {
                // Retrieve the ElementId of the scope box
                ElementId scopeBoxId = scopeBoxParam.AsElementId();

                // Check if the scope box ID is valid (not an empty element)
                if (scopeBoxId != ElementId.InvalidElementId)
                {
                    // Retrieve the scope box element using the ID
                    Element scopeBox = doc.GetElement(scopeBoxId);

                    // Return the name of the scope box if it's found
                    return scopeBox?.Name ?? string.Empty;
                }
            }

            // Return an empty string if there is no associated scope box or parameter is null
            return string.Empty;
        }


        internal static PushButtonData GetButtonData()
        {
            // use this method to define the properties for this command in the Revit ribbon
            string buttonInternalName = "BtnSheetRegionVisibility";
            string buttonTitle = "Sheet\nRegionVisibility";

            ButtonDataClass myButtonData1 = new ButtonDataClass(
                buttonInternalName,
                buttonTitle,
                MethodBase.GetCurrentMethod().DeclaringType?.FullName,
                Properties.Resources.Red_32,
                Properties.Resources.Red_16,
                "Set KeyMap YesNo param to On based on the scope box name of the view placed on each sheet. \nThe parameters have to have the same name of the scope box associated to the viewport");

            return myButtonData1.Data;
        }
    }
}
