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
using System.Diagnostics;
using System.IO;
using WinForms = System.Windows.Forms;
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
            modListBox.Items.Clear();

            int i = 0;
            foreach (var mod in App.GetMods())
            {
                var modItem = new CheckBox
                {
                    Tag = i, //Tag is the id of the checkbox and the mod
                    Content = mod.Item1
                };
                modItem.Checked += itemChecked;
                modItem.Unchecked += itemChecked;
                Style style = Application.Current.FindResource("CheckBoxListItem") as Style;
                modItem.Style = style;
                (modItem.ContextMenu.Items[0] as MenuItem).Click -= remModContext_Click;
                (modItem.ContextMenu.Items[0] as MenuItem).Click += remModContext_Click;

                modItem.IsChecked = mod.Item2;

                modListBox.Items.Add(modItem);

                i++;
            }
            App.Settings.ParseSettingsJSON();
        }

        private void addMod_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new WinForms.OpenFileDialog();
            var tmpFolder = Path.Combine(Path.GetTempPath(), "SMMMaddMod");
            if (!Directory.Exists(tmpFolder))
                Directory.CreateDirectory(tmpFolder);
            dialog.DefaultExt = "zip";
            dialog.Filter = "zip files (*.zip)|*.zip|rar files (*.rar)|*.rar|all files|*";
            if (dialog.ShowDialog() == WinForms.DialogResult.OK)
            {
                var name = dialog.FileName.Split('\\');
                ZipFile.ExtractToDirectory(dialog.FileName, tmpFolder);
                if (!InstallMod(tmpFolder, name))
                    MessageBox.Show("nativePC not found... Please check if it's exist in the mod...", "Simple MHW Mod Manager", MessageBoxButton.OK, MessageBoxImage.Error);
                Directory.Delete(tmpFolder, true);
                var mods = App.GetMods();
                for (int i = 0; i < mods.Count; i++)
                {
                    if (mods[i].Item1.Contains(name[name.GetLength(0) - 1].Split('.')[0]))
                    {
                        var info = new ModInfo();
                        info.GenInfo(Path.Combine(App.ModsPath, name[name.GetLength(0) - 1].Split('.')[0]), i);
                    }
                }
            }
            UpdateModsList();
        }

        private bool InstallMod(string path, string[] name)
        {
            foreach (var dir in Directory.GetDirectories(path))
            {
                if (dir.Contains("nativePC"))
                {
                    var modName = name[name.GetLength(0) - 1].Split('.')[0];
                    if (!Directory.Exists(Path.Combine(App.ModsPath, modName)))
                        Directory.Move(dir, Path.Combine(App.ModsPath, modName));
                    else
                        MessageBox.Show("This mod is already installed", "Simple MHW Mod Manager", MessageBoxButton.OK, MessageBoxImage.Information);
                    return true;
                }
                else
                {
                    InstallMod(dir, name);
                }
            }
            return false;
        }

        private void remMod_Click(object sender, RoutedEventArgs e)
        {
            foreach (var mod in modListBox.SelectedItems)
            {
                Directory.Delete(Path.Combine(App.ModsPath, (mod as CheckBox).Content.ToString()), true);
            }
            UpdateModsList();
        }

        private void startGame_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(Path.Combine(App.Settings.settings.mhw_path, "MonsterHunterWorld.exe"));
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
            var settingsWindow = new SettingsDialog();
            settingsWindow.ShowDialog();
        }

        private void itemChecked(object sender, RoutedEventArgs e)
        {
            var mod = Path.Combine(App.ModsPath, (sender as CheckBox).Content.ToString());
            if ((sender as CheckBox).IsChecked.Value == true)
                DirectoryCopy(mod, Path.Combine(App.Settings.settings.mhw_path, "nativePC"), true);
            else
            {
                DeleteMod(Path.Combine(App.ModsPath, (sender as CheckBox).Content.ToString()), Path.Combine(App.Settings.settings.mhw_path, "nativePC"));
                CleanNativePC(Path.Combine(App.Settings.settings.mhw_path, "nativePC"));
            }
            var info = new ModInfo();
            info.GenInfo(mod);
            info.activated = (sender as CheckBox).IsChecked.Value;
            info.ParseSettingsJSON(mod);
            App.Settings.ParseSettingsJSON();
        }

        // Credits to https://docs.microsoft.com/fr-fr/dotnet/standard/io/how-to-copy-directories
        private static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
        {
            // Get the subdirectories for the specified directory.
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);

            DirectoryInfo[] dirs = dir.GetDirectories();
            // If the destination directory doesn't exist, create it.
            if (!Directory.Exists(destDirName))
                Directory.CreateDirectory(destDirName);

            // Get the files in the directory and copy them to the new location.
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                string temppath = Path.Combine(destDirName, file.Name);
                if (!file.Name.Contains("mod.info"))
                    if (!File.Exists(temppath))
                        file.CopyTo(temppath, false);
            }

            // If copying subdirectories, copy them and their contents to new location.
            if (copySubDirs)
                foreach (DirectoryInfo subdir in dirs)
                {
                    string temppath = Path.Combine(destDirName, subdir.Name);
                    DirectoryCopy(subdir.FullName, temppath, copySubDirs);
                }
        }

        private static void DeleteMod(string modPath, string folder)
        {
            // Get the subdirectories for the specified directory.
            DirectoryInfo modDir = new DirectoryInfo(modPath);
            DirectoryInfo[] modDirs = modDir.GetDirectories();

            // Get the files in the directory
            FileInfo[] modFiles = modDir.GetFiles();

            DirectoryInfo dir = new DirectoryInfo(folder);
            DirectoryInfo[] dirs = dir.GetDirectories();
            FileInfo[] files = dir.GetFiles();

            foreach (FileInfo modfile in modFiles)
                foreach (FileInfo file in files)
                    if (modfile.Name == file.Name)
                    {
                        file.Delete();
                        break;
                    }

            foreach (DirectoryInfo submoddir in modDirs)
                foreach (DirectoryInfo subdir in dirs)
                    DeleteMod(submoddir.FullName, subdir.FullName);
        }

        private static void CleanNativePC(string folder)
        {
            DirectoryInfo dir = new DirectoryInfo(folder);
            DirectoryInfo[] dirs = dir.GetDirectories();

            foreach (DirectoryInfo subdir in dirs)
            {
                CleanNativePC(subdir.FullName);
                if (!Directory.EnumerateFileSystemEntries(subdir.FullName).Any())
                    Directory.Delete(subdir.FullName);
            }
        }

        private void remModContext_Click(object sender, RoutedEventArgs e)
        {
            var caller = (((sender as MenuItem).Parent as ContextMenu).PlacementTarget as CheckBox);
            Directory.Delete(Path.Combine(App.ModsPath, caller.Content.ToString()), true);
            UpdateModsList();
        }
    }
}