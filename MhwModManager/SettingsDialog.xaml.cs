using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Windows;

namespace MhwModManager
{
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

    public class Setting
    {
        public struct Settings
        {
            public bool dark_mode;
            public string mhw_path;
            public List<bool> mod_installed;
        }

        public Settings settings = new Settings();

        public void GenConfig()
        {
            if (!File.Exists("settings.json"))
            {
                File.Create("settings.json").Close();

                settings.dark_mode = false;
                settings.mhw_path = @"C:\Program Files (x86)\Steam\steamapps\common\Monster Hunter World";
                settings.mod_installed = new List<bool>();

                ParseSettingsJSON();
            }
            else
            {
                Setting sets;
                using (StreamReader file = new StreamReader("settings.json"))
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
            using (StreamWriter file = new StreamWriter("settings.json"))
            {
                file.Write(JsonConvert.SerializeObject(this));
                file.Close();
            }
        }
    }
}