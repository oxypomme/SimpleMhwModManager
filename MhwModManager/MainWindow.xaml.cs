using SevenZipExtractor;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WinForms = System.Windows.Forms;
using System.IO.Compression;

using System.Windows.Input;

namespace MhwModManager
{
    /// <summary>
    /// Logique d'interaction pour MainWindow.xaml-
    /// </summary>
    public partial class MainWindow : Window
    {
        public static MainWindow Instance;
        private UIElement _dummyDragSource = new UIElement();
        private bool _isDown;
        private bool _isDragging;
        private UIElement _realDragSource;
        private Point _startPoint;
        private bool isDarkTheme = false;

        public MainWindow()
        {
            Instance = this;
            InitializeComponent();

            UpdateModsList();

            App.ReloadTheme();
        }

        public void MakeDarkTheme()
        {
            try
            {
                var converter = new BrushConverter();
                if (App.Settings.settings.dark_mode && !isDarkTheme)
                {
                    Background = (Brush)converter.ConvertFromString("#FF171717");

                    for (int i = 0; i < btnsSP.Children.Count; i++)
                    {
                        Button btn;
                        try
                        {
                            btn = (Button)btnsSP.Children[i];
                        }
                        catch (InvalidCastException) { i++; btn = (Button)btnsSP.Children[i]; }
                        if (btn.Content != null)
                            (btn.Content as Image).Source = Utilities.MakeDarkTheme((btn.Content as Image).Source as BitmapSource);
                    }
                    (startGame.Content as Image).Source = Utilities.MakeDarkTheme((startGame.Content as Image).Source as BitmapSource);
                    isDarkTheme = true;
                }
                else if (!App.Settings.settings.dark_mode && isDarkTheme)
                {
                    Background = (Brush)converter.ConvertFromString("#FFFFFFFF");

                    for (int i = 0; i < btnsSP.Children.Count; i++)
                    {
                        Button btn;
                        try
                        {
                            btn = (Button)btnsSP.Children[i];
                        }
                        catch (InvalidCastException) { i++; btn = (Button)btnsSP.Children[i]; }

                        var icon = new BitmapImage();
                        icon.BeginInit();
                        icon.UriSource = new Uri($"pack://application:,,,/MhwModManager;component/icons/{btn.Name.Replace("Mod", "")}.png", UriKind.RelativeOrAbsolute);
                        icon.EndInit();

                        (btn.Content as Image).Source = icon;

                        isDarkTheme = false;
                    }

                    var startIcon = new BitmapImage();
                    startIcon.BeginInit();
                    startIcon.UriSource = new Uri("pack://application:,,,/MhwModManager;component/icons/launch.png", UriKind.RelativeOrAbsolute);
                    startIcon.EndInit();
                    (startGame.Content as Image).Source = startIcon;
                }
            }
            catch (Exception e) { App.logStream.WriteLine(e.ToString(), "FATAL"); }
        }

        public void UpdateModsList()
        {
            try
            {
                modListBox.Items.Clear();
                App.GetMods();

                foreach (var mod in App.Mods)
                {
                    // Increase the order count if didn't exist
                    if (mod.Item1.order >= App.Mods.Count())
                        mod.Item1.order = App.Mods.Count() - 1;

                    var modItem = new CheckBox
                    {
                        Tag = mod.Item1.order,
                        Content = mod.Item1.name,
                        MinWidth = 80
                    };
                    modItem.IsChecked = mod.Item1.activated;
                    modItem.Checked += itemChecked;
                    modItem.Unchecked += itemChecked;
                    // Adding the context menu
                    var style = Application.Current.FindResource("CheckBoxListItem") as Style;
                    modItem.Style = style;
                    foreach (MenuItem item in modItem.ContextMenu.Items)
                    {
                        if (item.Tag.ToString() == "rem")
                        {
                            item.Click -= remModContext_Click;
                            item.Click += remModContext_Click;
                        }
                        else if (item.Tag.ToString() == "edit")
                        {
                            item.Click -= editModContext_Click;
                            item.Click += editModContext_Click;
                        }
                    }

                    modListBox.Items.Add(modItem);
                }

                // Check if there's mods conflicts
                for (int i = 0; i < App.Mods.Count() - 1; i++)
                    if (!CheckFiles(Path.Combine(App.ModsPath, App.Mods[i].Item2), Path.Combine(App.ModsPath, App.Mods[i + 1].Item2)))
                    {
                        var firstModItem = modListBox.Items[App.Mods[i].Item1.order];
                        var secondModItem = modListBox.Items[App.Mods[i + 1].Item1.order];
                        (firstModItem as CheckBox).Foreground = Brushes.Red;
                        (firstModItem as CheckBox).ToolTip = "Conflict with " + App.Mods[i + 1].Item1.name;
                        (secondModItem as CheckBox).Foreground = Brushes.Red;
                        (secondModItem as CheckBox).ToolTip = "Conflict with " + App.Mods[i].Item1.name;
                    }
            }
            catch (Exception e) { App.logStream.WriteLine(e.Message, "FATAL"); }
        }

        private static void CleanFolder(string folder)
        {
            DirectoryInfo dir = new DirectoryInfo(folder);
            DirectoryInfo[] dirs = dir.GetDirectories();

            foreach (DirectoryInfo subdir in dirs)
            {
                CleanFolder(subdir.FullName);
                if (!Directory.EnumerateFileSystemEntries(subdir.FullName).Any())
                    // If the directory is empty
                    Directory.Delete(subdir.FullName);
            }
        }

        private static void DeleteMod(string modPath, string folder)
        {
            // Get the subdirectories for the mod directory.
            DirectoryInfo modDir = new DirectoryInfo(modPath);
            DirectoryInfo[] modDirs = modDir.GetDirectories();

            // Get the files in the directory
            FileInfo[] modFiles = modDir.GetFiles();

            // Get the subdirectories for the nativePC directory.
            DirectoryInfo dir = new DirectoryInfo(folder);
            DirectoryInfo[] dirs = dir.GetDirectories();

            // Get the files in the directory
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
                if (!file.Name.Equals("mod.info"))
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

        private void addMod_Click(object sender, RoutedEventArgs e)
        {
            App.AddMods();
        }

        private bool CheckFiles(string pathFirstMod, string pathSecondMod)
        {
            // Get the subdirectories for the mod directory.
            DirectoryInfo dirFirstMod = new DirectoryInfo(pathFirstMod);
            DirectoryInfo[] dirsFirstMod = dirFirstMod.GetDirectories();

            // Get the files in the directory
            FileInfo[] filesFirstMod = dirFirstMod.GetFiles();

            // Get the subdirectories for the nativePC directory.
            DirectoryInfo dirSecondMod = new DirectoryInfo(pathSecondMod);
            DirectoryInfo[] dirsSecondMod = dirSecondMod.GetDirectories();

            // Get the files in the directory
            FileInfo[] filesSecondMod = dirSecondMod.GetFiles();

            foreach (FileInfo firstFile in filesFirstMod)
                foreach (FileInfo secondFile in filesSecondMod)
                    if (firstFile.Name == secondFile.Name && firstFile.Name != "mod.info")
                    {
                        return false; // return false if conflict
                    }

            foreach (DirectoryInfo subdirFirstMod in dirsFirstMod)
                foreach (DirectoryInfo subdirSecondMod in dirsSecondMod)
                    return CheckFiles(subdirFirstMod.FullName, subdirSecondMod.FullName);

            return true; // return true if everything's fine
        }

        private void editMod((ModInfo, string) modInfo)
        {
            var editWindow = new EditWindow(modInfo);
            editWindow.Owner = Application.Current.MainWindow;
            editWindow.ShowDialog();
            UpdateModsList();
        }

        private void editModContext_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var caller = (((sender as MenuItem).Parent as ContextMenu).PlacementTarget as CheckBox);
                editMod(App.Mods[(caller.Tag as int?).Value]);
            }
            catch (Exception ex) { App.logStream.WriteLine(ex.ToString(), "FATAL"); }
        }

        private void itemChecked(object sender, RoutedEventArgs e)
        {
            try
            {
                // Get the full path of the mod
                var index = int.Parse((sender as CheckBox).Tag.ToString());
                var mod = Path.Combine(App.ModsPath, App.Mods[index].Item2);

                if ((sender as CheckBox).IsChecked.Value == true)
                {
                    // Install the mod
                    DirectoryCopy(mod, Path.Combine(App.Settings.settings.mhw_path, "nativePC"), true);
                    App.logStream.WriteLine($"{App.Mods[index].Item1.name} installed");
                }
                else
                {
                    // Desinstall the mod
                    DeleteMod(mod, Path.Combine(App.Settings.settings.mhw_path, "nativePC"));
                    CleanFolder(Path.Combine(App.Settings.settings.mhw_path, "nativePC"));
                    App.logStream.WriteLine($"{App.Mods[index].Item1.name} unistalled");
                }

                var info = App.Mods[index].Item1;
                info.GenInfo(mod);
                info.activated = (sender as CheckBox).IsChecked.Value;
                info.ParseSettingsJSON(mod);
            }
            catch (Exception ex) { App.logStream.WriteLine(ex.ToString(), "FATAL"); }
        }

        private void modListBox_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("UIElement"))
            {
                e.Effects = DragDropEffects.Move;
            }
        }

        private void modListBox_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("UIElement"))
            {
                if (e.Source == modListBox)
                {
                    _isDown = false;
                    _isDragging = false;
                    _realDragSource.ReleaseMouseCapture();
                    return;
                }
                UIElement droptarget = e.Source as UIElement;
                int droptargetIndex = -1, i = 0;
                foreach (UIElement element in modListBox.Items)
                {
                    if (element.Equals(droptarget))
                    {
                        droptargetIndex = i;
                        break;
                    }
                    i++;
                }
                if (droptargetIndex != -1)
                {
                    modListBox.Items.Remove(_realDragSource);
                    modListBox.Items.Insert(droptargetIndex, _realDragSource);
                    int index = 0;
                    foreach (CheckBox checkbox in modListBox.Items)
                    {
                        checkbox.Tag = index;
                        var couple = App.Mods.Find((mod) => mod.Item1.name == checkbox.Content.ToString());
                        couple.Item1.order = index;
                        couple.Item1.ParseSettingsJSON(Path.Combine(App.ModsPath, couple.Item2));
                        index++;
                    }
                }

                _isDown = false;
                _isDragging = false;
                _realDragSource.ReleaseMouseCapture();
            }
            else if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                App.AddMods(files);
            }
        }

        private void modListBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.Source != modListBox)
            {
                _isDown = true;
                _startPoint = e.GetPosition(modListBox);
            }
        }

        private void modListBox_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _isDown = false;
            if (_isDragging)
            {
                _isDragging = false;
                _realDragSource.ReleaseMouseCapture();
            }
        }

        private void modListBox_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (_isDown)
            {
                if ((_isDragging == false) && ((Math.Abs(e.GetPosition(modListBox).X - _startPoint.X) > SystemParameters.MinimumHorizontalDragDistance) ||
                    (Math.Abs(e.GetPosition(modListBox).Y - _startPoint.Y) > SystemParameters.MinimumVerticalDragDistance)))
                {
                    _isDragging = true;
                    _realDragSource = e.Source as UIElement;
                    _realDragSource.CaptureMouse();
                    DragDrop.DoDragDrop(_dummyDragSource, new DataObject("UIElement", e.Source, true), DragDropEffects.Move);
                }
            }
        }

        private void refreshMod_Click(object sender, RoutedEventArgs e)
        {
            UpdateModsList();
        }

        private void remMod_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                foreach (var mod in modListBox.SelectedItems)
                {
                    var caller = (mod as CheckBox);
                    var index = int.Parse(caller.Tag.ToString());
                    App.logStream.WriteLine($"Mod {App.Mods[index].Item1.name} removed");
                    Directory.Delete(Path.Combine(App.ModsPath, App.Mods[index].Item2), true);
                    App.Mods.RemoveAt(index);
                    for (int i = index; i < App.Mods.Count(); i++)
                        App.Mods[i].Item1.order = i;
                }
                UpdateModsList();
            }
            catch (Exception ex) { App.logStream.WriteLine(ex.ToString(), "FATAL"); }
        }

        private void remModContext_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var caller = (((sender as MenuItem).Parent as ContextMenu).PlacementTarget as CheckBox);
                var index = int.Parse(caller.Tag.ToString());
                Directory.Delete(Path.Combine(App.ModsPath, App.Mods[index].Item2), true);
                App.Mods.RemoveAt(index);
                for (int i = index; i < App.Mods.Count(); i++)
                {
                    App.Mods[i].Item1.order = i;
                    App.Mods[i].Item1.ParseSettingsJSON(Path.Combine(App.ModsPath, App.Mods[i].Item2));
                }

                UpdateModsList();
            }
            catch (Exception ex) { App.logStream.WriteLine(ex.ToString(), "FATAL"); }
        }

        private void settingsMod_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var settingsWindow = new SettingsDialog();
                settingsWindow.Owner = Application.Current.MainWindow;
                settingsWindow.ShowDialog();
            }
            catch (Exception ex) { App.logStream.WriteLine(ex.ToString(), "FATAL"); }
        }

        private void startGame_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(Path.Combine(App.Settings.settings.mhw_path, "MonsterHunterWorld.exe"));
                App.logStream.WriteLine($"MHW Started");
            }
            catch (Exception ex) { App.logStream.WriteLine(ex.ToString(), "FATAL"); }
        }

        private void webMod_Click(object sender, RoutedEventArgs e)
        {
            try { Process.Start("https://www.nexusmods.com/monsterhunterworld"); }
            catch (Exception ex) { App.logStream.WriteLine(ex.ToString(), "FATAL"); }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            App.logStream.Close();
        }
    }
}