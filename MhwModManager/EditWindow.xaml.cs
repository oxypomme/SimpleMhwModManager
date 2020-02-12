using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MhwModManager
{
    /// <summary>
    /// Logique d'interaction pour EditWindow.xaml
    /// </summary>
    public partial class EditWindow : Window
    {
        private string modPath;
        private int index;
        private int? order;

        public EditWindow(string path)
        {
            InitializeComponent();

            MakeDarkTheme();

            modPath = path;
            int i = 0;
            foreach (var mod in App.Mods)
            {
                if (mod.Item2 == path)
                {
                    index = i;

                    nameTB.Text = mod.Item1.name;
                    nameTB.TextChanged += nameTB_TextChanged;

                    order = mod.Item1.order + 1;
                    orderTB.Text = order.ToString();
                    orderTB.TextChanged += orderTB_TextChanged;

                    break;
                }
                i++;
            }
        }

        private void validateBTN_Click(object sender, RoutedEventArgs e)
        {
            if (order != null)
            {
                foreach (var mod in App.Mods)
                {
                    if (mod.Item1.order == order.Value)
                    {
                        // If the new order is already given, exchange them
                        mod.Item1.order = App.Mods[index].Item1.order;
                        mod.Item1.ParseSettingsJSON(System.IO.Path.Combine(App.ModsPath, mod.Item2));
                        break;
                    }
                }
                App.Mods[index].Item1.order = order.Value;
                App.Mods[index].Item1.ParseSettingsJSON(System.IO.Path.Combine(App.ModsPath, modPath));
                Close();
            }
            else
                MessageBox.Show("The order must be a number !", "Simple MHW Mod Manager", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void MakeDarkTheme()
        {
            var converter = new BrushConverter();
            if (App.Settings.settings.dark_mode)
                Background = (Brush)converter.ConvertFromString("#FF171717");
            else
                Background = (Brush)converter.ConvertFromString("#FFFFFFFF");
        }

        private void cancelBTN_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void nameTB_TextChanged(object sender, TextChangedEventArgs e)
        {
            App.Mods[index].Item1.name = nameTB.Text;
        }

        private void orderTB_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                order = int.Parse(orderTB.Text);
            }
            catch (FormatException)
            {
                order = null;
            }
        }
    }
}