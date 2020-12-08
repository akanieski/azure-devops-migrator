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
using Ookii.Dialogs.Wpf;
using Path = System.IO.Path;

namespace AzureDevOpsMigrator.WPF.Pages
{
    /// <summary>
    /// Interaction logic for GeneralPage.xaml
    /// </summary>
    public partial class GeneralPage : Page
    {
        public MigrationViewModel Model => MainWindow.CurrentModel;
        public GeneralPage()
        {
            if (Model.CurrentConfig == null)
            {
                Model.StartNew();
            }
            MainWindow.RefreshBindings();
            InitializeComponent();
            Unloaded += GeneralPage_Unloaded;
            DataContext = this;
        }

        private void GeneralPage_Unloaded(object sender, RoutedEventArgs e)
        {

        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (Model.CurrentConfig != null && (Model.WorkingFolder == "" || Model.WorkingFolder.StartsWith(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments))))
            {
                Model.WorkingFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Azure DevOps Migration Utility", Model.CurrentConfig.Name ?? "");
                Text_Name.GetBindingExpression(TextBox.TextProperty).UpdateSource();
                Text_WorkingFolder.GetBindingExpression(TextBox.TextProperty).UpdateSource();
                Text_WorkingFolder.PageRight();
            }
        }

        private void Button_Next_Click(object sender, RoutedEventArgs e)
        {
            if (Model.CurrentConfig.Name.Length > 1 && Model.WorkingFolder.Length > 1)
            {
                MainWindow.NavigateTo<EndpointPage>(EndpointConfigMode.Source);
            } 
            else
            {
                MessageBox.Show("Please provide a valid name and working directory.");
            }
        }
        private void BtnFindWorkingFolder_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new VistaFolderBrowserDialog();
            dialog.RootFolder = Environment.SpecialFolder.MyDocuments;
            var result = dialog.ShowDialog();

            if (result.HasValue && result.Value)
            {
                Model.WorkingFolder = Path.GetFullPath(dialog.SelectedPath);
                Text_WorkingFolder.GetBindingExpression(TextBox.TextProperty).UpdateSource();
                Text_WorkingFolder.ScrollToEnd();
            }
        }
    }
}
