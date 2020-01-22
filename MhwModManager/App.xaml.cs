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

namespace MhwModManager
{
    /// <summary>
    /// Logique d'interaction pour App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static string AppData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SMMM");
        public static string ModsPath = Path.Combine(AppData, "mods");
        public static Setting Settings = new Setting();
        public static string SettingsPath = Path.Combine(AppData, "settings.json");

        public App()
        {
            Settings.GenConfig();
            if (!Directory.Exists(Settings.settings.mhw_path))
            {
                MessageBox.Show("The path to MHW is not found, you have to install the game first, or if the game is already installed, open it", "Simple MHW Mod Manager", MessageBoxButton.OK, MessageBoxImage.Error);
                var dialog = new WinForms.FolderBrowserDialog();
                if (dialog.ShowDialog() == WinForms.DialogResult.OK)
                {
                    Settings.settings.mhw_path = dialog.SelectedPath;
                    Settings.ParseSettingsJSON();
                }
                else
                    Environment.Exit(0);
            }
        }

        public static List<ModInfo> GetMods()
        {
            var modList = new List<ModInfo>();

            if (!Directory.Exists(ModsPath))
                Directory.CreateDirectory(ModsPath);

            var modFolder = new DirectoryInfo(ModsPath);

            foreach (var mod in modFolder.GetDirectories())
            {
                var info = new ModInfo();
                info.GenInfo(mod.FullName);
                modList.Add(info);
            }

            return modList;
        }

        public async static void Updater()
        {
            /* Credits to WildGoat07 : https://github.com/WildGoat07 */
            var github = new Octokit.GitHubClient(new Octokit.ProductHeaderValue("SimpleMhwModManager"));
            var lastRelease = await github.Repository.Release.GetLatest(234864718);
            var current = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            if (new Version(lastRelease.TagName) > current)
            {
                var result = MessageBox.Show("A new version is available, do you want to download it now ?", "SMMM", MessageBoxButton.YesNo, MessageBoxImage.Information);
                if (result == MessageBoxResult.Yes)
                    System.Diagnostics.Process.Start("https://github.com/oxypomme/SimpleMhwModManager/releases/latest");
            }
        }
    }

    public class ModInfo
    {
        public bool activated { get; set; }
        public int order { get; set; }
        public string name { get; set; }

        public void GenInfo(string path, int index = 0)
        {
            if (!File.Exists(Path.Combine(path, "mod.info")))
            {
                activated = false;
                order = index;

                var foldName = path.Split('\\');
                name = foldName[foldName.GetLength(0) - 1].Split('.')[0];

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
            }
        }

        public void ParseSettingsJSON(string path)
        {
            using (StreamWriter file = new StreamWriter(Path.Combine(path, "mod.info")))
            {
                file.Write(JsonConvert.SerializeObject(this, Formatting.Indented));
                file.Close();
            }
        }
    }
}