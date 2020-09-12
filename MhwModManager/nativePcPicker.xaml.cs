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
    /// Logique d'interaction pour nativePcPicker.xaml
    /// </summary>
    public partial class nativePcPicker : Window
    {
        #region Public Constructors

        public nativePcPicker(IEnumerable<string> values)
        {
            InitializeComponent();
            foreach (var value in values)
                pathList.Items.Add(value);
            if (pathList.Items.Count > 0)
                pathList.SelectedIndex = 0;
            MakeDarkTheme();
        }

        #endregion Public Constructors

        #region Public Properties

        public string Value { get; private set; }

        #endregion Public Properties

        #region Private Methods

        private void installButton_Click(object sender, RoutedEventArgs e)
        {
            Value = pathList.SelectedItem as string;
            DialogResult = true;
        }

        private void MakeDarkTheme()
        {
            var converter = new BrushConverter();
            if (App.Settings.settings.dark_mode)
                Background = (Brush)converter.ConvertFromString("#FF171717");
            else
                Background = (Brush)converter.ConvertFromString("#FFFFFFFF");
        }

        private void pathList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            installButton.IsEnabled = pathList.SelectedIndex != -1;
        }

        #endregion Private Methods
    }
}