using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MhwModManager
{
    /// <summary>
    /// Logique d'interaction pour EditWindow.xaml
    /// </summary>
    public partial class EditWindow : Window
    {
        private ModInfo modInfo;
        private string initCateg;

        public EditWindow(ModInfo modInfo)
        {
            InitializeComponent();

            MakeDarkTheme();

            this.modInfo = modInfo;

            nameTB.Text = modInfo.name;
            nameTB.TextChanged += nameTB_TextChanged;

            ReloadCB();

            categCB.SelectedItem = initCateg = modInfo.category;
        }

        internal void ReloadCB()
        {
            var categs = new string[App.Categories.Count + 1];
            App.Categories.CopyTo(categs);

            categs[App.Categories.Count] = "<new>";
            categCB.ItemsSource = categs;
        }

        private void validateBTN_Click(object sender, RoutedEventArgs e)
        {
            modInfo.ParseSettingsJSON();
            Close();
        }

        private void MakeDarkTheme()
        {
            var converter = new BrushConverter();
            if (App.Settings.settings.dark_mode)
                Background = (Brush)converter.ConvertFromString("#FF171717");
            else
                Background = (Brush)converter.ConvertFromString("#FFFFFFFF");
        }

        private void cancelBTN_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void nameTB_TextChanged(object sender, TextChangedEventArgs e)
        {
            modInfo.name = nameTB.Text;
        }

        private void categCB_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (categCB.SelectedItem as string == "<new>")
            {
                try
                {
                    var categoriesManager = new CategoriesManager();
                    categoriesManager.Owner = this;

                    categoriesManager.ShowDialog();

                    if (categoriesManager.nameTB.Text.Trim() != "")
                    {
                        App.Categories.Add(categoriesManager.nameTB.Text);
                        categCB.SelectedItem = categoriesManager.nameTB.Text;
                        ReloadCB();
                    }
                    else
                        categCB.SelectedItem = initCateg;
                }
                catch (Exception ex) { App.logStream.Error(ex.ToString()); }
            }
            else
                modInfo.category = categCB.SelectedItem as string;
        }
    }
}