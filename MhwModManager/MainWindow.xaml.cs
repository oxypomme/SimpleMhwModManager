using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MhwModManager
{
    /// <summary>
    /// Logique d'interaction pour MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

#if RELEASE
            App.Updater();
#endif
        }

        private void addMod_Click(object sender, RoutedEventArgs e)
        {
        }

        private void remMod_Click(object sender, RoutedEventArgs e)
        {
        }

        private void startGame_Click(object sender, RoutedEventArgs e)
        {
        }
    }
}