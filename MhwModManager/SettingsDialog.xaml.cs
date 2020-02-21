using System.Windows;
using WinForms = System.Windows.Forms;
using System.Windows.Media;
using System.Linq;
using System.IO;
using System;

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
            MakeDarkTheme();
            versionLbl.Content = "v" + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
        }

        private void browseBTN_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new WinForms.FolderBrowserDialog();
            dialog.SelectedPath = App.Settings.settings.mhw_path;
            if (dialog.ShowDialog() == WinForms.DialogResult.OK)
                App.Settings.settings.mhw_path = dialog.SelectedPath;
        }

        private void cancelBTN_Click(object sender, RoutedEventArgs e)
        {
            App.Settings.GenConfig();
            App.ReloadTheme();
            Close();
        }

        private void darkmodeCB_Checked(object sender, RoutedEventArgs e)
        {
            App.Settings.settings.dark_mode = darkmodeCB.IsChecked.Value;
            if (App.Settings.settings.dark_mode)
                darkmodeCB.Content = "Enabled";
            else
                darkmodeCB.Content = "Disabled";

            App.ReloadTheme();
            MakeDarkTheme();
        }

        private void InitializeSettings()
        {
            pathTB.Text = App.Settings.settings.mhw_path;
            darkmodeCB.IsChecked = App.Settings.settings.dark_mode;
        }

        private void MakeDarkTheme()
        {
            var converter = new BrushConverter();
            if (App.Settings.settings.dark_mode)
            {
                Background = (Brush)converter.ConvertFromString("#FF171717");
                (browseBTN.Content as System.Windows.Controls.Border).BorderBrush = (Brush)converter.ConvertFromString("#FFFFFFFF");
            }
            else
            {
                Background = (Brush)converter.ConvertFromString("#FFFFFFFF");
                (browseBTN.Content as System.Windows.Controls.Border).BorderBrush = (Brush)converter.ConvertFromString("#FF171717");
            }
        }

        private void resetBtn_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Warning ! Clicking Yes will delete every mod informations stored for the software. It will NOT remove installed mods. Are you sure you want to continue ?", "Reset data", MessageBoxButton.YesNoCancel, MessageBoxImage.Warning, MessageBoxResult.Cancel) == MessageBoxResult.Yes)
            {
                Directory.Delete(App.ModsPath, true);
                Close();
                Application.Current.MainWindow.Close();
                System.Diagnostics.Process.Start(Environment.GetCommandLineArgs().First());
                Environment.Exit(0);
            }
        }

        private void validateBTN_Click(object sender, RoutedEventArgs e)
        {
            App.Settings.ParseSettingsJSON();
            App.ReloadTheme();
            Close();
        }
    }
}