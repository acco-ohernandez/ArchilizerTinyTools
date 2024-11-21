using System;
using System.Collections;
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
            this.KeyDown += ViewSheets_Form_KeyDown;
            AddViewSheetsToObservableCollection(viewSheets);

            // Populate the combobox with unique sheet types
            cb_SheetType.ItemsSource = PopulateTheSheetTypeComboBoxWithUniqueNames(viewSheets);
        }

        private IEnumerable<string> PopulateTheSheetTypeComboBoxWithUniqueNames(List<ViewSheet> viewSheets)
        {
            // Extract unique, non-empty sheet types using LINQ
            var uniqueSheetTypes = viewSheets
                .Select(sheet => sheet.LookupParameter("Sheet Type")?.AsString())
                .Where(sheetType => !string.IsNullOrEmpty(sheetType))
                .Distinct()
                .OrderBy(sheetType => sheetType)
                .ToList();

            // Add "All" to the list if there is more than one unique sheet type
            if (uniqueSheetTypes.Count > 1)
            {
                uniqueSheetTypes.Insert(0, "All");
            }

            // Return the sorted, unique sheet types
            return uniqueSheetTypes;
        }

        private IEnumerable<string> PopulateTheSheetTypeComboBoxWithUniqueNames2(List<ViewSheet> viewSheets)
        {
            // Use a HashSet to collect unique sheet types
            HashSet<string> uniqueSheetTypes = new HashSet<string>();

            // Iterate through the list of ViewSheets
            foreach (var sheet in viewSheets)
            {
                // Retrieve the sheet type (assuming it's stored in a parameter)
                Parameter sheetTypeParam = sheet.LookupParameter("Sheet Type");

                // If the parameter exists and has a value, add it to the HashSet
                if (sheetTypeParam != null && sheetTypeParam.HasValue)
                {
                    string sheetType = sheetTypeParam.AsString();
                    if (!string.IsNullOrEmpty(sheetType))
                    {
                        uniqueSheetTypes.Add(sheetType);
                    }
                }
            }

            // if uniqueSheetTypes has more than one sheet type add "All" as the first item
            if (uniqueSheetTypes.Count > 1)
            {
                uniqueSheetTypes.Add("All");
            }
            // sort the uniqueSheetTypes
            uniqueSheetTypes = new HashSet<string>(uniqueSheetTypes.OrderBy(x => x));


            // Return the unique sheet types as an IEnumerable<string>
            return uniqueSheetTypes;
        }


        // add the ability to cancel the form if the user presses the escape key
        private void ViewSheets_Form_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                this.DialogResult = false;
                this.Close();
            }
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

        private void btn_Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        // Popluate the combobox with the sheet types from the ViewSheetInfo class
        private void cb_SheetType_Loaded(object sender, RoutedEventArgs e)
        {
            // Ensure the ComboBox has items
            if (cb_SheetType.Items.Count == 1)
            {
                // set the selected index to 0
                cb_SheetType.SelectedIndex = 0;
            }
            else
            {
                // set the text to "All"
                cb_SheetType.Text = "All";
            }
        }

        private void cb_SheetType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Get the selected sheet type
            string selectedSheetType = cb_SheetType.SelectedItem as string;

            // Filter the viewSheetsObsCol based on the selected sheet type
            if (selectedSheetType == "All")
            {
                dg_ViewSheets.ItemsSource = viewSheetsObsCol;
            }
            else
            {
                dg_ViewSheets.ItemsSource = viewSheetsObsCol.Where(sheet => sheet.SheetType == selectedSheetType);
            }
        }

        private void tb_SearchSheets_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Get the search text from the TextBox
            string searchText = tb_SearchSheets.Text.ToLower();

            // Get the selected sheet type from the ComboBox
            string selectedSheetType = cb_SheetType.SelectedItem as string;

            // Retrieve the current list to filter (based on the selected sheet type)
            IEnumerable<ViewSheetInfo> currentList;
            if (selectedSheetType == "All" || string.IsNullOrEmpty(selectedSheetType))
            {
                // If "All" is selected, work with the full observable collection
                currentList = viewSheetsObsCol;
            }
            else
            {
                // Otherwise, filter the observable collection by the selected sheet type
                currentList = viewSheetsObsCol.Where(sheet => sheet.SheetType == selectedSheetType);
            }

            // Reset the filter if the search text is empty
            if (string.IsNullOrEmpty(searchText))
            {
                dg_ViewSheets.ItemsSource = currentList.ToList(); // Reset to the current list based on sheet type
                return;
            }

            // Filter the current list based on the search text
            //var filteredViewSheets = currentList
            //    .Where(sheet =>
            //        sheet.SheetName.ToLower().Contains(searchText) || // Filter by SheetName
            //        sheet.SheetType.ToLower().Contains(searchText))   // Filter by SheetType
            //    .ToList();
            var filteredViewSheets = currentList
                .Where(sheet =>
                    sheet.SheetName.ToLower().Contains(searchText)  // Filter by SheetName
                    )
                .ToList();

            // Update the DataGrid's ItemsSource to reflect the filtered results
            dg_ViewSheets.ItemsSource = filteredViewSheets;
        }
    }
}
