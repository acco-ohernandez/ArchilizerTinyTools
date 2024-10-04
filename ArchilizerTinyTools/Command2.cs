#region Namespaces
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

#endregion

namespace ArchilizerTinyTools
{
    [Transaction(TransactionMode.Manual)]
    public class Command2 : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            // Get the active application and document
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = uidoc.Document;

            // Step 1: Retrieve the title block family symbol
            //FamilySymbol titleBlock = GetTitleBlockFamilySymbols(doc)
            //    .Where(tv => tv.Name == "30x42")
            //    .Where(t => t.FamilyName == "ACCO TITLE BLOCK")
            //    .FirstOrDefault();
            var titleBlock = GetTitleBlockFamily(doc, "ACCO TITLE BLOCK - Copy");



            if (titleBlock == null)
            {
                message = "Title block not found.";
                return Result.Failed;
            }

            // Step 2: Open the family document for editing
            Document familyDoc = doc.EditFamily(titleBlock);

            // Step 3: Add the parameter to the family document
            // Step 3: Add the parameter to the family document

            try
            {
                using (Transaction trans = new Transaction(familyDoc, "Add Family Parameter"))
                {


                    FamilyManager familyManager = familyDoc.FamilyManager;
                    if (familyManager == null)
                    {
                        message = "FamilyManager not available.";
                        return Result.Failed;
                    }

                    // Define a new parameter group
                    BuiltInParameterGroup parameterGroup = BuiltInParameterGroup.PG_VISIBILITY;

                    // Set the parameter as instance type
                    bool isInstance = true;

                    // Define the new parameter name
                    string newParameterName = "Area B";  // Or pass this dynamically
                    trans.Start();

#if REVIT2021
                // Revit 2021 uses ParameterType
                ParameterType parameterType = ParameterType.YesNo;

                // Create a new family parameter for Revit 2021
                FamilyParameter newParameter = familyManager.AddParameter(
                    newParameterName,
                    parameterGroup,
                    parameterType,
                    isInstance
                );

#elif REVIT2022 || REVIT2023 || REVIT2024
                    // Revit 2022 and newer use ForgeTypeId for the parameter type but BuiltInParameterGroup for the group
                    ForgeTypeId parameterYesNoTypeId = SpecTypeId.Boolean.YesNo;  // This is the Yes/No type parameter
                    ForgeTypeId groupTypeId = GroupTypeId.Visibility;  // This is the Visibility group
                    var familyCategory = familyDoc.OwnerFamily.FamilyCategory;


                    FamilyParameter newParameter = familyManager.AddParameter(
                        newParameterName,
                        groupTypeId,
                        parameterYesNoTypeId,
                        isInstance
                    );

#endif


                    // Commit the transaction that adds the parameter in the family document
                    trans.Commit();
                }

                // get the temp folder path
                string tempFolder = Path.GetTempPath();

                string tmpFile = Path.Combine(tempFolder, titleBlock.Name + ".rfa");

                if (File.Exists(tmpFile))
                    File.Delete(tmpFile);

                familyDoc.SaveAs(tmpFile);
                familyDoc.Close(false);


                using (Transaction trans = new Transaction(doc, "Load family."))
                {
                    trans.Start();
                    IFamilyLoadOptions famLoadOptions = new FamilyLoadOptions();
                    famLoadOptions.OnFamilyFound(true, out bool overwriteParameterValues);
                    Family newFam = null;
                    doc.LoadFamily(tmpFile, famLoadOptions, out newFam);
                    trans.Commit();
                }
            }
            catch (Exception e)
            {
                TaskDialog.Show("info", e.Message);
            }

            return Result.Succeeded;
        }


        // Helper method to get the title block family symbols
        private static List<FamilySymbol> GetTitleBlockFamilySymbols(Document doc)
        {
            return new FilteredElementCollector(doc)
                            .OfCategory(BuiltInCategory.OST_TitleBlocks)
                            .WhereElementIsElementType()
                            .Cast<FamilySymbol>()
                            .ToList();
        }
        private static Family GetTitleBlockFamily2(Document doc, string famName)
        {
            return new FilteredElementCollector(doc)
                            .OfCategory(BuiltInCategory.OST_TitleBlocks)
                            .WhereElementIsNotElementType()
                            .Cast<Family>()
                            .Where(f => f.Name == famName)
                            .FirstOrDefault();

        }
        private static Family GetTitleBlockFamily(Document doc, string famName)
        {
            // Collect all FamilySymbols in the TitleBlocks category
            FamilySymbol titleBlockSymbol = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_TitleBlocks)
                .WhereElementIsElementType() // We're looking for FamilySymbols, which are ElementTypes
                .Cast<FamilySymbol>()
                .Where(f => f.FamilyName == famName)
                .FirstOrDefault();

            // Return the Family from the FamilySymbol (if found)
            return titleBlockSymbol?.Family;
        }




        // Implement LoadFamilyOptions class (optional customization)
        private class LoadFamilyOptions : IFamilyLoadOptions
        {
            public bool OnFamilyFound(bool familyInUse, out bool overwriteParameterValues)
            {
                overwriteParameterValues = true;  // Overwrite existing parameter values
                return true;  // Load family regardless if it’s already in use
            }

            public bool OnSharedFamilyFound(Family sharedFamily, bool familyInUse, out FamilySource source, out bool overwriteParameterValues)
            {
                source = FamilySource.Project;
                overwriteParameterValues = true;  // Overwrite shared family
                return true;
            }
        }



        internal static PushButtonData GetButtonData()
        {
            // use this method to define the properties for this command in the Revit ribbon
            string buttonInternalName = "btnCommand2";
            string buttonTitle = "Button 2";

            ButtonDataClass myButtonData1 = new ButtonDataClass(
                buttonInternalName,
                buttonTitle,
                MethodBase.GetCurrentMethod().DeclaringType?.FullName,
                Properties.Resources.Blue_32,
                Properties.Resources.Blue_16,
                "This is a tooltip for Button 2");

            return myButtonData1.Data;
        }
        private class FamilyLoadOptions : IFamilyLoadOptions
        {
            public bool OnFamilyFound(bool familyInUse, out bool overwriteParameterValues)
            {
                overwriteParameterValues = true;
                return true;
            }

            public bool OnSharedFamilyFound(Family sharedFamily, bool familyInUse, out FamilySource source, out bool overwriteParameterValues)
            {
                source = FamilySource.Family;
                overwriteParameterValues = true;
                return true;
            }
        }
    }
}
