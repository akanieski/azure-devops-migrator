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
using Microsoft.WindowsAPICodePack.Dialogs;
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
            InitializeComponent();
            Unloaded += GeneralPage_Unloaded;
            DataContext = this;
        }

        private void GeneralPage_Unloaded(object sender, RoutedEventArgs e)
        {
            Model.CurrentConfig.WorkingFolder = Path.GetFullPath(Model.CurrentConfig.WorkingFolder);

        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (Model.CurrentConfig != null && (Model.CurrentConfig.WorkingFolder == "" || Model.CurrentConfig.WorkingFolder.StartsWith(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments))))
            {
                Model.CurrentConfig.WorkingFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Azure DevOps Migration Utility", Model.CurrentConfig.Name);
                Text_Name.GetBindingExpression(TextBox.TextProperty).UpdateSource();
            }
        }

        private void Button_Next_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.NavigateTo<EndpointPage>(EndpointConfigMode.Source);
        }
        private void BtnFindWorkingFolder_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CommonOpenFileDialog();
            dialog.IsFolderPicker = true;
            dialog.DefaultDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            CommonFileDialogResult result = dialog.ShowDialog();

            if (result == CommonFileDialogResult.Ok)
            {
                Model.CurrentConfig.WorkingFolder = Path.GetFullPath(dialog.FileName);
                Text_WorkingFolder.GetBindingExpression(TextBox.TextProperty).UpdateSource();
            }
        }
    }
}
