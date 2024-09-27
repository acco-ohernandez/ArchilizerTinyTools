using Autodesk.Revit.DB;

namespace ArchilizerTinyTools.Forms
{
    public class TitleBlockInfo
    {
        public FamilySymbol TitleBlockSymbol { get; set; }
        public string TitleBlockName { get; set; }
        public string FamilyName { get; set; }

        public TitleBlockInfo(FamilySymbol titleBlockName)
        {
            TitleBlockSymbol = titleBlockName;
            TitleBlockName = titleBlockName.Name;
            FamilyName = titleBlockName.FamilyName;
        }
    }
}