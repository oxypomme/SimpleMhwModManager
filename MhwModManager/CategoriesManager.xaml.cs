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
    /// Logique d'interaction pour EditStringBox.xaml
    /// </summary>
    public partial class CategoriesManager : Window
    {
        public CategoriesManager()
        {
            InitializeComponent();

            MakeDarkTheme();
        }

        private void MakeDarkTheme()
        {
            var converter = new BrushConverter();
            if (App.Settings.settings.dark_mode)
                Background = (Brush)converter.ConvertFromString("#FF171717");
            else
                Background = (Brush)converter.ConvertFromString("#FFFFFFFF");
        }

        private void validateBTN_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void cancelBTN_Click(object sender, RoutedEventArgs e)
        {
            nameTB.Text = "";
            Close();
        }
    }
}