using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchilizerTinyTools.Forms
{
    public class SampleViewModel
    {
        public ObservableCollection<SampleRowItems> SampleDataCollection { get; set; }

        public SampleViewModel()
        {
            SampleDataCollection = new ObservableCollection<SampleRowItems>
        {
            new SampleRowItems { ViewType = "Type1", Name = "Sample View 1" },
            new SampleRowItems { ViewType = "Type2", Name = "Sample View 2" }
        };
        }
    }

    public class SampleRowItems
    {
        public string Name { get; set; }
        public string ViewType { get; set; }

        // Parameterless constructor for design-time data creation
        public SampleRowItems() { }

        public SampleRowItems(string name, string viewType)
        {
            Name = name;
            ViewType = viewType;
        }
    }

}
