using AzureDevOpsMigrator.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace AzureDevOpsMigrator.WPF.Pages.WitPages
{
    /// <summary>
    /// Interaction logic for WitIterationsPage.xaml
    /// </summary>
    public partial class WitIterationsPage : Page
    {
        public MigrationConfig Model => MainWindow.CurrentModel.CurrentConfig;
        public WitIterationsPage()
        {
            InitializeComponent();
            DataContext = this;
        }
    }
}
