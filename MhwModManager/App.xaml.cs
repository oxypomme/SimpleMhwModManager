using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
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
        public static Setting Settings = new Setting();

        public App()
        {
            Settings.GenConfig();
            if (!Directory.Exists(Settings.settings.mhw_path))
            {
                MessageBox.Show("The path to MHW is wrong, please correct it !", "MHW Mod Manager", MessageBoxButton.OK, MessageBoxImage.Error);
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

        public static List<string> GetMods()
        {
            var modList = new List<string>();
            var modFolder = new DirectoryInfo("mods");

            if (!Directory.Exists("mods"))
                Directory.CreateDirectory("mods");

            foreach (var mod in modFolder.GetDirectories())
                modList.Add(mod.Name);

            return modList;
        }

        public async static void Updater()
        {
            /* Credits to WildGoat07 : https://github.com/WildGoat07 */
            var github = new Octokit.GitHubClient(new Octokit.ProductHeaderValue("MhwModManager"));
            var lastRelease = await github.Repository.Release.GetLatest(234864718);
            var current = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            if (new Version(lastRelease.TagName) > current)
            {
                var result = MessageBox.Show("A new version is available, do you want to download it now ?", "MHW Mod Manager", MessageBoxButton.YesNo, MessageBoxImage.Information);
                if (result == MessageBoxResult.Yes)
                    System.Diagnostics.Process.Start("https://github.com/oxypomme/MhwModManager/releases");
            }
        }

        private void ListBoxItem_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            MessageBox.Show("test");
        }
    }
}