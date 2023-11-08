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
        private void ViewsToSheets_Form_Loaded(object sender, RoutedEventArgs e)
        {
            // Set the initial state of the radio buttons.
            rb_OneToOne.IsChecked = true;
            rb_SigleSheet.IsChecked = false;
        }

        private void rb_OneToOne_Checked(object sender, RoutedEventArgs e)
        {
            lbl_SheetName.Visibility = Visibility.Hidden;
            tb_SheetName.Visibility = Visibility.Hidden;
        }

        private void rb_SigleSheet_Checked(object sender, RoutedEventArgs e)
        {
            lbl_SheetName.Visibility = Visibility.Visible;
            tb_SheetName.Visibility = Visibility.Visible;
        }

        private void btn_Ok_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        private void btn_Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
