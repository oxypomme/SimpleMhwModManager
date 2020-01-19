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
using System.Diagnostics;
using System.IO;
using Microsoft.Win32;
using System.IO.Compression;

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
            var listContents = new List<(string, bool)>();

            foreach (var item in modListBox.Items)
            {
                listContents.Add(((item as CheckBox).Content.ToString(), (item as CheckBox).IsChecked.Value));
            }

            modListBox.Items.Clear();

            foreach (var mod in App.GetMods())
            {
                var modItem = new CheckBox
                {
                    Content = mod
                };
                modItem.Checked += itemChecked;

                if (listContents.Contains((mod, true)))
                {
                    modItem.IsChecked = true;
                }

                modListBox.Items.Add(modItem);
            }
        }

        private void addMod_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog();
            dialog.DefaultExt = "zip";
            dialog.Filter = "zip files (*.zip)|*.zip|rar files (*.rar)|*.rar";
            if (dialog.ShowDialog() == true)
            {
                ZipFile.ExtractToDirectory(dialog.FileName, "mods/tmp");
                foreach (var dir in Directory.GetDirectories("mods/tmp"))
                {
                    if (dir.Contains("nativePC"))
                    {
                        var name = dialog.FileName.Split('\\');
                        var modName = name[name.GetLength(0) - 1].Split('.')[0];
                        if (!Directory.Exists("mods/" + modName))
                            Directory.Move(dir, @"mods\" + modName);
                        else
                            MessageBox.Show("This mod is already installed", "MHW Mod Manager", MessageBoxButton.OK, MessageBoxImage.Information);
                        Directory.Delete("mods/tmp/", true);
                    }
                }
            }
            UpdateModsList();
        }

        private void remMod_Click(object sender, RoutedEventArgs e)
        {
            foreach (var mod in modListBox.SelectedItems)
                Directory.Delete(@"mods\" + (mod as CheckBox).Content.ToString() + @"\", true);
            UpdateModsList();
        }

        private void startGame_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(App.Settings.settings.mhw_path + "\\MonsterHunterWorld.exe");
        }

        private void refreshMod_Click(object sender, RoutedEventArgs e)
        {
            UpdateModsList();
        }

        private void webMod_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://www.nexusmods.com/monsterhunterworld");
        }

        private void settingsMod_Click(object sender, RoutedEventArgs e)
        {
        }

        private void itemChecked(object sender, RoutedEventArgs e)
        {
        }
    }
}