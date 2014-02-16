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

namespace Chimer
{
    /// <summary>
    /// Interaction logic for ConfigFileErrorDialog.xaml
    /// </summary>
    public partial class ConfigFileErrorDialog : Window
    {
        public ConfigFileErrorDialog(string errorText)
        {
            InitializeComponent();
            txtError.Text = errorText;
        }

        public bool HideRevert
        {
            set
            {
                btnRevert.Visibility = (value) ? Visibility.Hidden : Visibility.Visible;
            }
        }

        public event Action EditClicked;
        public event Action RevertClicked;

        private void btnEdit_Click(object sender, RoutedEventArgs e)
        {
            EditClicked();
            this.Close();
        }

        private void btnRevert_Click(object sender, RoutedEventArgs e)
        {
            RevertClicked();
            this.Close();
        }

        private void btnLeave_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
