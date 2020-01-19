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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;

namespace MhwModManager
{
    /// <summary>
    /// Logique d'interaction pour MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            UpdateModsList();

#if RELEASE
            App.Updater();
#endif
        }

        private void UpdateModsList()
        {
            modListBox.Items.Clear();
            foreach (var mod in App.GetMods())
            {
                var modItem = new CheckBox();
                modItem.Content = mod;
                modListBox.Items.Add(modItem);
            }
        }

        private void addMod_Click(object sender, RoutedEventArgs e)
        {
            UpdateModsList();
        }

        private void remMod_Click(object sender, RoutedEventArgs e)
        {
            foreach (var mod in modListBox.SelectedItems)
                Directory.Delete(@"mods\" + (mod as CheckBox).Content.ToString());
            UpdateModsList();
        }

        private void startGame_Click(object sender, RoutedEventArgs e)
        {
        }

        private void refreshMod_Click(object sender, RoutedEventArgs e)
        {
            UpdateModsList();
        }

        private void webMod_Click(object sender, RoutedEventArgs e)
        {
        }
    }
}