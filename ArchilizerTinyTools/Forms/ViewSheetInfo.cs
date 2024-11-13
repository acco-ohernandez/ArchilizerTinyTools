using System.Security.RightsManagement;
using System.Windows.Controls;

using Autodesk.Revit.DB;

namespace ArchilizerTinyTools.Forms
{
    public class ViewSheetInfo
    {
        // SheetName
        public string SheetName { get; set; }

        // SheetType (custom parameter or default if unavailable)
        public string SheetType { get; set; }

        // Reference to the actual ViewSheet object
        public ViewSheet ViewSheet { get; set; }

        // Constructor that initializes properties
        public ViewSheetInfo(ViewSheet viewSheet)
        {
            // Set SheetName directly from the ViewSheet
            SheetName = viewSheet.Name;

            // Attempt to retrieve a custom "Sheet Type" parameter
            Parameter sheetTypeParam = viewSheet.LookupParameter("Sheet Type");

            // Set SheetType based on the parameter value or a default value if the parameter is not found
            SheetType = sheetTypeParam != null && sheetTypeParam.HasValue
                ? sheetTypeParam.AsString()
                : "Default Type"; // You can set "Default Type" to any fallback value

            // Store the actual ViewSheet object
            ViewSheet = viewSheet;
        }
    }

}