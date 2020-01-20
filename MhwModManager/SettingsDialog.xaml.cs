using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using WinForms = System.Windows.Forms;

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
            InitializeSettings();
        }

        private void InitializeSettings()
        {
            pathTB.Text = App.Settings.settings.mhw_path;
            darkmodeCB.IsChecked = App.Settings.settings.dark_mode;
        }

        private void validateBTN_Click(object sender, RoutedEventArgs e)
        {
            App.Settings.ParseSettingsJSON();
        }

        private void cancelBTN_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void darkmodeCB_Checked(object sender, RoutedEventArgs e)
        {
            App.Settings.settings.dark_mode = darkmodeCB.IsChecked.Value;
            if (App.Settings.settings.dark_mode)
                darkmodeCB.Content = "Enabled";
            else
                darkmodeCB.Content = "Disabled";
        }

        private void browseBTN_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new WinForms.FolderBrowserDialog();
            if (dialog.ShowDialog() == WinForms.DialogResult.OK)
                App.Settings.settings.mhw_path = dialog.SelectedPath;
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