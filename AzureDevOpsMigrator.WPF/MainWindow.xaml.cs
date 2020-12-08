using AzureDevOpsMigrator.WPF.Pages;
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
using AzureDevOpsMigrator.Models;
using System.Collections.ObjectModel;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.Common;
using System.IO;
using Newtonsoft.Json;
using Path = System.IO.Path;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using AzureDevOpsMigrator.Services;
using AzureDevOpsMigrator.Migrators;
using AzureDevOpsMigrator.EndpointServices;
using Microsoft.Extensions.Logging;
using System.Reflection;
using System.Collections.Specialized;

namespace AzureDevOpsMigrator.WPF
{
    public class BaseViewModel : INotifyPropertyChanged
    {

        public event PropertyChangedEventHandler PropertyChanged;
        public T SignalChange<T>(T value, params string[] propertyNames)
        {
            propertyNames.ForEach(prop => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop)));
            return value;
        }
    }
    public class MigrationViewModel : BaseViewModel
    {
        public Visibility DependsOnCurrentConfigBeingSet => _currentConfig != null ? Visibility.Visible : Visibility.Collapsed;
        private MigrationConfig _currentConfig = null;
        public MigrationConfig CurrentConfig
        {
            get => _currentConfig;
            set => SignalChange(_currentConfig = value,
                "CurrentConfig",
                "DependsOnCurrentConfigBeingSet");
        }
        private ObservableCollection<WorkItem> _previewedWorkItems;
        public ObservableCollection<WorkItem> PreviewedWorkItems
        {
            get => _previewedWorkItems;
            set => SignalChange(_previewedWorkItems = value, "PreviewedWorkItems");
        }
        private bool _saved;
        public bool Saved
        {
            get => _saved;
            set => SignalChange(_saved = value, "Saved");
        }
        private string _workingFolder;
        public string WorkingFolder
        {
            get => _workingFolder;
            set => SignalChange(_workingFolder = value, "WorkingFolder");
        }

        public void StartNew()
        {

            CurrentConfig = new Models.MigrationConfig();
            Saved = false;
            WorkingFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        }
    }
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private static MainWindow _currentInstance;
        public static MigrationViewModel CurrentModel { get; set; } = new MigrationViewModel();
        public static Page CurrentPage { get; set; }
        public MigrationViewModel Model => CurrentModel;
        public static IServiceProvider ServiceProvider { get; set; }
        public static void InitializeHost()
        {
            if (ServiceProvider == null)
            {
                var host = new HostBuilder()
                            .ConfigureHostConfiguration(c =>
                            {

                            })
                            .ConfigureServices((c, x) => ConfigureServices(c, x))
                            .Build();

                //Save our service provider so we can use it later.
                ServiceProvider = host.Services;
            }
        }

        static void ConfigureServices(HostBuilderContext ctx, IServiceCollection services)
        {
            services.AddLogging(builder => builder.AddGUILogger());
            services.AddSingleton((provider) => CurrentModel.CurrentConfig);
            services.AddSingleton<IEndpointServiceResolver, EndpointServiceResolver>();
            services.AddTransient<RestEndpointService>();
            services.AddSingleton<IOrchestratorService, OrchestratorService>();
            services.AddSingleton<IWorkItemMigrator, WorkItemMigrator>();
            services.AddSingleton<IIterationMigrator, IterationMigrator>();
            services.AddSingleton<IAreaPathMigrator, AreaPathMigrator>();
        }


        public static void SaveModel()
        {
            if (!string.IsNullOrEmpty(CurrentModel.WorkingFolder) && !string.IsNullOrEmpty(CurrentModel.CurrentConfig.Name))
            {
                CurrentModel.WorkingFolder = Path.GetFullPath(CurrentModel.WorkingFolder);
                Directory.CreateDirectory(CurrentModel.WorkingFolder);
                var path = Path.Combine(CurrentModel.WorkingFolder, $"{CurrentModel.CurrentConfig.Name}.miproj");
                File.WriteAllText(path, JsonConvert.SerializeObject(CurrentModel.CurrentConfig, new JsonSerializerSettings()
                {
                    Formatting = Formatting.Indented
                }));
                if (!Properties.Settings.Default.RecentMigrations.Contains(path))
                {
                    Properties.Settings.Default.RecentMigrations.Add(path);
                }
                Properties.Settings.Default.Save();
                CurrentModel.Saved = true;
            }
        }
        public static void LoadModel(string path = null)
        {
            if (path == null)
            {
                CurrentModel.CurrentConfig = new MigrationConfig();
            }
            else
            {
                var config = JsonConvert.DeserializeObject<MigrationConfig>(File.ReadAllText(path)) as MigrationConfig;
                CurrentModel.Saved = true;
                CurrentModel.CurrentConfig = config;
            }
            InitializeHost();
            _currentInstance.PropertyChanged?.Invoke(_currentInstance, new PropertyChangedEventArgs("ShownWhileEditing"));
            _currentInstance.Model.WorkingFolder = new FileInfo(path).DirectoryName;
            NavigateTo<GeneralPage>();
        }

        public static void NavigateTo<TPage>(object parameter = null) where TPage : Page
        {
            if (CurrentModel.CurrentConfig != null &&
                !string.IsNullOrEmpty(CurrentModel.WorkingFolder) &&
                !string.IsNullOrEmpty(CurrentModel.CurrentConfig.Name))
            {
                SaveModel();
            }
            CurrentPage = parameter == null ? Activator.CreateInstance(typeof(TPage)) as Page  : Activator.CreateInstance(typeof(TPage), parameter) as Page;
            _currentInstance.AppFrame.Navigate(CurrentPage);
            RefreshBindings();
            _currentInstance.Title = _currentInstance.CustomTitle;
        }

        #region Nav Styles ...
        private Style _activeNavButtonStyle;
        private Style _navButtonStyle;
        private Style _navButtonStyleHidden;

        public Style AboutPageStyle => CurrentPage is AboutPage ? _activeNavButtonStyle : null;
        public Style WorkItemsPageStyle => CurrentPage is WorkItemsPage ? _activeNavButtonStyle : ShownWhileEditing && Model.CurrentConfig != null ? _navButtonStyle : null;
        public Style RunMigrationPageStyle => CurrentPage is RunMigrationPage ? _activeNavButtonStyle : ShownWhileEditing && Model.CurrentConfig != null ? _navButtonStyle : null;
        public Style CloseMigrationStyle => ShownWhileEditing && Model.CurrentConfig != null ? _navButtonStyle : null;
        public Style GettingStartedPageStyle =>  CurrentPage is GettingStartedPage ? _activeNavButtonStyle : ShownWhileEditing && Model.CurrentConfig != null ? _navButtonStyle : null;
        public Style GeneralPageStyle => CurrentPage is GeneralPage ? _activeNavButtonStyle : Model.CurrentConfig != null ? _navButtonStyle : null;
        public Style TargetEndpointPageStyle => CurrentPage is EndpointPage && (CurrentPage as EndpointPage).Mode == EndpointConfigMode.Target ? _activeNavButtonStyle : ShownWhileEditing && Model.CurrentConfig != null ? _navButtonStyle : null;
        public Style SourceEndpointPageStyle => CurrentPage is EndpointPage && (CurrentPage as EndpointPage).Mode == EndpointConfigMode.Source ? _activeNavButtonStyle : ShownWhileEditing && Model.CurrentConfig != null ? _navButtonStyle : null;
        public bool ShownWhileEditing => CurrentModel.CurrentConfig != null && CurrentModel.Saved;
        public bool ShownWhileNew => CurrentModel.CurrentConfig != null && !CurrentModel.Saved;
        #endregion

        public string Version => $"v{Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyFileVersionAttribute>().Version}";
        public string CustomTitle => $"Azure DevOps Migration Utility {(CurrentModel?.CurrentConfig != null ? "- " + CurrentModel.CurrentConfig.Name : "")}";

        public static void RefreshBindings()
        {
            _currentInstance.PropertyChanged?.Invoke(_currentInstance, new PropertyChangedEventArgs("Title"));
            _currentInstance.PropertyChanged?.Invoke(_currentInstance, new PropertyChangedEventArgs("CurrentPage"));
            _currentInstance.PropertyChanged?.Invoke(_currentInstance, new PropertyChangedEventArgs("SourceEndpointPageStyle"));
            _currentInstance.PropertyChanged?.Invoke(_currentInstance, new PropertyChangedEventArgs("TargetEndpointPageStyle"));
            _currentInstance.PropertyChanged?.Invoke(_currentInstance, new PropertyChangedEventArgs("GeneralPageStyle"));
            _currentInstance.PropertyChanged?.Invoke(_currentInstance, new PropertyChangedEventArgs("WorkItemsPageStyle"));
            _currentInstance.PropertyChanged?.Invoke(_currentInstance, new PropertyChangedEventArgs("HistoryPageStyle"));
            _currentInstance.PropertyChanged?.Invoke(_currentInstance, new PropertyChangedEventArgs("GettingStartedPageStyle"));
            _currentInstance.PropertyChanged?.Invoke(_currentInstance, new PropertyChangedEventArgs("RunMigrationPageStyle"));
            _currentInstance.PropertyChanged?.Invoke(_currentInstance, new PropertyChangedEventArgs("CloseMigrationStyle")); 
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                try
                {
                    this.DragMove();
                }
                catch
                {
                }
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            Title = CustomTitle;
            _currentInstance = this;
            _activeNavButtonStyle = Application.Current.FindResource("NavButtonActive") as Style;
            _navButtonStyle = Application.Current.FindResource("NavButton") as Style;
            NavigateTo<GettingStartedPage>();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void Nav_GettingStarted_Clicked(object sender, EventArgs e) => NavigateTo<GettingStartedPage>();

        private void Nav_TargetEndpoint_Clicked(object sender, RoutedEventArgs e) => NavigateTo<EndpointPage>(EndpointConfigMode.Target);

        private void Nav_SourceEndpoint_Clicked(object sender, RoutedEventArgs e) => NavigateTo<EndpointPage>(EndpointConfigMode.Source);

        private void Nav_General_Clicked(object sender, RoutedEventArgs e) => NavigateTo<GeneralPage>();

        private void Nav_WorkItems_Clicked(object sender, RoutedEventArgs e) => NavigateTo<WorkItemsPage>();

        private void Nav_RunMigration_Clicked(object sender, RoutedEventArgs e) => NavigateTo<RunMigrationPage>();

        private void Nav_About_Clicked(object sender, RoutedEventArgs e) => NavigateTo<AboutPage>();

        private void Nav_ReportFeedback_Clicked(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("cmd", "/C start https://github.com/akanieski/azure-devops-migrator/issues/new/choose")
            {
                WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden,
                CreateNoWindow = true,
                UseShellExecute = true
            });
        }

        private void Nav_CLoseMigration_Clicked(object sender, RoutedEventArgs e)
        {
            CurrentModel.CurrentConfig = null;
            NavigateTo<GettingStartedPage>();
        }
    }

    public static class BrowserBehavior
    {
        public static readonly DependencyProperty HtmlProperty = DependencyProperty.RegisterAttached(
            "Html",
            typeof(string),
            typeof(BrowserBehavior),
            new FrameworkPropertyMetadata(OnHtmlChanged));

        [AttachedPropertyBrowsableForType(typeof(WebBrowser))]
        public static string GetHtml(WebBrowser d)
        {
            return (string)d.GetValue(HtmlProperty);
        }

        public static void SetHtml(WebBrowser d, string value)
        {
            d.SetValue(HtmlProperty, value);
        }

        static void OnHtmlChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            WebBrowser wb = d as WebBrowser;
            if (wb != null)
                wb.NavigateToString(e.NewValue as string);
        }
    }

    public class LogEventArgs : EventArgs
    {
        public string Message { get; set; }
        public LogLevel Level { get; set; }
        public LogEventArgs(string message, LogLevel level)
        {
            Message = message;
            Level = level;
        }
    }
    public class GUILoggerProvider : ILoggerProvider
    {
        private GUILogger _logger;
        public ILogger CreateLogger(string categoryName)
        {
            _logger = _logger ?? new GUILogger();
            return _logger;
        }

        public void Dispose()
        {
            _logger.Dispose();
        }
    }
    public static class ILoggingBuilderExtensions
    {
        public static ILoggingBuilder AddGUILogger(this ILoggingBuilder builder)
        {
            builder.AddProvider(new GUILoggerProvider());
            return builder;
        }
    }
    public class GUILogger : ILogger, IDisposable
    {
        public static event LogEventHandler Logged;
        public delegate void LogEventHandler(object sender, LogEventArgs eventArgs);
        public IDisposable BeginScope<TState>(TState state)
        {
            throw new NotImplementedException();
        }

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            Logged?.Invoke(null, new LogEventArgs($"[{Enum.GetName(typeof(LogLevel), logLevel)}] {state}", logLevel));
        }

        public void Dispose()
        {

        }
    }
}
