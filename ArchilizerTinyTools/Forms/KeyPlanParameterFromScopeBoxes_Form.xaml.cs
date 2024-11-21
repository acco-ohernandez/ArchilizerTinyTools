using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

using Autodesk.Revit.DB;

namespace ArchilizerTinyTools.Forms
{
    /// <summary>
    /// Interaction logic for KeyPlanParameterFromScopeBoxes_Form.xaml
    /// </summary>
    public partial class KeyPlanParameterFromScopeBoxes_Form : Window
    {
        // Observable collections for binding DataGrids
        private ObservableCollection<string> _scopeBoxNamesList;
        private ObservableCollection<TitleBlockInfo> _titleBlockList;

        private readonly Document _doc;

        // Constructor: Initializes form with a document and a List of scope box names
        public KeyPlanParameterFromScopeBoxes_Form(Document doc, List<string> scopeBoxNamesList)
        {
            InitializeComponent();

            // Attach the KeyDown event handler to the form
            this.KeyDown += KeyPlanParameterFromScopeBoxes_Form_KeyDown;

            // Assign the document
            _doc = doc;

            // Convert the List<string> to ObservableCollection<string>
            _scopeBoxNamesList = new ObservableCollection<string>(scopeBoxNamesList);

            // Initialize the title block list using the existing GetTitleBlockSymbols method
            //_titleBlockList = new ObservableCollection<TitleBlockInfo>(GetTitleBlockSymbols(doc));
            _titleBlockList = new ObservableCollection<TitleBlockInfo>(GetTitleBlockUniqueFamilyNames(doc));

            // Bind DataGrids to collections
            dg_ScopeBoxes.ItemsSource = _scopeBoxNamesList;
            dg_TitleBlockFamilyName.ItemsSource = _titleBlockList;
        }

        // Method to retrieve all title block symbols in the document (unchanged)
        private List<TitleBlockInfo> GetTitleBlockSymbols(Document doc)
        {
            return new FilteredElementCollector(doc)
                .OfClass(typeof(FamilySymbol))
                .OfCategory(BuiltInCategory.OST_TitleBlocks)
                .Cast<FamilySymbol>()
                .Select(famSymbol => new TitleBlockInfo(famSymbol))
                .ToList();
        }
        private List<TitleBlockInfo> GetTitleBlockUniqueFamilyNames(Document doc)
        {
            return new FilteredElementCollector(doc)
                .OfClass(typeof(FamilySymbol))
                .OfCategory(BuiltInCategory.OST_TitleBlocks)
                .Cast<FamilySymbol>()
                .GroupBy(famSymbol => famSymbol.Family.Name) // Group by family name
                .Select(group => new TitleBlockInfo(group.First())) // Select the first unique family symbol
                .ToList();
        }



        // Event handler for Search Box TextChanged
        private void txt_Search_TextChanged(object sender, TextChangedEventArgs e)
        {
            var searchText = txt_Search.Text.ToLower();

            // Filter scope boxes based on search text
            var filteredScopeBoxes = _scopeBoxNamesList
                .Where(sb => sb.ToLower().Contains(searchText))
                .ToList();

            // Update DataGrid items
            dg_ScopeBoxes.ItemsSource = new ObservableCollection<string>(filteredScopeBoxes);
        }

        private void dataGrids_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // check if dg_ScopeBoxes has any selected items
            // check if dg_TitleBlockFamilyName has any selected items
            // if both are selected, enable the OK button
            btn_OK.IsEnabled = dg_ScopeBoxes.SelectedItems.Count > 0 && dg_TitleBlockFamilyName.SelectedItem != null;
        }
        // OK button click handler
        private void btn_OK_Click(object sender, RoutedEventArgs e)
        {
            // if user has not selected any dg_ScopeBoxes items, show a message box and return
            if (dg_ScopeBoxes.SelectedItems.Count == 0)
            {
                MessageBox.Show("Please select at least one Scope Box.", "No Scope Box Selected", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            // if user has not selected any dg_TitleBlockFamilyName items, show a message box and return
            if (dg_TitleBlockFamilyName.SelectedItem == null)
            {
                MessageBox.Show("Please select a Title Block Family Name.", "No Title Block Selected", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            this.DialogResult = true;
            this.Close();
        }

        // add ability to scape to close the form
        private void KeyPlanParameterFromScopeBoxes_Form_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Escape)
            {
                this.DialogResult = false;
                this.Close();
            }
        }

        // Cancel button click handler
        private void btn_Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        // Retrieve selected scope box names from DataGrid
        internal List<string> GetSelectedScopeBoxNames()
        {
            return dg_ScopeBoxes.SelectedItems.Cast<string>().ToList();
        }

        // Retrieve selected title block from DataGrid
        internal TitleBlockInfo GetSelectedTitleBlock()
        {
            return dg_TitleBlockFamilyName.SelectedItem as TitleBlockInfo;
        }
    }

}

