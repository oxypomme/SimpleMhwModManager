using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.IO;
using WinForms = System.Windows.Forms;
using Newtonsoft.Json;

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
        public static LogStream logStream = new LogStream("last.log");

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

    public class LogStream
    {
        private StreamWriter writer;

        public LogStream(string path)
        {
            writer = new StreamWriter(path);
            writer.Close();
            writer = File.AppendText(path);
        }

        public void WriteLine(string value, string status = "INFO")
        {
            writer.WriteLine($"[{status}] {DateTime.Now} - {value}");
            writer.Flush();
        }

        public void Close()
        {
            writer.Flush();
            writer.Close();
        }
    }

    public class ModInfo
    {
        public bool activated { get; set; }
        public int order { get; set; }
        public string name { get; set; }

        public void GenInfo(string path, int? index = null)
        {
            try
            {
                if (!File.Exists(Path.Combine(path, "mod.info")))
                {
                    activated = false;
                    if (index != null)
                        order = index.Value;
                    else
                        order = App.Mods.Count();

                    // Get the name of the extracted folder (without the .zip at the end), not the full path
                    var foldName = path.Split('\\');
                    name = foldName[foldName.GetLength(0) - 1].Split('.')[0];

                    App.logStream.WriteLine($"Mod {name} info not found");

                    ParseSettingsJSON(path);
                }
                else
                {
                    ModInfo sets;
                    using (StreamReader file = new StreamReader(Path.Combine(path, "mod.info")))
                    {
                        sets = JsonConvert.DeserializeObject<ModInfo>(file.ReadToEnd());
                        file.Close();
                    }

                    activated = sets.activated;
                    order = sets.order;
                    name = sets.name;

                    App.logStream.WriteLine($"Mod {name} info found");
                }
            }
            catch (Exception e) { App.logStream.WriteLine(e.ToString(), "FATAL"); }
        }

        public void ParseSettingsJSON(string path)
        {
            try
            {
                App.logStream.WriteLine("Mod info updated");
                using (StreamWriter file = new StreamWriter(Path.Combine(path, "mod.info")))
                {
                    file.Write(JsonConvert.SerializeObject(this, Formatting.Indented));
                    file.Close();
                }
            }
            catch (Exception e) { App.logStream.WriteLine(e.ToString(), "FATAL"); }
        }
    }
}