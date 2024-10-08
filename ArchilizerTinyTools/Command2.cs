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
            try
            {
                // Step 1: Retrieve the title block family symbol
                var titleBlock = GetTitleBlockFamily(doc, "ACCO TITLE BLOCK");
                if (titleBlock == null)
                {
                    message = "Title block not found.";
                    return Result.Failed;
                }

                // Step 2: Open the family document for editing
                Document familyDoc = doc.EditFamily(titleBlock);

                // Step 3: Add the parameter to the family document

                using (Transaction trans = new Transaction(familyDoc, "Add Family Parameter"))
                {
                    FamilyManager familyManager = familyDoc.FamilyManager;
                    if (familyManager == null)
                    {
                        message = "FamilyManager not available.";
                        return Result.Failed;
                    }

                    // Set the parameter as instance type
                    bool isInstance = true;

                    // Define the new parameter name
                    string newParameterName = "Area F";  // Or pass this dynamically
                    trans.Start();  // ---------- Start the transaction ----------

#if REVIT2021
                    // Define a new parameter group
                    BuiltInParameterGroup parameterGroup = BuiltInParameterGroup.PG_VISIBILITY;

                    // Revit 2021 uses ParameterType
                    ParameterType parameterType = ParameterType.YesNo;

                    // Create a new family parameter for Revit 2021
                    FamilyParameter newParameter = familyManager.AddParameter(
                        newParameterName,
                        parameterGroup,
                        parameterType,
                        isInstance
                    );

                    // start a sub-transaction to set the default value of the parameter
                    using (SubTransaction subTrans = new SubTransaction(familyDoc))
                    {
                        subTrans.Start();  // ---------- Start the sub-transaction ----------
                        // Set the parameter's default value to false (unchecked)
                        if (parameterType == ParameterType.YesNo)
                        {
                            familyManager.Set(newParameter, 0); // Set to 'false' (unchecked)
                        }
                        subTrans.Commit();  // ---------- Commit the sub-transaction ----------
                    }

#elif REVIT2022 || REVIT2023 || REVIT2024
                    // Revit 2022 and newer use ForgeTypeId for the parameter type but BuiltInParameterGroup for the group
                    ForgeTypeId parameterYesNoTypeId = SpecTypeId.Boolean.YesNo;  // This is the Yes/No type parameter
                    ForgeTypeId groupTypeId = GroupTypeId.Visibility;  // This is the Visibility group

                    // Get the family category
                    var familyCategory = familyDoc.OwnerFamily.FamilyCategory;

                    // Create a new family parameter for Revit 2022 and newer
                    FamilyParameter newParameter = familyManager.AddParameter(
                        newParameterName,
                        groupTypeId,
                        parameterYesNoTypeId,
                        isInstance
                    );

                    // Set the parameter's default value to false (unchecked)
                    if (parameterYesNoTypeId == SpecTypeId.Boolean.YesNo)
                    {
                        familyManager.Set(newParameter, 0); // Set to 'false' (unchecked)
                    }
#endif

                    // Commit the transaction that adds the parameter in the family document
                    trans.Commit();
                }

                // Save to a temporary file and load the family back into the project
                string tmpFile = SeveRfaToTempFile(titleBlock.Name, familyDoc);
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

        private static string SeveRfaToTempFile(string fileName, Document familyDoc)
        {
            string tempFolder = Path.GetTempPath();

            string tmpFile = Path.Combine(tempFolder, fileName + ".rfa");

            if (File.Exists(tmpFile))
                File.Delete(tmpFile);

            familyDoc.SaveAs(tmpFile);
            familyDoc.Close(false);
            return tmpFile;
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

        //internal static PushButtonData GetButtonData()
        //{
        //    // use this method to define the properties for this command in the Revit ribbon
        //    string buttonInternalName = "btnCommand2";
        //    string buttonTitle = "Button 2";

        //    ButtonDataClass myButtonData1 = new ButtonDataClass(
        //        buttonInternalName,
        //        buttonTitle,
        //        MethodBase.GetCurrentMethod().DeclaringType?.FullName,
        //        Properties.Resources.Blue_32,
        //        Properties.Resources.Blue_16,
        //        "This is a tooltip for Button 2");

        //    return myButtonData1.Data;
        //}

    }
}
