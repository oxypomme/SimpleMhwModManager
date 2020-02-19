using System;
using System.Collections.Generic;
using System.Windows;
using System.IO;
using WinForms = System.Windows.Forms;

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
        public static LogStream logStream = new LogStream(Path.Combine(AppData, "last.log"));

        public App()
        {
            try
            {
                Settings.GenConfig();

                ReloadTheme();

                if (!Directory.Exists(Settings.settings.mhw_path))
                {
                    try { throw new FileNotFoundException(); } catch (FileNotFoundException e) { logStream.WriteLine(e.ToString(), "CRITICAL"); }

                    MessageBox.Show("The path to MHW is not found, you have to install the game first, or if the game is already installed, open it", "Simple MHW Mod Manager", MessageBoxButton.OK, MessageBoxImage.Error);
                    var dialog = new WinForms.FolderBrowserDialog();
                    if (dialog.ShowDialog() == WinForms.DialogResult.OK)
                    {
                        logStream.WriteLine("MHW path set");
                        Settings.settings.mhw_path = dialog.SelectedPath;
                        Settings.ParseSettingsJSON();
                    }
                    else
                    {
                        logStream.WriteLine("MHW path not set", "FATAL");
                        Environment.Exit(0);
                    }
                }

                Updater();
            }
            catch (Exception e) { logStream.WriteLine(e.ToString(), "FATAL"); }
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
            catch (Exception e) { logStream.WriteLine(e.ToString(), "ERROR"); }
        }

        public static void GetMods()
        {
            try
            {
                logStream.WriteLine("Updating modlist...");
                // This list contain the ModInfos and the folder name of each mod
                Mods = new List<(ModInfo, string)>();

                if (!Directory.Exists(ModsPath))
                    Directory.CreateDirectory(ModsPath);

                var modFolder = new DirectoryInfo(ModsPath);

                int i = 0;
                foreach (var mod in modFolder.GetDirectories())
                {
                    var info = new ModInfo();
                    info.GenInfo(mod.FullName);
                    // If the order change the generation of the list
                    if (info.order >= Mods.Count)
                        Mods.Add((info, mod.Name));
                    else
                    {
                        if (i > 0)
                            if (info.order == Mods[i - 1].Item1.order)
                            {
                                info.order++;
                                info.ParseSettingsJSON(mod.FullName);
                            }
                        Mods.Insert(info.order, (info, mod.Name));
                        logStream.WriteLine($"Mod added : {info.name}");
                    }
                    i++;
                }
                logStream.WriteLine("Modlist updated !");
            }
            catch (Exception e) { logStream.WriteLine(e.ToString(), "FATAL"); }
        }

        public async static void Updater()
        {
            try
            {
                /* Credits to WildGoat07 : https://github.com/WildGoat07 */
                var github = new Octokit.GitHubClient(new Octokit.ProductHeaderValue("SimpleMhwModManager"));
                var lastRelease = await github.Repository.Release.GetLatest(234864718);
                var current = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                logStream.WriteLine($"Versions : Current = {current}, Latest = {lastRelease.TagName}");
                if (new Version(lastRelease.TagName) > current)
                {
                    var result = MessageBox.Show("A new version is available, do you want to download it now ?", "SMMM", MessageBoxButton.YesNo, MessageBoxImage.Information);
                    if (result == MessageBoxResult.Yes)
                        System.Diagnostics.Process.Start("https://github.com/oxypomme/SimpleMhwModManager/releases/latest");
                }
            }
            catch (Exception e) { logStream.WriteLine(e.ToString(), "FATAL"); }
        }
    }
}