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

        public ViewsToSheets_Form(List<View> views, IEnumerable<FamilySymbol> titleBlocksCollector)
        {
            InitializeComponent();
            // Attach the Loaded event handler to set the radio button's properties.
            this.Loaded += ViewsToSheets_Form_Loaded;
            this.views = views;
            this.titleBlocksCollector = titleBlocksCollector; // Assign the parameter to the class field
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


        //private void btn_Ok_Click(object sender, RoutedEventArgs e)
        //{
        //    this.DialogResult = true;
        //    this.Close();
        //}

        private void btn_Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchText = txtSearch.Text.ToLower();

            // Filter the views based on the search text
            var filteredViews = views.Cast<View>().Where(view => view.Name.ToLower().Contains(searchText)).ToList();

            // Update the DataGrid's item source with the filtered views
            dgViews.ItemsSource = filteredViews.Select(view => view.Name).ToList();
        }


        // Add properties for selected views, title block, and title text
        public List<View> SelectedViews { get; private set; }
        public FamilySymbol SelectedTitleBlock { get; private set; }
        public Element SelectedTitleText { get; private set; }

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

        // Existing code...

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


        private string GetSelectedTitleBlockName()
        {
            string selectedTitleBlock = dgTitleBlocks.SelectedItem?.ToString();

            // If you have a title block object, you might get its name property instead of using ToString
            // string selectedTitleBlock = (dgTitleBlocks.SelectedItem as FamilySymbol)?.Name;

            return selectedTitleBlock;
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


        private Element GetSelectedTitleText()
        {
            //string selectedTitleText = dgTitleText.SelectedItem?.ToString();

            // If you have a title text object, you might get its name property instead of using ToString
            //string selectedTitleText = (dgTitleText.SelectedItem as Element)?.Name;
            Element selectedTitleText = dgTitleText.SelectedItem as Element;

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
    }
}
