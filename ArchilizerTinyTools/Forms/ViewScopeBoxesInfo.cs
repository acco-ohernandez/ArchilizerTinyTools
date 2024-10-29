using System.Collections.Generic;
using System.Linq;

using Autodesk.Revit.DB;

namespace ArchilizerTinyTools.Forms
{
    internal class ViewScopeBoxesInfo
    {
        public View View { get; set; }
        public List<Element> ScopeBoxes { get; set; }

        // Property to expose individual scope box names
        public IEnumerable<string> ScopeBoxNames => ScopeBoxes.Select(sb => sb.Name);

        public ViewScopeBoxesInfo(Document doc, View curView)
        {
            View = curView;
            ScopeBoxes = GetViewScopeBoxes(doc, curView);
        }

        private List<Element> GetViewScopeBoxes(Document doc, View curView)
        {
            // FilteredElementCollector to get all scope boxes in the current view
            var scopeBoxes = new FilteredElementCollector(doc, curView.Id)
                .OfCategory(BuiltInCategory.OST_VolumeOfInterest)
                .WhereElementIsNotElementType()
                .Where(e => e != null && e.Category != null && e.Category.Name == "Scope Boxes")
                .ToList();

            return scopeBoxes;
        }
    }


}
