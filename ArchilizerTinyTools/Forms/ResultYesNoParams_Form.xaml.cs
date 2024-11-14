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
    /// Interaction logic for ResultYesNoParams_Form.xaml
    /// </summary>
    public partial class ResultYesNoParams_Form : Window
    {
        public ResultYesNoParams_Form(List<YesNoParamInfo> yesNoParamResults)
        {
            InitializeComponent();
            this.KeyDown += ResultGridsForm_KeyDown; // Add the event handler for the KeyDown event
            dg_ResultYeNoParams.ItemsSource = yesNoParamResults;
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
}
