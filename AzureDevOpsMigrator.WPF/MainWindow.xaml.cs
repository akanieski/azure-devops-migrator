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
            var host = new HostBuilder()
                        .ConfigureHostConfiguration(c =>
                        {

                        })
                        .ConfigureServices((c, x) => ConfigureServices(c, x))
                        .Build();

            //Save our service provider so we can use it later.
            ServiceProvider = host.Services;
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
            Directory.CreateDirectory(CurrentModel.CurrentConfig.WorkingFolder);
            var path = Path.Combine(CurrentModel.CurrentConfig.WorkingFolder, $"{CurrentModel.CurrentConfig.Name}.miproj");
            File.WriteAllText(path, JsonConvert.SerializeObject(CurrentModel.CurrentConfig, new JsonSerializerSettings()
            {
                Formatting = Formatting.Indented
            }));
            if (!Properties.Settings.Default.RecentMigrations.Contains(path))
            {
                Properties.Settings.Default.RecentMigrations.Add(path);
            }
            Properties.Settings.Default.Save();
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
                CurrentModel.CurrentConfig = config;
            }
            InitializeHost();
            _currentInstance.PropertyChanged?.Invoke(_currentInstance, new PropertyChangedEventArgs("ShownWhileEditing"));
            NavigateTo<GeneralPage>();
        }

        public static void NavigateTo<TPage>(params object[] parameters) where TPage : Page
        {
            if (CurrentModel.CurrentConfig != null &&
                !string.IsNullOrEmpty(CurrentModel.CurrentConfig.WorkingFolder) &&
                !string.IsNullOrEmpty(CurrentModel.CurrentConfig.Name))
            {
                SaveModel();
            }
            CurrentPage = parameters.Count() == 0 ? Activator.CreateInstance(typeof(TPage)) as Page  : Activator.CreateInstance(typeof(TPage), parameters) as Page;
            _currentInstance.AppFrame.Navigate(CurrentPage);
            _currentInstance._PageChanged();
            _currentInstance.Title = _currentInstance.CustomTitle;
        }

        #region Nav Styles ...
        private Style _activeNavButtonStyle;
        private Style __navButtonStyle;
        
        public Style AboutPageStyle => CurrentPage is AboutPage ? _activeNavButtonStyle : null;
        public Style WorkItemsPageStyle => CurrentPage is WorkItemsPage ? _activeNavButtonStyle : null;
        public Style RunMigrationPageStyle => CurrentPage is RunMigrationPage ? _activeNavButtonStyle : null;
        public Style GettingStartedPageStyle => CurrentPage is GettingStartedPage ? _activeNavButtonStyle : null;
        public Style GeneralPageStyle => CurrentPage is GeneralPage ? _activeNavButtonStyle : null;
        public Style TargetEndpointPageStyle => CurrentPage is EndpointPage && (CurrentPage as EndpointPage).Mode == EndpointConfigMode.Target ? _activeNavButtonStyle : null;
        public Style SourceEndpointPageStyle => CurrentPage is EndpointPage && (CurrentPage as EndpointPage).Mode == EndpointConfigMode.Source ? _activeNavButtonStyle : null;
        public Visibility ShownWhileEditing => CurrentModel.CurrentConfig != null ? Visibility.Visible : Visibility.Collapsed;
        #endregion

        public string Version => $"v{Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyFileVersionAttribute>().Version}";
        public string CustomTitle => $"Azure DevOps Migration Utility {(CurrentModel?.CurrentConfig != null ? "- " + CurrentModel.CurrentConfig.Name : "")}";

        private void _PageChanged()
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Title"));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("CurrentPage"));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("SourceEndpointPageStyle"));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("TargetEndpointPageStyle"));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("GeneralPageStyle"));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("WorkItemsPageStyle"));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("HistoryPageStyle"));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("GettingStartedPageStyle"));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("RunMigrationPageStyle"));
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
            __navButtonStyle = Application.Current.FindResource("NavButton") as Style;
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
            NavigateTo<GettingStartedPage>();
            CurrentModel.CurrentConfig = null;
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
