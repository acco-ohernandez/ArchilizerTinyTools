#region Namespaces
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

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

            // "C:\Users\ohernandez\Videos\ArchSmarter Revit Plugins Course\03 - Creating Views and Sheets\02 - Challenge\Creating Views and Sheets - Challenge Solution.mp4"

            #region FocusedCode
            // Collect all views in the document
            var views = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Views);

            // Find the "1 - Mech" view by its name
            var view_1Mech = views.Cast<View>().Where(v => v.Name == "1 - Mech").First();

            // Collect all title blocks that are element types
            var titleBlocksCollector = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_TitleBlocks).WhereElementIsElementType();

            // Find the "30x42" title block by its name
            var view30x42 = titleBlocksCollector.Where(t => t.Name == "30x42").First();

            // Start a new transaction to create new sheets and viewports
            using (Transaction t = new Transaction(doc))
            {
                t.Start("Create Sheets and Viewports");

                // Create a new sheet
                var newSheet = ViewSheet.Create(doc, view30x42.Id);
                newSheet.Name = "My New Sheet";

                // Create a new viewport on the new sheet and place the "1 - Mech" view
                var newViewPort = Viewport.Create(doc, newSheet.Id, view_1Mech.Id, new XYZ(1, 1, 0));

                // Commit the transaction to save the changes
                t.Commit();
            }
            #endregion

            return Result.Succeeded;
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
