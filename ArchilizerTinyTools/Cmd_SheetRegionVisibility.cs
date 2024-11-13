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
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            // Revit application
            UIApplication uiapp = commandData.Application;

            // Revit model
            Document doc = uiapp.ActiveUIDocument.Document;

            // Get all the viewsheet instances that contain a viewport
            List<ViewSheet> sheetsWithViewports = GetSheetsWithViewPorts(doc);

            //// Create a list of ViewSheetInfo objects to store the view sheet information to load into the form
            //List<ViewSheetInfo> viewSheetsToForm = new List<ViewSheetInfo>();
            //foreach (var sheet in sheetsWithViewports)
            //{
            //    viewSheetsToForm.Add(new ViewSheetInfo(sheet));
            //}


            // Set the value of the Visibility Yes/No parameters of the title block regions to 1
            using (Transaction transaction = new Transaction(doc))
            {
                transaction.Start("Set the value of the Visibility Yes/No parameters of the title block regions");

                foreach (var curSheet in sheetsWithViewports)
                {
                    FamilyInstance titleBlockInstance = GetTitleBlockInstanceFromViewSheet(doc, curSheet);

                    var viewOnSheet = GetViewsOnSheet(doc, curSheet).FirstOrDefault();

                    // get the associated scope box name of the view
                    var scopeBoxName = GetViewAssociatedScopeBoxName(doc, viewOnSheet);

                    // Get the associated YesNo visibility parameters of the title block instance
                    List<Parameter> yesNoVisibilityParameters = GetAssociatedYesNoVisibilityParameters(titleBlockInstance);

                    foreach (var parameter in yesNoVisibilityParameters)
                    {
                        if (parameter.Definition.Name.Contains(scopeBoxName))
                            parameter.Set(1);
                        else
                            parameter.Set(0);
                    }
                }
                transaction.Commit();
            }

            return Result.Succeeded;
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
                    if (param.Definition.ParameterType == ParameterType.YesNo && param.IsReadOnly == false)
                    {
                        // Add the parameter to the list if it meets the criteria
                        yesNoVisibilityParameters.Add(param);
                    }
#else
                    if (param.Definition.GetDataType() == SpecTypeId.Boolean.YesNo && param.IsReadOnly == false)
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
                "Create new sheets from selected views");

            return myButtonData1.Data;
        }
    }



}
