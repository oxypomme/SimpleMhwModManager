using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Windows;

namespace MhwModManager
{
    public class Setting
    {
        public Settings settings = new Settings();

        public void GenConfig()
        {
            if (!File.Exists(App.SettingsPath))
            {
                if (!Directory.Exists(App.AppData))
                    Directory.CreateDirectory(App.AppData);
                File.Create(App.SettingsPath).Close();

                settings.dark_mode = false;
                settings.mhw_path = @"C:\Program Files (x86)\Steam\steamapps\common\Monster Hunter World";
                settings.mod_installed = new List<bool>();

                ParseSettingsJSON();
            }
            else
            {
                Setting sets;
                using (StreamReader file = new StreamReader(App.SettingsPath))
                {
                    sets = JsonConvert.DeserializeObject<Setting>(file.ReadToEnd());
                    file.Close();
                }

                settings.dark_mode = sets.settings.dark_mode;
                settings.mhw_path = sets.settings.mhw_path;
                settings.mod_installed = sets.settings.mod_installed;
            }
        }

        public void ParseSettingsJSON()
        {
            using (StreamWriter file = new StreamWriter(App.SettingsPath))
            {
                file.Write(JsonConvert.SerializeObject(this));
                file.Close();
            }
        }

        public struct Settings
        {
            public bool dark_mode;
            public string mhw_path;
            public List<bool> mod_installed;
        }
    }

    /// <summary>
    /// Logique d'interaction pour SettingsDialog.xaml
    /// </summary>
    public partial class SettingsDialog : Window
    {
        public SettingsDialog()
        {
            InitializeComponent();
        }
    }
}