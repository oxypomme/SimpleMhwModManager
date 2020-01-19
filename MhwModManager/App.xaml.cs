using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.IO;

namespace MhwModManager
{
    /// <summary>
    /// Logique d'interaction pour App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
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
    }
}