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

                // list of YesNo parameter names to add to the TitleBlock Family
                var listOfNewParamNames = new List<string>() { "Area 1", "Area 2", "Area 3" };

                // Step 2: Add the parameter to the title block family
                Document familyDoc = AddParameterToTitleBlockFamily(doc, titleBlock, listOfNewParamNames, out message);

                if (familyDoc == null)
                {
                    message = "Error adding parameter to title block family.";
                    return Result.Failed;
                }
            }
            catch (Exception e)
            {
                TaskDialog.Show("info", e.Message);
            }

            return Result.Succeeded;
        }

        private Document AddParameterToTitleBlockFamily(Document doc, Family titleBlock, List<string> listOfNewParamNames, out string message)
        {
            message = string.Empty;

            try
            {
                // Open the title block family for editing
                Document familyDoc = doc.EditFamily(titleBlock);

                // Start a transaction to add the parameter
                using (Transaction trans = new Transaction(familyDoc, "Add Yes/No Parameter"))
                {
                    trans.Start();  // ---------- Start the transaction ----------

                    FamilyManager familyManager = familyDoc.FamilyManager;
                    if (familyManager == null)
                    {
                        message = "FamilyManager not available in the family document.";
                        return null;
                    }

                    // Add a Yes/No parameter, with visibility set to false

                    // Set the parameter as instance type
                    bool isInstance = true;

                    // Define the new parameter name
                    foreach (var newParameterName in listOfNewParamNames)
                    {
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
                    }
                    //----------------------
                    IFamilyLoadOptions famLoadOptions = new FamilyLoadOptions();
                    famLoadOptions.OnFamilyFound(true, out bool overwriteParameterValues);
                    familyDoc.LoadFamily(doc, famLoadOptions);
                    //----------------------
                    trans.Commit();
                }
                return familyDoc;
            }
            catch (Exception ex)
            {
                message = $"Error: {ex.Message}";
                return null;
            }
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
