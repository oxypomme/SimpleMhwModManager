using System;
using System.Collections.Generic;
using System.Windows;
using System.IO;
using WinForms = System.Windows.Forms;
using SevenZipExtractor;
using System.Linq;

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
        public static List<ModInfo> Mods;
        public static LogStream logStream;
        public static HashSet<string> Categories;

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

        public static void AddMods(bool root = false, params string[] paths)
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
                    InstallMod(tmpFolder, splittedPath[splittedPath.GetLength(0) - 1].Split('.')[0], !root);
                    try
                    {
                        Directory.Delete(tmpFolder, true);
                    }
                    catch (DirectoryNotFoundException)
                    {
                        //the folder may be missing if it has no nativePC and moved the entire folder, deleting it
                    }

                    GetMods(); // Refresh the modlist
                }
                MhwModManager.MainWindow.Instance.UpdateModsList();
            }
            catch (Exception ex) { logStream.Error(ex); }
        }

        public static string[] GetRecursiveDirectories(string path)
        {
            var paths = new List<string>();
            void addDir(string p)
            {
                paths.Add(p);
                foreach (var item in Directory.GetDirectories(p))
                    addDir(item);
            }
            addDir(path);
            return paths.ToArray();
        }

        public static string[] GetRecursiveFiles(string path)
        {
            var paths = new List<string>();
            void addDir(string p)
            {
                paths.AddRange(Directory.GetFiles(p));
                foreach (var item in Directory.GetDirectories(p))
                    addDir(item);
            }
            addDir(path);
            return paths.ToArray();
        }

        public static bool InstallMod(string path, string name, bool lookForNativePC)
        {
            if (Directory.Exists(Path.Combine(ModsPath, name)))
            // If the mod is installed
            {
                MessageBox.Show("This mod is already installed", "Simple MHW Mod Manager", MessageBoxButton.OK, MessageBoxImage.Information);
                return true;
            }

            var nativePCs = new List<string>();
            if (lookForNativePC)
                foreach (var dir in GetRecursiveDirectories(path))
                {
                    if (Path.GetFileName(dir).Equals("nativePC", StringComparison.OrdinalIgnoreCase))
                    {
                        nativePCs.Add(dir);
                        logStream.Log(Path.Combine(name, dir.Substring(path.Length + 1)) + " found.");
                    }
                }
            else
            {
                Directory.CreateDirectory(Path.Combine(ModsPath, name));
                MoveDirectory(path, Path.Combine(ModsPath, name, "install"));
                Directory.CreateDirectory(Path.Combine(ModsPath, name, "uninstall"));
                foreach (var file in GetRecursiveFiles(Path.Combine(ModsPath, name, "install")))
                {
                    var relative = file.Substring(Path.Combine(ModsPath, name, "install").Length + 1);
                    if (File.Exists(Path.Combine(Settings.settings.mhw_path, relative)))
                        File.Copy(Path.Combine(Settings.settings.mhw_path, relative), Path.Combine(ModsPath, name, "uninstall", relative));
                }
                var info = new ModInfo();

                info.GenInfo(name, null, true);
                return true;
            }
            if (nativePCs.Count == 1)
            {
                MoveDirectory(nativePCs.First(), Path.Combine(ModsPath, name));
                return true;
            }
            else if (nativePCs.Count == 0)
            {
                logStream.Warning("No nativePC found.");
                if (MessageBox.Show("No nativePC found, add the entire file as the nativePC folder ?", "Simple MHW Mod Manager", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    MoveDirectory(path, Path.Combine(ModsPath, name));
                    return true;
                }
                else
                    return false;
            }
            else
            {
                var dialog = new nativePcPicker(nativePCs.Select(str => str.Substring(path.Length + 1)));
                if (dialog.ShowDialog() == true)
                {
                    MoveDirectory(Path.Combine(path, dialog.Value), Path.Combine(ModsPath, name));
                    return true;
                }
                else
                    return false;
            }
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
                Mods = new List<ModInfo>();
                Categories = new HashSet<string>();

                if (!Directory.Exists(ModsPath))
                    Directory.CreateDirectory(ModsPath);

                var modFolder = new DirectoryInfo(ModsPath);

                foreach (var mod in modFolder.GetDirectories())
                {
                    var info = new ModInfo();

                    info.GenInfo(mod.Name);
                    Mods.Add(info);
                    Categories.Add(info.category);
                }
                Mods.Sort((left, right) => left.order.CompareTo(right.order));
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

        public static void MoveFile(string source, string destination)
        {
            try
            {
                if (Path.GetPathRoot(source) == Path.GetPathRoot(destination))
                    File.Move(source, destination);
                else
                {
                    using (var sourceStream = new FileStream(source, FileMode.Open, FileAccess.Read))
                    using (var destinationStream = new FileStream(destination, FileMode.Create, FileAccess.Write))
                        sourceStream.CopyTo(destinationStream);
                    File.Delete(source);
                }
            }
            catch (Exception e) { logStream.Error(e.ToString()); }
        }

        public static void MoveDirectory(string source, string destination)
        {
            try
            {
                if (Directory.GetDirectoryRoot(source) == Directory.GetDirectoryRoot(destination))
                    Directory.Move(source, destination);
                else
                {
                    var requiredFolders = new List<string>();
                    void GetFolders(string path, ICollection<string> list)
                    {
                        foreach (var dir in Directory.GetDirectories(path))
                        {
                            var subDirs = Directory.GetDirectories(dir);
                            if (subDirs.Any())
                                foreach (var subDir in subDirs)
                                    GetFolders(subDir, list);
                            else
                                list.Add(Path.Combine(destination, dir.Substring(source.Length + 1)));
                        }
                    }
                    GetFolders(source, requiredFolders);
                    foreach (var folder in requiredFolders)
                        Directory.CreateDirectory(folder);
                    var requiredFiles = new List<(string, string)>();
                    void GetFiles(string path, ICollection<(string, string)> list)
                    {
                        foreach (var file in Directory.GetFiles(path))
                            list.Add((file, Path.Combine(destination, file.Substring(source.Length + 1))));
                        foreach (var folder in Directory.GetDirectories(path))
                            GetFiles(folder, list);
                    }
                    GetFiles(source, requiredFiles);
                    foreach (var file in requiredFiles)
                        MoveFile(file.Item1, file.Item2);
                    Directory.Delete(source, true);
                }
            }
            catch (Exception e) { logStream.Error(e.ToString()); }
        }
    }
}