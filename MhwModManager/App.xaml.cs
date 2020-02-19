using System;
using System.Collections.Generic;
using System.Windows;
using System.IO;
using WinForms = System.Windows.Forms;
using Newtonsoft.Json;
using SevenZipExtractor;

namespace MhwModManager
{
    /// <summary>
    /// Logique d'interaction pour App.xaml
    /// </summary>
    public partial class App : Application
    {
#if (!DEBUG && !TRACE)
        //It's the portable version

        public static string AppData = "Data/";

#else
        public static string AppData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SMMM");

#endif
        public static string ModsPath = Path.Combine(AppData, "mods");
        public static Setting Settings = new Setting();
        public static string SettingsPath = Path.Combine(AppData, "settings.json");
        public static List<(ModInfo, string)> Mods;
        public static LogStream logStream;

        public App()
        {
            try
            {
                if (!Directory.Exists(AppData))
                    Directory.CreateDirectory(AppData);

                logStream = new LogStream(Path.Combine(AppData, "last.log"));

                Settings.GenConfig();

                ReloadTheme();

                if (!Directory.Exists(Settings.settings.mhw_path))
                {
                    logStream.Error(new FileNotFoundException().ToString());

                    MessageBox.Show("The path to MHW is not found, you have to install the game first, or if the game is already installed, open it", "Simple MHW Mod Manager", MessageBoxButton.OK, MessageBoxImage.Error);
                    var dialog = new WinForms.FolderBrowserDialog();
                    if (dialog.ShowDialog() == WinForms.DialogResult.OK)
                    {
                        logStream.Log("MHW path set");
                        Settings.settings.mhw_path = dialog.SelectedPath;
                        Settings.ParseSettingsJSON();
                    }
                    else
                    {
                        logStream.Error("MHW path not set");
                        Environment.Exit(0);
                    }
                }

                Updater();
            }
            catch (Exception e) { logStream.Error(e.ToString()); }
        }

        public static void AddMods(params string[] paths)
        {
            try
            {
                if (paths == null || paths.Length == 0)
                {
                    var dialog = new WinForms.OpenFileDialog();
                    // Dialog to select a mod archive
                    dialog.DefaultExt = "zip";
                    dialog.Filter = "Mod Archives (*.zip, *.rar, *.7z)|*.zip;*.rar;*.7z|all files|*";
                    dialog.Multiselect = true;
                    if (dialog.ShowDialog() == WinForms.DialogResult.OK)
                        paths = dialog.FileNames;
                    else
                        return;
                }
                var tmpFolder = Path.Combine(Path.GetTempPath(), "SMMMaddMod");

                if (!Directory.Exists(tmpFolder))
                    Directory.CreateDirectory(tmpFolder);

                foreach (var file in paths)
                {
                    // Separate the path and unzip mod
                    var splittedPath = file.Split('\\');
                    using (ArchiveFile archiveFile = new ArchiveFile(file))
                        archiveFile.Extract(tmpFolder);

                    // Get the name of the extracted folder (without the .zip at the end), not the
                    // full path
                    if (!InstallMod(tmpFolder, splittedPath[splittedPath.GetLength(0) - 1].Split('.')[0]))
                        // If the install fail
                        MessageBox.Show("nativePC not found... Please check if it's exist in the mod...", "Simple MHW Mod Manager", MessageBoxButton.OK, MessageBoxImage.Error);
                    Directory.Delete(tmpFolder, true);

                    GetMods(); // Refresh the modlist
                }
                MhwModManager.MainWindow.Instance.UpdateModsList();
            }
            catch (Exception ex) { logStream.Error(ex.Message); }
        }

        public static bool InstallMod(string path, string name)
        {
            foreach (var dir in Directory.GetDirectories(path))
            {
                if (dir.Equals(Path.Combine(path, "nativePC"), StringComparison.OrdinalIgnoreCase))
                {
                    if (!Directory.Exists(Path.Combine(App.ModsPath, name)))
                        // If the mod isn't installed
                        Directory.Move(dir, Path.Combine(App.ModsPath, name));
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

        public static void ReloadTheme()
        {
            Current.Resources.MergedDictionaries.Clear();

            var ressource = new ResourceDictionary();

            if (Settings.settings.dark_mode)
            {
                ressource.Source = new Uri("pack://application:,,,/MhwModManager;component/Themes/DarkTheme.xaml", UriKind.RelativeOrAbsolute);
                Current.Resources.MergedDictionaries.Add(ressource);
            }
            else
            {
                ressource.Source = new Uri("pack://application:,,,/MhwModManager;component/Themes/LightTheme.xaml", UriKind.RelativeOrAbsolute);
                Current.Resources.MergedDictionaries.Add(ressource);
            }

            ressource = new ResourceDictionary();
            ressource.Source = new Uri("pack://application:,,,/MhwModManager;component/Themes/Theme.xaml", UriKind.RelativeOrAbsolute);
            Current.Resources.MergedDictionaries.Add(ressource);

            try
            {
                (Current.MainWindow as MainWindow).MakeDarkTheme();
                Current.MainWindow.UpdateLayout();
            }
            catch (Exception e) { logStream.Error(e.ToString()); }
        }

        public static void GetMods()
        {
            try
            {
                logStream.Log("Updating modlist...");
                // This list contain the ModInfos and the folder name of each mod
                Mods = new List<(ModInfo, string)>();

                if (!Directory.Exists(ModsPath))
                    Directory.CreateDirectory(ModsPath);

                var modFolder = new DirectoryInfo(ModsPath);

                foreach (var mod in modFolder.GetDirectories())
                {
                    var info = new ModInfo();
                    info.GenInfo(mod.FullName);
                    Mods.Add((info, mod.Name));
                }
                Mods.Sort((left, right) => left.Item1.order.CompareTo(right.Item1.order));
                logStream.Log("Modlist updated !");
            }
            catch (Exception e) { logStream.Error(e.ToString()); }
        }

        public async static void Updater()
        {
            try
            {
                /* Credits to WildGoat07 : https://github.com/WildGoat07 */
                var github = new Octokit.GitHubClient(new Octokit.ProductHeaderValue("SimpleMhwModManager"));
                var lastRelease = await github.Repository.Release.GetLatest(234864718);
                var current = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                logStream.Log($"Versions : Current = {current}, Latest = {lastRelease.TagName}");
                if (new Version(lastRelease.TagName) > current)
                {
                    var result = MessageBox.Show("A new version is available, do you want to download it now ?", "SMMM", MessageBoxButton.YesNo, MessageBoxImage.Information);
                    if (result == MessageBoxResult.Yes)
                        System.Diagnostics.Process.Start("https://github.com/oxypomme/SimpleMhwModManager/releases/latest");
                }
            }
            catch (Exception e) { logStream.Error(e.ToString()); }
        }
    }
}