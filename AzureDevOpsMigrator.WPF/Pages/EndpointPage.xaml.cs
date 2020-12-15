using AzureDevOpsMigrator.EndpointServices;
using AzureDevOpsMigrator.Services;
using Microsoft.TeamFoundation.Core.WebApi;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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

namespace AzureDevOpsMigrator.WPF.Pages
{
    public enum EndpointConfigMode
    {
        Source,
        Target
    }
    /// <summary>
    /// Interaction logic for EndpointPage.xaml
    /// </summary>
    public partial class EndpointPage : Page, INotifyPropertyChanged
    {
        public Models.EndpointConfig Model { get; set; }
        public string PageTitle => Mode == EndpointConfigMode.Source ? "Source Endpoint Configuration" : "Target Endpoint Configuration";
        public EndpointConfigMode Mode { get; set; }
        private bool? _testSuccessful = null;
        private bool _testLoading;
        private IEndpointServiceResolver _resolver;
        private IEndpointService _endpointService;

        public string SuccessMessage { get; set; }
        public string FailureMessage { get; set; }
        public IEnumerable<TeamProjectReference> Projects { get; set; }
        public bool TestLoading
        {
            get => !_testLoading;
            set
            {
                _testLoading = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("TestLoading"));
            }
        }
        public Visibility ConnectionSuccessful => _testSuccessful == true ? Visibility.Visible : Visibility.Collapsed;
        public Visibility ConnectionFailed => _testSuccessful == false ? Visibility.Visible : Visibility.Collapsed;


        public EndpointPage(EndpointConfigMode mode)
        {
            InitializeComponent();
            _resolver = MainWindow.ServiceProvider.GetService(typeof(IEndpointServiceResolver)) as IEndpointServiceResolver;
            DataContext = this;
            Mode = mode; 
            switch (Mode)
            {
                case EndpointConfigMode.Source:
                    Model = MainWindow.CurrentModel.CurrentConfig.SourceEndpointConfig = MainWindow.CurrentModel.CurrentConfig.SourceEndpointConfig ?? new AzureDevOpsMigrator.Models.EndpointConfig();
                    _endpointService = _resolver.Resolve(Model);
                    break;
                case EndpointConfigMode.Target:
                    Model = MainWindow.CurrentModel.CurrentConfig.TargetEndpointConfig = MainWindow.CurrentModel.CurrentConfig.TargetEndpointConfig ?? new AzureDevOpsMigrator.Models.EndpointConfig();
                    _endpointService = _resolver.Resolve(Model);
                    break;
            }
            if (!string.IsNullOrEmpty(Model.EndpointUri) && !string.IsNullOrEmpty(Model.PersonalAccessToken))
            {
                var _ = Dispatcher.Invoke(() => LoadProjects(true));
            }
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("PageTitle"));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private bool IsConfigured()
        {
            if (string.IsNullOrEmpty(Model.EndpointUri) || string.IsNullOrEmpty(Model.PersonalAccessToken))
            {
                return false;
            }
            return true;
        }

        private async Task LoadProjects(bool silent = false)
        {
            if (_testLoading) return;
            _testLoading = true;
            _endpointService.Initialize(Model.EndpointUri, Model.PersonalAccessToken);
            try
            {
                if (!IsConfigured())
                {
                    throw new Exception("Missing endpoint uri or personal access token.");
                }
                Projects = (await _endpointService.GetAllProjects()).OrderBy(p => p.Name);
                if (!silent)
                {
                    _testSuccessful = true;
                    SuccessMessage = "Test Successful";
                    FailureMessage = "";
                }
            }
            catch (Exception ex)
            {
                if (!silent)
                {
                    _testSuccessful = false;
                    FailureMessage = ex.Message;
                    SuccessMessage = "";
                    //Tip_ConnectionError.IsOpen = true;
                }
            }
            finally
            {
                _testLoading = false;
                RefreshBindings();
            }
        }

        private void RefreshBindings()
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("TestLoading"));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("ConnectionSuccessful"));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("ConnectionFailed"));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("FailureMessage"));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("SuccessMessage"));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("TestSuccessful"));
        }

        private void ResetTestOn_TextChanged(object sender, TextChangedEventArgs e)
        {
            _testLoading = false;
            FailureMessage = "";
            //Tip_ConnectionError.IsOpen = false;
            _testSuccessful = null;
            RefreshBindings();
        }

        private void Button_GeneratePat_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("cmd", $"/C start {Model.EndpointUri}/_usersSettings/tokens")
            {
                WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden,
                CreateNoWindow = true,
                UseShellExecute = true
            });
        }

        private void Button_LoadProjects_Click(object sender, RoutedEventArgs e)
        {
            var _ = LoadProjects();
        }

        private void Combo_EndpointType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_resolver != null)
            {
                _endpointService = _resolver.Resolve(Model);
            }
        }

        private void Text_PersonalAccesToken_LostFocus(object sender, RoutedEventArgs e)
        {
            if (Projects == null || Projects.Count() == 0)
            {
                var _ = LoadProjects(true);
            }
        }

        private void Button_Next_Click(object sender, RoutedEventArgs e)
        {

            if (Mode == EndpointConfigMode.Source)
            {
                MainWindow.NavigateTo<WorkItemsPage>();
            } else
            {
                MainWindow.NavigateTo<RunMigrationPage>();
            }
        }

        private void Button_Test_Click(object sender, RoutedEventArgs e)
        {
            var _ = LoadProjects();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {

        }
        public static T GetChildOfType<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj == null) return null;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
            {
                var child = VisualTreeHelper.GetChild(depObj, i);

                var result = (child as T) ?? GetChildOfType<T>(child);
                if (result != null) return result;
            }
            return null;
        }

        private void PreviewTextInput_EnhanceComboSearch(object sender, TextCompositionEventArgs e)
        {
            ComboBox cmb = (ComboBox)sender;

            cmb.IsDropDownOpen = true;

            if (!string.IsNullOrEmpty(cmb.Text))
            {
                var c = GetChildOfType<TextBox>(cmb);
                if (c != null)
                {
                    string fullText = cmb.Text.Insert(c.CaretIndex, e.Text);
                    cmb.ItemsSource = Projects
                        .Where(s => s.Name.IndexOf(fullText, StringComparison.InvariantCultureIgnoreCase) != -1)
                        .Take(10).ToList();
                }
            }
            else if (!string.IsNullOrEmpty(e.Text))
            {
                cmb.ItemsSource = Projects
                    .Where(s => s.Name.IndexOf(e.Text, StringComparison.InvariantCultureIgnoreCase) != -1)
                    .Take(10).ToList();
            }
            else
            {
                cmb.ItemsSource = Projects.Take(10);
            }
        }

        private void PreviewKeyUp_EnhanceComboSearch(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Back || e.Key == Key.Delete)
            {
                ComboBox cmb = (ComboBox)sender;

                cmb.IsDropDownOpen = true;

                if (!string.IsNullOrEmpty(cmb.Text))
                {
                    cmb.ItemsSource = Projects
                        .Where(s => s.Name.IndexOf(cmb.Text, StringComparison.InvariantCultureIgnoreCase) != -1)
                        .Take(10).ToList();
                }
                else
                {
                    cmb.ItemsSource = Projects.Take(10);
                }
            }
        }

        private void Pasting_EnhanceComboSearch(object sender, DataObjectPastingEventArgs e)
        {
            ComboBox cmb = (ComboBox)sender;

            cmb.IsDropDownOpen = true;

            string pastedText = (string)e.DataObject.GetData(typeof(string));
            string fullText = cmb.Text.Insert(GetChildOfType<TextBox>(cmb).CaretIndex, pastedText);

            if (!string.IsNullOrEmpty(fullText))
            {
                cmb.ItemsSource = Projects
                    .Where(s => s.Name.IndexOf(fullText, StringComparison.InvariantCultureIgnoreCase) != -1)
                    .Take(10).ToList();
            }
            else
            {
                cmb.ItemsSource = Projects.Take(10);
            }
        }
    }
}
