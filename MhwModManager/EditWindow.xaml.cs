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

        public EditWindow(ModInfo modInfo)
        {
            InitializeComponent();

            MakeDarkTheme();

            modPath = modInfo.path;

            index = App.Mods.IndexOf(modInfo);

            nameTB.Text = modInfo.name;
            nameTB.TextChanged += nameTB_TextChanged;

            order = modInfo.order + 1;
            //orderTB.Text = order.ToString();
            //orderTB.TextChanged += orderTB_TextChanged;
        }

        private void validateBTN_Click(object sender, RoutedEventArgs e)
        {
            if (order != null)
            {
                foreach (var mod in App.Mods)
                {
                    if (mod.order == order.Value - 1)
                    {
                        // If the new order is already given, exchange them
                        mod.order = App.Mods[index].order;
                        mod.ParseSettingsJSON();
                        break;
                    }
                }
                App.Mods[index].order = order.Value - 1;
                App.Mods[index].ParseSettingsJSON();
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
            App.Mods[index].name = nameTB.Text;
        }

        private void orderTB_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                //order = int.Parse(orderTB.Text);
            }
            catch (FormatException)
            {
                order = null;
            }
        }
    }
}