using System;
using System.Collections.Generic;
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
    /// Interaction logic for ViewsToSheets_Form.xaml
    /// </summary>
    public partial class ViewsToSheets_Form : Window
    {
        public ViewsToSheets_Form()
        {
            InitializeComponent();
            // Attach the Loaded event handler to set the radio button's properties.
            this.Loaded += ViewsToSheets_Form_Loaded;
        }

        private List<View> views;
        private IEnumerable<FamilySymbol> titleBlocksCollector;

        private List<ViewInfo> originalViewsListInfo; // Add this as a field in your class

        public ViewsToSheets_Form(List<View> views, IEnumerable<FamilySymbol> titleBlocksCollector)
        {
            InitializeComponent();
            // Attach the Loaded event handler to set the radio button's properties.
            this.Loaded += ViewsToSheets_Form_Loaded;
            this.views = views;
            this.titleBlocksCollector = titleBlocksCollector; // Assign the parameter to the class field

            // Save the original list when initializing or loading data
            originalViewsListInfo = views.OrderBy(view => view.Name)
                                     .Select(view => new ViewInfo(view.Name, view.ViewType, view.Id))
                                     .ToList();
        }
        private void ViewsToSheets_Form_Loaded(object sender, RoutedEventArgs e)
        {
            // Set the initial state of the radio buttons.
            rb_OneToOne.IsChecked = true;
            rb_SigleSheet.IsChecked = false;
        }

        private void rb_OneToOne_Checked(object sender, RoutedEventArgs e)
        {
            lbl_SheetName.Visibility = System.Windows.Visibility.Hidden;
            tb_SheetName.Visibility = System.Windows.Visibility.Hidden;
        }

        private void rb_SigleSheet_Checked(object sender, RoutedEventArgs e)
        {
            lbl_SheetName.Visibility = System.Windows.Visibility.Visible;
            tb_SheetName.Visibility = System.Windows.Visibility.Visible;
        }

        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchText = txtSearch.Text.ToLower();

            // Filter the views based on the search text
            var filteredViewInfos = originalViewsListInfo
                .Where(viewInfo => viewInfo.Name.ToLower().Contains(searchText))
                .ToList();

            // Update the DataGrid's item source with the filtered views
            this.dgViews.ItemsSource = filteredViewInfos;
        }

        private void ResetDataGrid()
        {
            // Reset the DataGrid to show the original list of views
            this.dgViews.ItemsSource = originalViewsListInfo;
        }


        // Add properties for selected views, title block, and title text
        public List<View> SelectedViews { get; private set; }
        public FamilySymbol SelectedTitleBlock { get; private set; }
        public string SelectedTitleText { get; private set; }

        private void btn_Ok_Click(object sender, RoutedEventArgs e)
        {
            // Assign the selected views, title block, and title text
            SelectedViews = GetSelectedViews();
            SelectedTitleBlock = GetSelectedTitleBlockFS();
            SelectedTitleText = GetSelectedTitleText();

            // Set DialogResult to true to indicate that the user clicked OK
            DialogResult = true;

            // Close the form
            Close();
        }


        private void btn_Cancel_Click(object sender, RoutedEventArgs e)
        {
            ResetDataGrid(); // Reset the DataGrid before closing the form
            this.DialogResult = false;
            this.Close();
        }

        // Add methods to get selected views, title block, and title text
        private List<View> GetSelectedViews()
        {
            List<View> selectedViews = new List<View>();

            foreach (var selectedItem in dgViews.SelectedItems)
            {
                string viewName = selectedItem.ToString();
                var selectedView = views.FirstOrDefault(view => view.Name == viewName);
                if (selectedView != null)
                {
                    selectedViews.Add(selectedView);
                }
            }

            return selectedViews;
        }

        private FamilySymbol GetSelectedTitleBlockFS()
        {
            string selectedTitleBlockName = dgTitleBlocks.SelectedItem?.ToString();

            if (selectedTitleBlockName != null)
            {
                // Assuming titleBlocksCollector is a list of FamilySymbol
                return titleBlocksCollector.FirstOrDefault(tb => tb.Name == selectedTitleBlockName);
            }

            return null;
        }

        private string GetSelectedTitleText()
        {
            var selectedTitleText = dgTitleText.SelectedItem as string;
            return selectedTitleText;
        }

        public bool OneOrManySheets()
        {
            bool oneToOne = true;
            if (rb_OneToOne.IsChecked == true)
            {
                return oneToOne;
            }
            else
                return false;
        }

        public string GetTheMultipleViewsSheetName()
        {
            return tb_SheetName.Text;
        }

        private void dg_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // Set e.Handled to true to indicate that the event is handled and no further action should be taken
            e.Handled = true;
        }


        #region This section has event handlers to make sure the user makes the necessary selections before clicking OK
        public bool viewsSelected { get; set; }
        public bool titleBlockSelected { get; set; }
        public bool titleTextSelected { get; set; }
        public bool xIsDouble { get; set; }
        public bool yIsDouble { get; set; }

        private void dgViews_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            viewsSelected = true;
            if (viewsSelected & titleBlockSelected & titleTextSelected & xIsDouble)
                btn_Ok.IsEnabled = true;
        }
        private void dgTitleBlocks_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            titleBlockSelected = true;
            if (viewsSelected & titleBlockSelected & titleTextSelected & xIsDouble & yIsDouble)
                btn_Ok.IsEnabled = true;
        }
        private void dgTitleText_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            titleTextSelected = true;
            if (viewsSelected & titleBlockSelected & titleTextSelected & xIsDouble & yIsDouble)
                btn_Ok.IsEnabled = true;
        }
        private void txt_X_TextChanged(object sender, TextChangedEventArgs e)
        {
            xIsDouble = true;
            TextBox textBox = (TextBox)sender;
            if (!double.TryParse(textBox.Text, out double result))
            {
                // If the input is not a valid double, you can clear the text or take other action.
                textBox.Text = ""; // Replace with your desired default value.
                xIsDouble = false;
                btn_Ok.IsEnabled = false;
            }
            else
            {
                if (viewsSelected & titleBlockSelected & titleTextSelected & xIsDouble & yIsDouble)
                    btn_Ok.IsEnabled = true;
            }
        }
        private void txt_Y_TextChanged(object sender, TextChangedEventArgs e)
        {
            yIsDouble = true;
            TextBox textBox = (TextBox)sender;
            if (!double.TryParse(textBox.Text, out double result))
            {
                // If the input is not a valid double, you can clear the text or take other action.
                textBox.Text = ""; // Replace with your desired default value.
                yIsDouble = false;
                btn_Ok.IsEnabled = false;
            }
            else
            {
                if (viewsSelected & titleBlockSelected & titleTextSelected & xIsDouble & yIsDouble)
                    btn_Ok.IsEnabled = true;
            }
        }
        #endregion

    }
}
