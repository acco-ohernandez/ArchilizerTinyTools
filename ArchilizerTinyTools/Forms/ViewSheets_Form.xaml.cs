using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

using Autodesk.Revit.DB;

namespace ArchilizerTinyTools.Forms
{

    /// <summary>
    /// Interaction logic for ViewSheets_Form.xaml
    /// </summary>
    public partial class ViewSheets_Form : Window
    {
        public ObservableCollection<ViewSheetInfo> viewSheetsObsCol = new ObservableCollection<ViewSheetInfo>();
        private bool sheetsSelected;

        public ViewSheets_Form(List<ViewSheet> viewSheets)
        {
            InitializeComponent();
            AddViewSheetsToObservableCollection(viewSheets);
        }

        public void AddViewSheetsToObservableCollection(List<ViewSheet> viewSheets)
        {
            foreach (ViewSheet viewSheet in viewSheets)
            {
                viewSheetsObsCol.Add(new ViewSheetInfo(viewSheet));
            }
            dg_ViewSheets.ItemsSource = viewSheetsObsCol;
        }


        // get the selected ViewSheetInfo object from the DataGrid and return a list of ViewSheets
        public List<ViewSheet> GetSelectedViewSheets()
        {
            List<ViewSheet> selectedViewSheets = new List<ViewSheet>();
            foreach (ViewSheetInfo viewSheetInfo in dg_ViewSheets.SelectedItems)
            {
                selectedViewSheets.Add(viewSheetInfo.ViewSheet);
            }
            return selectedViewSheets;
        }

        private void dg_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // Set e.Handled to true to indicate that the event is handled and no further action should be taken
            e.Handled = true;
        }

        private void dg_ViewSheets_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            sheetsSelected = true;
            if (sheetsSelected)
                btn_OK.IsEnabled = true;
        }
        private void btn_OK_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }
    }
}
