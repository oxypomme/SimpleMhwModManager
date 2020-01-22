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

namespace MhwModManager
{
    /// <summary>
    /// Logique d'interaction pour EditWindow.xaml
    /// </summary>
    public partial class EditWindow : Window
    {
        public EditWindow(string path)
        {
            InitializeComponent();
        }

        private void validateBTN_Click(object sender, RoutedEventArgs e)
        {
        }

        private void cancelBTN_Click(object sender, RoutedEventArgs e)
        {
        }

        private void nameTB_TextChanged(object sender, TextChangedEventArgs e)
        {
        }

        private void orderTB_TextChanged(object sender, TextChangedEventArgs e)
        {
        }
    }
}