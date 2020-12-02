using AzureDevOpsMigrator.Windows.Models;
using Microsoft.TeamFoundation.Test.WebApi;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using WinRT;
using Windows.System;
using System.Threading.Tasks;
using System.Net.Http;
using Windows.UI.Popups;
using Windows.UI.Core;
using System.Net.Http.Headers;
using Microsoft.TeamFoundation.Core.WebApi;
using AzureDevOpsMigrator.Migrators;
using AzureDevOpsMigrator.EndpointServices;
using AzureDevOpsMigrator.Services;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace AzureDevOpsMigrator.Windows.Views
{
    public enum EndpointPageMode
    {
        Source,
        Target
    }
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class EndpointPage : Page
    {
        public string PageTitle => $"{Enum.GetName(_mode)} Endpoint Configuration";
        public AzureDevOpsMigrator.Models.EndpointConfig Model { get; set; }
        private EndpointPageMode _mode;
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
                Bindings.Update();
            } 
        }
        public Visibility ConnectionSuccessful => _testSuccessful == true ? Visibility.Visible : Visibility.Collapsed;
        public Visibility ConnectionFailed => _testSuccessful == false ? Visibility.Visible : Visibility.Collapsed;
        public EndpointPage()
        {
            this.InitializeComponent();
            _resolver = MainWindow.ServiceProvider.GetService(typeof(IEndpointServiceResolver)) as IEndpointServiceResolver;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"Navigated to Endpoint Page in {e.Parameter} mode.");
            _mode = (EndpointPageMode)Enum.Parse(typeof(EndpointPageMode), e.Parameter.ToString());
            switch(_mode)
            {
                case EndpointPageMode.Source:
                    Model = MainWindow.CurrentModel.CurrentConfig.SourceEndpointConfig = MainWindow.CurrentModel.CurrentConfig.SourceEndpointConfig ?? new AzureDevOpsMigrator.Models.EndpointConfig();
                    _endpointService = _resolver.Resolve(Model);
                    break;
                case EndpointPageMode.Target:
                    Model = MainWindow.CurrentModel.CurrentConfig.TargetEndpointConfig = MainWindow.CurrentModel.CurrentConfig.TargetEndpointConfig ?? new AzureDevOpsMigrator.Models.EndpointConfig();
                    _endpointService = _resolver.Resolve(Model);
                    break;
            }
            if (!string.IsNullOrEmpty(Model.EndpointUri) && !string.IsNullOrEmpty(Model.PersonalAccessToken))
            {
                var _ = Dispatcher.RunAsync(CoreDispatcherPriority.High, () => LoadProjects(true));
            }
        }

        private void Button_Next_Click(object sender, RoutedEventArgs e)
        {
            if (_mode == EndpointPageMode.Source)
            {
                MainWindow.NavigateTo<QueryPage>();
            }
        }

        private void Button_GeneratePat_Click(object sender, RoutedEventArgs e)
        {
            Launcher.LaunchUriAsync(new Uri($"{Model.EndpointUri}/_usersSettings/tokens"));
        }

        private void Button_Test_Click(object sender, RoutedEventArgs e)
        {
            var _ = Dispatcher.RunAsync(CoreDispatcherPriority.High, () => LoadProjects());
        }

        private void Button_LoadProjects_Click(object sender, RoutedEventArgs e)
        {
            var _ = Dispatcher.RunAsync(CoreDispatcherPriority.High, async () =>
            {
                await LoadProjects();
                Suggest_Project.IsSuggestionListOpen = true;
            });
        }

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
                Projects = await _endpointService.GetAllProjects();
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
                    Tip_ConnectionError.IsOpen = true;
                }
            }
            finally
            {
                _testLoading = false;
                Bindings.Update();
            }
        }

        private void Combo_EndpointType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_resolver != null)
            {
                _endpointService = _resolver.Resolve(Model);
            }
        }


        private void ResetTestOn_TextChanged(object sender, TextChangedEventArgs e)
        {
            _testLoading = false;
            FailureMessage = "";
            Tip_ConnectionError.IsOpen = false;
            _testSuccessful = null;
            //Bindings.Update();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            Text_EndpointUri.Focus(FocusState.Programmatic);
        }

        private void Suggest_Project_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            // Only get results when it was a user typing,
            // otherwise assume the value got filled in by TextMemberPath
            // or the handler for SuggestionChosen.
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                //Set the ItemsSource to be your filtered dataset
                //sender.ItemsSource = dataset;
                sender.ItemsSource = (Projects ?? new List<TeamProjectReference>())
                    .Where(p => p.Name.ToLower().Contains(sender.Text.ToLower()));
            }
        }

        private void Suggest_Project_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            if (args.ChosenSuggestion != null)
            {
                // User selected an item from the suggestion list, take an action on it here.
                sender.Text = (args.ChosenSuggestion as TeamProjectReference).Name;
            }
            else
            {
                // Use args.QueryText to determine what to do.
                sender.ItemsSource = (Projects ?? new List<TeamProjectReference>())
                    .Where(p => p.Name.ToLower().Contains(args.QueryText.ToLower()));
            }
        }

        private void Suggest_Project_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            // Set sender.Text. You can use args.SelectedItem to build your text string.
            sender.Text = (args.SelectedItem as TeamProjectReference).Name;
        }


        private void Text_PersonalAccesToken_LostFocus(object sender, RoutedEventArgs e)
        {
            if (Projects == null || Projects.Count() == 0)
            {
                var _ = Dispatcher.RunAsync(CoreDispatcherPriority.High, async () =>
                {
                    await LoadProjects(true);
                });
            }
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            MainWindow.SaveModel();
            base.OnNavigatingFrom(e);
        }
    }
}
