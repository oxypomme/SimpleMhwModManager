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
    /// Logique d'interaction pour MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Public Fields

        public static MainWindow Instance;

        #endregion Public Fields

        #region Private Fields

        private UIElement _dummyDragSource = new UIElement();
        private bool _isDown;
        private bool _isDragging;
        private UIElement _realDragSource;
        private Point _startPoint;
        private bool isDarkTheme = false;

        #endregion Private Fields

        #region Public Constructors

        public MainWindow()
        {
            Instance = this;
            InitializeComponent();

            UpdateModsList();

            App.ReloadTheme();
        }

        #endregion Public Constructors

        #region Public Methods

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
            catch (Exception e) { App.logStream.Error(e.ToString()); }
        }

        public void UpdateModsList()
        {
            try
            {
                modListBox.Items.Clear();
                App.GetMods();

                foreach (var mod in App.Mods)
                {
                    var style = Application.Current.FindResource("CheckBoxListItem") as Style;
                    var modItem = new ModCheckBox
                    {
                        Info = mod,
                        ModName = mod.name,
                        Category = mod.category,
                        Style = style
                    };
                    modItem.Checked += itemChecked;
                    modItem.Unchecked += itemChecked;
                    modItem.IsChecked = mod.activated;
                    // Adding the context menu
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
                    if (!CheckFiles(Path.Combine(App.ModsPath, App.Mods[i].path), Path.Combine(App.ModsPath, App.Mods[i + 1].path)))
                    {
                        var firstModItem = modListBox.Items[App.Mods[i].order] as ModCheckBox;
                        var secondModItem = modListBox.Items[App.Mods[i + 1].order] as ModCheckBox;
                        firstModItem.FontStyle = FontStyles.Italic;
                        firstModItem.ToolTip = "Conflict with " + App.Mods[i + 1].name;
                        secondModItem.FontStyle = FontStyles.Italic;
                        secondModItem.ToolTip = "Conflict with " + App.Mods[i].name;
                    }
            }
            catch (Exception e) { App.logStream.Error(e.Message); }
        }

        #endregion Public Methods

        #region Private Methods

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
                    file.CopyTo(temppath, true);
            }

            // If copying subdirectories, copy them and their contents to new location.
            if (copySubDirs)
                foreach (DirectoryInfo subdir in dirs)
                {
                    string temppath = Path.Combine(destDirName, subdir.Name);
                    DirectoryCopy(subdir.FullName, temppath, copySubDirs);
                }
        }

        private void addAdvancedMod_Click(object sender, RoutedEventArgs e)
        {
            addAdvancedMod.ContextMenu.PlacementTarget = addAdvancedMod;
            addAdvancedMod.ContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            addAdvancedMod.ContextMenu.IsOpen = true;
        }

        private void addMod_Click(object sender, RoutedEventArgs e)
        {
            App.AddMods();
        }

        private void addRootMod_Click(object sender, RoutedEventArgs e)
        {
            App.AddMods(true);
        }

        private void btn_help_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Not Implemented yet", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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

        private void editMod(ModInfo modInfo)
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
                editMod((((sender as MenuItem).Parent as ContextMenu).PlacementTarget as ModCheckBox).Info as ModInfo);
            }
            catch (Exception ex) { App.logStream.Error(ex.ToString()); }
        }

        private void itemChecked(object sender, RoutedEventArgs e)
        {
            try
            {
                // Get the full path of the mod
                var mod = (sender as ModCheckBox).Info as ModInfo;
                var modPath = Path.Combine(App.ModsPath, mod.path);
                var isAnyChanges = false;

                if ((e.OriginalSource as CheckBox).IsChecked.Value && !mod.activated)
                {
                    // Install the mod
                    if (mod.root)
                        DirectoryCopy(Path.Combine(modPath, "install"), App.Settings.settings.mhw_path, true);
                    else
                        DirectoryCopy(modPath, Path.Combine(App.Settings.settings.mhw_path, "nativePC"), true);
                    App.logStream.Log($"{mod.name} installed");
                    isAnyChanges = true;
                }
                else if (!(e.OriginalSource as CheckBox).IsChecked.Value && mod.activated)
                {
                    // Unistalled the mod
                    if (!mod.root)
                        DeleteMod(modPath, Path.Combine(App.Settings.settings.mhw_path, "nativePC"));
                    else
                    {
                        foreach (var file in App.GetRecursiveFiles(Path.Combine(modPath, "install")))
                        {
                            var relative = file.Substring(Path.Combine(modPath, "install").Length + 1);
                            File.Delete(Path.Combine(App.Settings.settings.mhw_path, relative));
                            DirectoryCopy(Path.Combine(modPath, "uninstall"), App.Settings.settings.mhw_path, true);
                        }
                    }
                    CleanFolder(Path.Combine(App.Settings.settings.mhw_path, "nativePC"));
                    App.logStream.Log($"{mod.name} unistalled");
                    isAnyChanges = true;
                }

                mod.activated = (e.OriginalSource as CheckBox).IsChecked.Value;
                if (isAnyChanges)
                    mod.ParseSettingsJSON();
            }
            catch (Exception ex) { App.logStream.Error(ex.ToString()); }
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
                    var buffer = ((ModCheckBox)_realDragSource).Info;
                    App.Mods.Remove(buffer);
                    App.Mods.Insert(droptargetIndex, buffer);
                    int index = 0;
                    foreach (var mod in App.Mods)
                    {
                        mod.order = index++;
                        mod.ParseSettingsJSON();
                    }
                    UpdateModsList();
                }

                _isDown = false;
                _isDragging = false;
                _realDragSource.ReleaseMouseCapture();
            }
            else if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                App.AddMods(false, files);
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
            try
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
            catch (NullReferenceException ex) { App.logStream.Error(ex.GetType().ToString() + ": " + ex.Message + " " + ex.TargetSite); }
            catch (Exception ex) { App.logStream.Error(ex.ToString()); }
        }

        private void refreshMod_Click(object sender, RoutedEventArgs e)
        {
            UpdateModsList();
        }

        private void remMod_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                foreach (var cb in modListBox.SelectedItems)
                {
                    var caller = (cb as ModCheckBox);
                    var mod = (sender as ModCheckBox).Info as ModInfo;
                    App.logStream.Log($"Mod {mod.name} removed");
                    Directory.Delete(Path.Combine(App.ModsPath, mod.path), true);
                    App.Mods.Remove(mod);
                    for (int i = mod.order; i < App.Mods.Count(); i++)
                        App.Mods[i].order = i;
                }
                UpdateModsList();
            }
            catch (Exception ex) { App.logStream.Error(ex.ToString()); }
        }

        private void remModContext_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var caller = (((sender as MenuItem).Parent as ContextMenu).PlacementTarget as ModCheckBox);
                var mod = (caller as ModCheckBox).Info as ModInfo;
                Directory.Delete(Path.Combine(App.ModsPath, mod.path), true);
                App.Mods.Remove(mod);
                for (int i = mod.order; i < App.Mods.Count(); i++)
                {
                    App.Mods[i].order = i;
                    App.Mods[i].ParseSettingsJSON();
                }

                UpdateModsList();
            }
            catch (Exception ex) { App.logStream.Error(ex.ToString()); }
        }

        private void settingsMod_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var settingsWindow = new SettingsDialog();
                settingsWindow.Owner = Application.Current.MainWindow;
                settingsWindow.ShowDialog();
            }
            catch (Exception ex) { App.logStream.Error(ex.ToString()); }
        }

        private void startGame_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(Path.Combine(App.Settings.settings.mhw_path, "MonsterHunterWorld.exe"));
                App.logStream.Log($"MHW Started");
            }
            catch (Exception ex) { App.logStream.Error(ex.ToString()); }
        }

        private void webMod_Click(object sender, RoutedEventArgs e)
        {
            try { Process.Start("https://www.nexusmods.com/monsterhunterworld"); }
            catch (Exception ex) { App.logStream.Error(ex.ToString()); }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            App.logStream.Close();
        }

        #endregion Private Methods
    }
}