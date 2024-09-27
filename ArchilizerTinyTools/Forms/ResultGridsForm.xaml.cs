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

namespace ArchilizerTinyTools.Forms
{
    /// <summary>
    /// Interaction logic for ResultGridsForm.xaml
    /// </summary>
    public partial class ResultGridsForm : Window
    {
        public ResultGridsForm()
        {
            InitializeComponent();
            this.KeyDown += ResultGridsForm_KeyDown; // Add the event handler for the KeyDown event
        }

        private void btn_OK_Click(object sender, RoutedEventArgs e)
        {
            //this.DialogResult = true;
            this.Close();
        }
        // add the ability to close the form if the user presses the escape key or the enter key
        private void ResultGridsForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape || e.Key == Key.Enter)
            {
                //this.DialogResult = true;
                this.Close();
            }
        }
    }

    // Classes for DataGrids
    //// View Not Placed Info
    //public class ViewNotPlacedInfo
    //{
    //    // Sheet Number,Sheet Name,View Name,REASON
    //    public string ViewType { get; set; }
    //    public string SheetNumber { get; set; }
    //    public string SheetName { get; set; }
    //    public string ViewName { get; set; }
    //    public string Reason { get; set; }

    //    public ViewNotPlacedInfo(string viewType, string sheetNumber, string sheetName, string viewName, string reason)
    //    {
    //        ViewType = viewType;
    //        SheetNumber = sheetNumber;
    //        SheetName = sheetName;
    //        ViewName = viewName;
    //        Reason = reason;
    //    }
    //}

    //// Views Succesfully placed on Sheets
    //public class ViewPlacedInfo
    //{
    //    // View Type, Sheet Number,Sheet Name,View Name
    //    public string ViewType { get; set; }
    //    public string SheetNumber { get; set; }
    //    public string SheetName { get; set; }
    //    public string ViewName { get; set; }

    //    public ViewPlacedInfo(string viewType, string sheetNumber, string sheetName, string viewName)
    //    {
    //        ViewType = viewType;
    //        SheetNumber = sheetNumber;
    //        SheetName = sheetName;
    //        ViewName = viewName;
    //    }
    //}

}
