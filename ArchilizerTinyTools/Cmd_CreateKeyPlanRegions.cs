#region Namespaces
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Controls;
using System.Windows.Input;

using ArchilizerTinyTools.Forms;

using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

#endregion

namespace ArchilizerTinyTools
{
    [Transaction(TransactionMode.Manual)]
    public class Cmd_CreateKeyPlanRegions : IExternalCommand
    {
        public string SetupViewName { get; private set; }

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            // Get the active application and document
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = uidoc.Document;


            //---------
            // list of YesNo parameter names to add to the TitleBlock Family
            // get all the elemements Ids from the view "BIM Setup View -" then get all the elements Category.Name == "Scope Boxes"
            List<string> listOfNewParamNames = GetAllScopeBoxesFromParentBIMSetupView(doc, "BIM Setup View -"); // dynamic list of parameter names

            if (!ValidateBIMSetupViewHasScopeBoxes(listOfNewParamNames)) // if the view does not exist or does not have scope boxes
                return Result.Failed;

            //---------
            // call the KeyPlanParameterFromScopeBoxes_Form
            var keyPlanParameterFromScopeBoxes_Form = new KeyPlanParameterFromScopeBoxes_Form(doc, listOfNewParamNames);
            keyPlanParameterFromScopeBoxes_Form.ShowDialog();
            if (keyPlanParameterFromScopeBoxes_Form.DialogResult == false)
                return Result.Cancelled;

            var selectedScopeBoxNames = keyPlanParameterFromScopeBoxes_Form.GetSelectedScopeBoxNames();

            // Get the selected title block from the form
            var selectedTitleBlockFam = keyPlanParameterFromScopeBoxes_Form.GetSelectedTitleBlock();

            try
            {
                //// Usage example
                //var titleBlockListSymbols = GetTitleBlockFamilySymbolsT(doc);

                //// Create and show the form
                //var titleBlocksListForm = new TitleBlocksListForm();
                //titleBlocksListForm.LoadTitleBlocks(titleBlockListSymbols);
                //titleBlocksListForm.ShowDialog();

                //// Get the selected title block from the form
                //var selectedTitleBlock = titleBlocksListForm.GetSelectedTitleBlock();
                //if (selectedTitleBlock == null)
                //    return Result.Failed;

                //var selectedFamilyName = selectedTitleBlock.FamilyName;
                ////var titleBlock = GetTitleBlockFamilyByName(doc, selectedFamilyName);

                var titleBlock = GetTitleBlockFamilyByName(doc, selectedTitleBlockFam.FamilyName);

                // Step 1: Retrieve the title block family symbol
                //var titleBlock = GetTitleBlockFamilyByName(doc, "ACCO TITLE BLOCK");
                if (titleBlock == null)
                {
                    message = "Title block not found.";
                    return Result.Failed;
                }




                // Step 2: Add the parameter to the title block family
                //Document familyDoc = AddParameterToTitleBlockFamily(doc, titleBlock, listOfNewParamNames, out message);
                Document familyDoc = AddParameterToTitleBlockFamily(doc, titleBlock, selectedScopeBoxNames, out message);

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
            // writeline ~11001100 outputs the value of: 
            //Console.WriteLine(~11001100);// -11001101. This is called bitwise NOT operator. It inverts the bits of its operand.
            return Result.Succeeded;
        }

        private bool ValidateBIMSetupViewHasScopeBoxes(List<string> listOfNewParamNames)
        {
            if (string.IsNullOrEmpty(SetupViewName))
            {
                TaskDialog.Show("Error", "No \"BIM Setup View\" found");
                return false;
            }
            if (!listOfNewParamNames.Any())
            {
                TaskDialog.Show("Error", $"No Scope Boxes found in the view: \n {SetupViewName}");
                return false;
            }
            return true;
        }


        // Example method to get title block family symbols
        public List<TitleBlockInfo> GetTitleBlockFamilySymbolsT(Document doc)
        {
            // Use FilteredElementCollector to find title block family symbols in the document
            var titleBlocks = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_TitleBlocks)
                .OfClass(typeof(FamilySymbol))
                .Cast<FamilySymbol>()
                .Select(ts => new TitleBlockInfo(ts)) // Use the constructor to create TitleBlockInfo instances
                .ToList();

            return titleBlocks;
        }

        //public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        //{
        //    // Get the active application and document
        //    UIApplication uiapp = commandData.Application;
        //    UIDocument uidoc = uiapp.ActiveUIDocument;
        //    Document doc = uidoc.Document;

        //    try
        //    {
        //        // Step 1: Retrieve the title block family symbol
        //        var titleBlock = GetTitleBlockFamilyByName(doc, "ACCO TITLE BLOCK");
        //        if (titleBlock == null)
        //        {
        //            message = "Title block not found.";
        //            return Result.Failed;
        //        }

        //        //---------
        //        // list of YesNo parameter names to add to the TitleBlock Family
        //        //var listOfNewParamNames = new List<string>() { "Area 1", "Area 2", "Area 3" }; // hard coded list of parameter names

        //        // get all the elemements Ids from the view "BIM Setup View - " then get all the elements Category.Name == "Scope Boxes"
        //        List<string> listOfNewParamNames = GetAllScopeBoxesFromView(doc, "BIM Setup View - "); // dynamic list of parameter names
        //        //---------


        //        // Step 2: Add the parameter to the title block family
        //        Document familyDoc = AddParameterToTitleBlockFamily(doc, titleBlock, listOfNewParamNames, out message);

        //        if (familyDoc == null)
        //        {
        //            message = "Error adding parameter to title block family.";
        //            return Result.Failed;
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        TaskDialog.Show("info", e.Message);
        //    }

        //    return Result.Succeeded;
        //}

        private List<string> GetAllScopeBoxesFromParentBIMSetupView(Document doc, string viewName)
        {
            //Find the view by its name
            View targetView = new FilteredElementCollector(doc)
                .OfClass(typeof(View))
                .Cast<View>()
                .FirstOrDefault(view => view.GetPrimaryViewId() == ElementId.InvalidElementId &&
                                view.Name != null &&
                                view.Name.StartsWith(viewName));



            if (targetView != null)
            {
                // update global variable with view name
                SetupViewName = targetView.Name;

                // Get all elements in the target view with Category Name "Scope Boxes"
                var ListOfScopeBoxNames = new FilteredElementCollector(doc, targetView.Id)
                    .OfCategory(BuiltInCategory.OST_VolumeOfInterest)
                    .WhereElementIsNotElementType()
                    .Select(i => i.Name)
                    .ToList();

                // return list of scope box names
                return ListOfScopeBoxNames;
            }
            else
            {
                // Return an empty list if the view is not found
                return new List<string>();
            }
        }
        private List<string> GetAllScopeBoxesFromView(Document doc, string viewName)
        {
            //Find the view by its name
            View targetView = new FilteredElementCollector(doc)
                .OfClass(typeof(View))
                .Cast<View>()
                .FirstOrDefault(view => view.Name.StartsWith(viewName));


            // update global variable with view name
            SetupViewName = targetView.Name;

            if (targetView != null)
            {
                // Get all elements in the target view with Category Name "Scope Boxes"
                var ListOfScopeBoxNames = new FilteredElementCollector(doc, targetView.Id)
                    .OfCategory(BuiltInCategory.OST_VolumeOfInterest)
                    .WhereElementIsNotElementType()
                    .Select(i => i.Name)
                    .ToList();

                // return list of scope box names
                return ListOfScopeBoxNames;
            }
            else
            {
                // Return an empty list if the view is not found
                return new List<string>();
            }
        }


        private Document AddParameterToTitleBlockFamily2(Document doc, Family titleBlock, List<string> listOfNewParamNames, out string message)
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
                    List<FamilyParameter> ListOfNewParmeters = new List<FamilyParameter>();
                    // Define the new parameter name
                    foreach (var newParameterName in listOfNewParamNames)
                    {
                        // Check if the parameter already exists
                        FamilyParameter existingParameter = familyManager.get_Parameter(newParameterName);
                        if (existingParameter != null)
                        {
                            ListOfNewParmeters.Add(existingParameter);
                            continue;
                        }

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

                        // Method to add a fill region to the title block family and link its visibility to the newParameterName 
                        //LinkFilledRegionVisibility(familyDoc, newParameter);
                        ListOfNewParmeters.Add(newParameter);
                    }
                    LinkMultipleFilledRegions(familyDoc, ListOfNewParmeters);
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
                TaskDialog.Show("info-", ex.Message);
                message = $"Error: {ex.Message}";
                return null;
            }
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


                    // Add a Yes/No parameter, visibility set to 1 = true, 0 = false
                    int isVisibleInt = 0;

                    // Set the parameter as instance type
                    bool isInstance = true;
                    List<FamilyParameter> ListOfNewParmeters = new List<FamilyParameter>();
                    // Define the new parameter name
                    foreach (var newParameterName in listOfNewParamNames)
                    {
                        // if the parameter already exists, set the newParameter to the existing parameter
                        FamilyParameter existingParameter = familyManager.get_Parameter(newParameterName);

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
                                familyManager.Set(newParameter, isVisibleInt); // Set to 'false' (unchecked)
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
                            familyManager.Set(newParameter, isVisibleInt); // Set to 'false' (unchecked)
                        }
#endif

                        // Method to add a fill region to the title block family and link its visibility to the newParameterName 
                        //LinkFilledRegionVisibility(familyDoc, newParameter);
                        ListOfNewParmeters.Add(newParameter);
                    }
                    //LinkMultipleFilledRegions(familyDoc, ListOfNewParmeters);
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
                TaskDialog.Show("info-", ex.Message);
                message = $"Error: {ex.Message}";
                return null;
            }
        }

        private void LinkMultipleFilledRegions(Document familyDoc, List<FamilyParameter> parameters)
        {
            FamilyManager familyManager = familyDoc.FamilyManager;
            if (familyManager == null)
            {
                TaskDialog.Show("Error", "FamilyManager not available in the family document.");
                return;
            }

            if (parameters == null || parameters.Count == 0)
            {
                TaskDialog.Show("Error", "No parameters provided.");
                return;
            }

            // Define a precise starting position (adjust based on the actual coordinates you need)
            XYZ referencePoint = new XYZ(3.7, 1.6, 0);  // Adjust these coordinates according to your family layout

            double squareSize = 1.0 / 12;  // Define the size of each square
            double spacing = 0.01;        // Define spacing between squares
            double startX = referencePoint.X + spacing;  // Use the manually defined reference point for X coordinate

            for (int i = 0; i < parameters.Count; i++)
            {
                double offsetX = startX + i * (squareSize + spacing);

                XYZ topLeftPoint = new XYZ(offsetX, referencePoint.Y + squareSize, 0);
                XYZ topRightPoint = new XYZ(offsetX + squareSize, referencePoint.Y + squareSize, 0);
                XYZ bottomRightPoint = new XYZ(offsetX + squareSize, referencePoint.Y, 0);
                XYZ bottomLeftPoint = new XYZ(offsetX, referencePoint.Y, 0);
                IList<CurveLoop> loopList = CreateLinesLoop(topLeftPoint, topRightPoint, bottomRightPoint, bottomLeftPoint);

                // Find the filled region type to use
                FilledRegionType filledRegionType = new FilteredElementCollector(familyDoc)
                    .OfClass(typeof(FilledRegionType))
                    .Cast<FilledRegionType>()
                    .FirstOrDefault();

                if (filledRegionType == null)
                {
                    TaskDialog.Show("Error", "No FilledRegionType found in the document.");
                    return;
                }

                // Find a view that can be used for placing the filled region
                View view = new FilteredElementCollector(familyDoc)
                    .OfClass(typeof(View))
                    .Cast<View>()
                    .FirstOrDefault(v => v.CanBePrinted && !v.IsTemplate);

                if (view == null)
                {
                    TaskDialog.Show("Error", "No suitable view found in the family document to create a Filled Region.");
                    return;
                }

                try
                {
                    // Create the filled region in the document
                    FilledRegion filledRegion = FilledRegion.Create(familyDoc, filledRegionType.Id, view.Id, loopList);

                    if (filledRegion != null)
                    {
                        // Associate the visibility parameter of the filled region with the family parameter
                        Parameter elementVisibilityParam = filledRegion.LookupParameter("Visible");
                        if (elementVisibilityParam != null)
                        {
                            familyManager.AssociateElementParameterToFamilyParameter(elementVisibilityParam, parameters[i]);
                        }
                        else
                        {
                            TaskDialog.Show("Error", "FilledRegion does not have a visibility parameter.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    TaskDialog.Show("Error", $"Failed to create Filled Region: {ex.Message}");
                }
            }
        }

        private static IList<CurveLoop> CreateLinesLoop(XYZ topLeft, XYZ topRight, XYZ bottomRight, XYZ bottomLeft)
        {
            CurveLoop loop = new CurveLoop();
            loop.Append(Line.CreateBound(topLeft, topRight));
            loop.Append(Line.CreateBound(topRight, bottomRight));
            loop.Append(Line.CreateBound(bottomRight, bottomLeft));
            loop.Append(Line.CreateBound(bottomLeft, topLeft));

            IList<CurveLoop> loopList = new List<CurveLoop> { loop };
            return loopList;
        }


        private void LinkMultipleFilledRegions2(Document familyDoc, List<FamilyParameter> parameters)
        {
            FamilyManager familyManager = familyDoc.FamilyManager;
            if (familyManager == null)
            {
                TaskDialog.Show("Error", "FamilyManager not available in the family document.");
                return;
            }

            if (parameters == null || parameters.Count == 0)
            {
                TaskDialog.Show("Error", "No parameters provided.");
                return;
            }

            // Find the Key Plan Border element by its type name
            var keyPlanBorder = new FilteredElementCollector(familyDoc)
                .OfCategory(BuiltInCategory.OST_GenericAnnotation)
                .WhereElementIsElementType()
                .Where(i => i.Name == "Key Plan Border")
                .FirstOrDefault();


            if (keyPlanBorder == null)
            {
                TaskDialog.Show("Error", "Key Plan Border not found.");
                return;
            }

            var keyPlanBorderBB = keyPlanBorder.get_BoundingBox(null);

            if (keyPlanBorderBB == null)
            {
                TaskDialog.Show("Error", "BoundingBox not available for Key Plan Border.");
                return;
            }

            double squareSize = 1.0 / 8;  // Define the size of each square
            double spacing = 0.01;     // Define spacing between squares
            double startX = keyPlanBorderBB.Max.X + spacing;

            for (int i = 0; i < parameters.Count; i++)
            {
                double offsetX = startX + i * (squareSize + spacing);

                XYZ topLeftPoint = new XYZ(offsetX, keyPlanBorderBB.Min.Y + squareSize, 0);
                XYZ topRightPoint = new XYZ(offsetX + squareSize, keyPlanBorderBB.Min.Y + squareSize, 0);
                XYZ bottomRightPoint = new XYZ(offsetX + squareSize, keyPlanBorderBB.Min.Y, 0);
                XYZ bottomLeftPoint = new XYZ(offsetX, keyPlanBorderBB.Min.Y, 0);
                IList<CurveLoop> loopList = CreateLinesLoop(topLeftPoint, topRightPoint, bottomRightPoint, bottomLeftPoint);

                FilledRegionType filledRegionType = new FilteredElementCollector(familyDoc)
                    .OfClass(typeof(FilledRegionType))
                    .Cast<FilledRegionType>()
                    .FirstOrDefault();

                if (filledRegionType == null)
                {
                    TaskDialog.Show("Error", "No FilledRegionType found in the document.");
                    return;
                }

                View view = new FilteredElementCollector(familyDoc)
                    .OfClass(typeof(View))
                    .Cast<View>()
                    .FirstOrDefault(v => v.CanBePrinted && !v.IsTemplate);

                if (view == null)
                {
                    TaskDialog.Show("Error", "No suitable view found in the family document to create a Filled Region.");
                    return;
                }

                try
                {
                    FilledRegion filledRegion = FilledRegion.Create(familyDoc, filledRegionType.Id, view.Id, loopList);

                    if (filledRegion != null)
                    {
                        Parameter elementVisibilityParam = filledRegion.LookupParameter("Visible");
                        if (elementVisibilityParam != null)
                        {
                            familyManager.AssociateElementParameterToFamilyParameter(elementVisibilityParam, parameters[i]);
                        }
                        else
                        {
                            TaskDialog.Show("Error", "FilledRegion does not have a visibility parameter.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    TaskDialog.Show("Error", $"Failed to create Filled Region: {ex.Message}");
                }
            }
        }

        private static IList<CurveLoop> CreateLinesLoop2(XYZ topLeft, XYZ topRight, XYZ bottomRight, XYZ bottomLeft)
        {
            CurveLoop loop = new CurveLoop();
            loop.Append(Line.CreateBound(topLeft, topRight));
            loop.Append(Line.CreateBound(topRight, bottomRight));
            loop.Append(Line.CreateBound(bottomRight, bottomLeft));
            loop.Append(Line.CreateBound(bottomLeft, topLeft));

            IList<CurveLoop> loopList = new List<CurveLoop> { loop };
            return loopList;
        }



        //        // this is the main method that adds a Yes/No parameter to the title block family
        //        private Document AddParameterToTitleBlockFamily(Document doc, Family titleBlock, List<string> listOfNewParamNames, out string message)
        //        {
        //            message = string.Empty;

        //            try
        //            {
        //                // Open the title block family for editing
        //                Document familyDoc = doc.EditFamily(titleBlock);

        //                // Start a transaction to add the parameter
        //                using (Transaction trans = new Transaction(familyDoc, "Add Yes/No Parameter"))
        //                {
        //                    trans.Start();  // ---------- Start the transaction ----------

        //                    FamilyManager familyManager = familyDoc.FamilyManager;
        //                    if (familyManager == null)
        //                    {
        //                        message = "FamilyManager not available in the family document.";
        //                        return null;
        //                    }


        //                    // Add a Yes/No parameter, with visibility set to false

        //                    // Set the parameter as instance type
        //                    bool isInstance = true;

        //                    // Define the new parameter name
        //                    foreach (var newParameterName in listOfNewParamNames)
        //                    {
        //#if REVIT2021
        //                        // Define a new parameter group
        //                        BuiltInParameterGroup parameterGroup = BuiltInParameterGroup.PG_VISIBILITY;

        //                        // Revit 2021 uses ParameterType
        //                        ParameterType parameterType = ParameterType.YesNo;

        //                        // Create a new family parameter for Revit 2021
        //                        FamilyParameter newParameter = familyManager.AddParameter(
        //                            newParameterName,
        //                            parameterGroup,
        //                            parameterType,
        //                            isInstance
        //                        );

        //                        // start a sub-transaction to set the default value of the parameter
        //                        using (SubTransaction subTrans = new SubTransaction(familyDoc))
        //                        {
        //                            subTrans.Start();  // ---------- Start the sub-transaction ----------
        //                                               // Set the parameter's default value to false (unchecked)
        //                            if (parameterType == ParameterType.YesNo)
        //                            {
        //                                familyManager.Set(newParameter, 0); // Set to 'false' (unchecked)
        //                            }
        //                            subTrans.Commit();  // ---------- Commit the sub-transaction ----------
        //                        }

        //#elif REVIT2022 || REVIT2023 || REVIT2024
        //                        // Revit 2022 and newer use ForgeTypeId for the parameter type but BuiltInParameterGroup for the group
        //                        ForgeTypeId parameterYesNoTypeId = SpecTypeId.Boolean.YesNo;  // This is the Yes/No type parameter
        //                        ForgeTypeId groupTypeId = GroupTypeId.Visibility;  // This is the Visibility group

        //                        // Get the family category
        //                        var familyCategory = familyDoc.OwnerFamily.FamilyCategory;

        //                        // Create a new family parameter for Revit 2022 and newer
        //                        FamilyParameter newParameter = familyManager.AddParameter(
        //                            newParameterName,
        //                            groupTypeId,
        //                            parameterYesNoTypeId,
        //                            isInstance
        //                        );

        //                        // Set the parameter's default value to false (unchecked)
        //                        if (parameterYesNoTypeId == SpecTypeId.Boolean.YesNo)
        //                        {
        //                            familyManager.Set(newParameter, 0); // Set to 'false' (unchecked)
        //                        }
        //#endif
        //                    }
        //                    //----------------------
        //                    IFamilyLoadOptions famLoadOptions = new FamilyLoadOptions();
        //                    famLoadOptions.OnFamilyFound(true, out bool overwriteParameterValues);
        //                    familyDoc.LoadFamily(doc, famLoadOptions);
        //                    //----------------------
        //                    trans.Commit();
        //                }
        //                return familyDoc;
        //            }
        //            catch (Exception ex)
        //            {
        //                message = $"Error: {ex.Message}";
        //                return null;
        //            }
        //        }


        //// This is not tested yet
        // Assuming familyDoc is the Document for the title block family and the parameter name is "Area 1"
        //public void LinkFilledRegionVisibility(Document familyDoc, string parameterName)
        //{
        //    // Start a transaction to make changes
        //    using (Transaction trans = new Transaction(familyDoc, "Link Visibility Parameter"))
        //    {
        //        trans.Start();

        //        // Get the FamilyManager to work with family parameters
        //        FamilyManager familyManager = familyDoc.FamilyManager;
        //        if (familyManager == null)
        //        {
        //            TaskDialog.Show("Error", "FamilyManager not available in the family document.");
        //            return;
        //        }

        //        // Retrieve the parameter by name
        //        FamilyParameter visibilityParam = familyManager.get_Parameter(parameterName);
        //        if (visibilityParam == null)
        //        {
        //            TaskDialog.Show("Error", $"Parameter '{parameterName}' not found.");
        //            return;
        //        }

        //        // Collect all FilledRegion elements in the family document
        //        FilteredElementCollector collector = new FilteredElementCollector(familyDoc)
        //            .OfClass(typeof(FilledRegion));

        //        foreach (FilledRegion filledRegion in collector)
        //        {
        //            // Example: Link the first FilledRegion's visibility to the parameter
        //            // Use SetElementIdParameter or AssociateParameter if available
        //            Parameter visibleParam = filledRegion.get_Parameter(BuiltInParameter.VISIBLE_PARAM);
        //            if (visibleParam != null)
        //            {
        //                // Associate the Yes/No parameter to the FilledRegion's visibility
        //                visibleParam.AssociateWithFamilyParameter(visibilityParam);
        //            }
        //        }

        //        trans.Commit();
        //    }
        //}


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

        private static Family GetTitleBlockFamilyByName(Document doc, string famName)
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
