using Autodesk.Revit.DB;

namespace ArchilizerTinyTools.Forms
{
    public class YesNoParamInfo
    {
        public string ParamName { get; set; }
        public ElementId Value { get; set; }
        public string SheetName { get; set; }
        public string SheetNumber { get; set; }
    }
}