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

        // OK button click handler
        private void btn_OK_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
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

    // Helper class for Title Block Information
    //public class TitleBlockInfo
    //{
    //    public FamilySymbol FamilySymbol { get; private set; }

    //    public TitleBlockInfo(FamilySymbol familySymbol)
    //    {
    //        FamilySymbol = familySymbol;
    //    }

    //    public override string ToString()
    //    {
    //        return FamilySymbol.Name;
    //    }
    //}
}


/*
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace ArchilizerTinyTools.Forms
{
    /// <summary>
    /// Interaction logic for KeyPlanParameterFromScopeBoxes_Form.xaml
    /// </summary>
    public partial class KeyPlanParameterFromScopeBoxes_Form : Window
    {
        // Observable collections for binding DataGrids
        //private ObservableCollection<ScopeBoxViewInfo> _flattenedScopeBoxList;
        //private ObservableCollection<ViewScopeBoxesInfo> _scopeBoxList;
        private ObservableCollection<string> _scopeBoxNamesList;
        private ObservableCollection<TitleBlockInfo> _titleBlockList;

        private readonly Document _doc;

        //public KeyPlanParameterFromScopeBoxes_Form(Document doc, ObservableCollection<string> scopeBoxNamesList)
        public KeyPlanParameterFromScopeBoxes_Form(Document doc, List<string> scopeBoxNamesList)
        {
            InitializeComponent();

            // Assign the document
            _doc = doc;

            // Initialize collections
            //_scopeBoxList = new ObservableCollection<ViewScopeBoxesInfo>(GetScopeBoxViews(doc));
            _titleBlockList = new ObservableCollection<TitleBlockInfo>(GetTitleBlockSymbols(doc));

            // Convert the list of scope box names to ObservableCollection
            ObservableCollection<string> observableParamNames = new ObservableCollection<string>(scopeBoxNamesList);
            _scopeBoxNamesList = observableParamNames;


            _doc = doc;

            //// Initialize and set the flattened scope box list
            //_flattenedScopeBoxList = new ObservableCollection<ScopeBoxViewInfo>(GetFlattenedScopeBoxList(doc));
            //dg_ScopeBoxes.ItemsSource = _flattenedScopeBoxList;
            dg_ScopeBoxes.ItemsSource = _scopeBoxNamesList;

            // Set the DataContext for DataGrids
            //dg_ScopeBoxes.ItemsSource = _scopeBoxList;
            dg_TitleBlockFamilyName.ItemsSource = _titleBlockList;
        }

        // Method to get all scope boxes in the document
        private List<ViewScopeBoxesInfo> GetScopeBoxViews(Document doc)
        {
            // get the active view
            var activeView = doc.ActiveView;

            var scopeBoxInfoList = new List<ViewScopeBoxesInfo>();

            var scopeBoxInfo = new ViewScopeBoxesInfo(doc, activeView);
            if (scopeBoxInfo.ScopeBoxes.Any())  // Add only if scope boxes exist
            {
                scopeBoxInfoList.Add(scopeBoxInfo);
            }

            return scopeBoxInfoList;
        }

        // Method to get all title block symbols in the document
        private List<TitleBlockInfo> GetTitleBlockSymbols(Document doc)
        {
            var titleBlockSymbols = new FilteredElementCollector(doc)
                .OfClass(typeof(FamilySymbol))
                .OfCategory(BuiltInCategory.OST_TitleBlocks)
                .Cast<FamilySymbol>()
                .Select(famSymbol => new TitleBlockInfo(famSymbol))
                .ToList();

            return titleBlockSymbols;
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
            dg_ScopeBoxes.ItemsSource = filteredScopeBoxes;
        }
        //private void txt_Search_TextChanged2(object sender, TextChangedEventArgs e)
        //{
        //    var searchText = txt_Search.Text.ToLower();

        //    // Filter scope boxes based on search text
        //    var filteredScopeBoxes = _scopeBoxList
        //        .Where(sb => sb.View.Name.ToLower().Contains(searchText) ||
        //                     sb.ScopeBoxes.Any(s => s.Name.ToLower().Contains(searchText)))
        //        .ToList();

        //    // Update DataGrid items
        //    dg_ScopeBoxes.ItemsSource = filteredScopeBoxes;
        //}

        //private void txt_Search_TextChanged(object sender, TextChangedEventArgs e)
        //{
        //    var searchText = txt_Search.Text.ToLower();

        //    // Filter scope boxes based on search text
        //    var filteredScopeBoxes = _scopeBoxList
        //        .Where(sb => sb.view.Name.ToLower().Contains(searchText) ||
        //                     sb.ScopeBoxes.Any(s => s.Name.ToLower().Contains(searchText)))
        //        .ToList();

        //    // Update DataGrid items
        //    dg_ScopeBoxes.ItemsSource = filteredScopeBoxes;
        //}

        // Method to create a flattened list of scope boxes
        private List<ScopeBoxViewInfo> GetFlattenedScopeBoxList(Document doc)
        {
            var scopeBoxViewList = new List<ScopeBoxViewInfo>();
            var views = new FilteredElementCollector(doc)
                .OfClass(typeof(View))
                .Cast<View>()
                .Where(v => !v.IsTemplate);

            foreach (var view in views)
            {
                var scopeBoxes = new FilteredElementCollector(doc, view.Id)
                    .OfCategory(BuiltInCategory.OST_VolumeOfInterest)
                    .WhereElementIsNotElementType()
                    .Where(e => e.Category?.Name == "Scope Boxes")
                    .ToList();

                foreach (var scopeBox in scopeBoxes)
                {
                    scopeBoxViewList.Add(new ScopeBoxViewInfo
                    {
                        ViewName = view.Name,
                        ScopeBoxName = scopeBox.Name
                    });
                }
            }

            return scopeBoxViewList;
        }

        // OK button click handler
        private void btn_OK_Click(object sender, RoutedEventArgs e)
        {
            // Perform action on OK
            this.DialogResult = true;
            this.Close();
        }

        // Cancel button click handler
        private void btn_Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        internal List<string> GetSelectedScopeBoxNames()
        {
            // Return the selected items from the DataGrid as ScopeBoxViewInfo
            return dg_ScopeBoxes.SelectedItems.Cast<string>().ToList();

        }

        internal TitleBlockInfo GetSelectedTitleBlock()
        {
            // Return the selected item from the DataGrid as TitleBlockInfo
            return dg_TitleBlockFamilyName.SelectedItem as TitleBlockInfo;
        }
    }
    public class ScopeBoxViewInfo
    {
        public string ViewName { get; set; }
        public string ScopeBoxName { get; set; }
    }
}
*/