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
    /// Interaction logic for TitleBlocksListForm.xaml
    /// </summary>
    public partial class TitleBlocksListForm : Window
    {
        // ObservableCollection to hold the list of title blocks
        public ObservableCollection<TitleBlockInfo> TitleBlocksList { get; set; }

        public TitleBlocksListForm()
        {
            InitializeComponent();
            TitleBlocksList = new ObservableCollection<TitleBlockInfo>();
            DataContext = TitleBlocksList; // Set the DataContext to the ObservableCollection
        }

        // Method to load title blocks into the DataGrid
        public void LoadTitleBlocks(List<TitleBlockInfo> titleBlocks)
        {
            TitleBlocksList.Clear();
            foreach (var titleBlock in titleBlocks)
            {
                TitleBlocksList.Add(titleBlock);
            }
        }
        public TitleBlockInfo GetSelectedTitleBlock()
        {
            // Return the selected item from the DataGrid as TitleBlockInfo
            return TitleBlocksDataGrid.SelectedItem as TitleBlockInfo;
        }

        private void btn_OK_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }
    }

}
