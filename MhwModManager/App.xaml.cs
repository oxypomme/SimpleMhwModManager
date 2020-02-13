using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.IO;
using WinForms = System.Windows.Forms;
using Newtonsoft.Json;
using System.Text;
using System.Windows.Media.Imaging;
using System.Drawing;

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
            Settings.GenConfig();
            if (Settings.settings.dark_mode)
            {
            }

            if (!Directory.Exists(Settings.settings.mhw_path))
            {
                logStream.WriteLine("MHW not found", "CRITICAL");
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
        }

        public static void GetMods()
        {
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
                logStream.WriteLine("ModList updated");
            }
        }

        public async static void Updater()
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
    }

    public class LogStream
    {
        private string Path = null;

        public LogStream(string path)
        {
            Path = path;
            using (StreamWriter writer = new StreamWriter(Path)) { }
        }

        public void WriteLine(string value, string status = "INFO")
        {
            using (StreamWriter writer = File.AppendText(Path))
                writer.WriteLine($"[{status}] {DateTime.Now} - {value}");
        }
    }

    public class ModInfo
    {
        public bool activated { get; set; }
        public int order { get; set; }
        public string name { get; set; }

        public void GenInfo(string path, int? index = null)
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

        public void ParseSettingsJSON(string path)
        {
            App.logStream.WriteLine("Mod info updated");
            using (StreamWriter file = new StreamWriter(Path.Combine(path, "mod.info")))
            {
                file.Write(JsonConvert.SerializeObject(this, Formatting.Indented));
                file.Close();
            }
        }
    }
}