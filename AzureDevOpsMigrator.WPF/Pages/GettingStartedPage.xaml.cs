using Microsoft.Win32;
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

namespace AzureDevOpsMigrator.WPF.Pages
{
    /// <summary>
    /// Interaction logic for GettingStartedPage.xaml
    /// </summary>
    public partial class GettingStartedPage : Page
    {
        public GettingStartedPage()
        {
            InitializeComponent();
        }

        private void BtnGettingStarted_Click(object sender, RoutedEventArgs e)
        {

        }

        private void BtnLoadExisting_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true) MainWindow.LoadModel(openFileDialog.FileName);
        }
    }
}
