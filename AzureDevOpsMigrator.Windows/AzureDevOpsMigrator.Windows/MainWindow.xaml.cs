using AzureDevOpsMigrator.EndpointServices;
using AzureDevOpsMigrator.Migrators;
using AzureDevOpsMigrator.Models;
using AzureDevOpsMigrator.Services;
using AzureDevOpsMigrator.Windows.Models;
using AzureDevOpsMigrator.Windows.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json;
using Windows.Foundation;
using Windows.Foundation.Collections;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Services.Organization.Client;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace AzureDevOpsMigrator.Windows
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        private static MainWindow _currentInstance;
        public MigrationViewModel Model => CurrentModel;
        public MainWindow()
        {
            this.InitializeComponent();
            Title = "Community Azure DevOps Migration Utility";
            AppFrame.Navigate(typeof(GettingStartedPage));
            _currentInstance = this;
        }

        public Frame AppFrame => frame;
        public string ConfirmationMessage { get; set; }

        public static void NavigateTo<TPage>(object parameter = null) where TPage : Page
        {
            if (CurrentModel.CurrentConfig != null &&
                !string.IsNullOrEmpty(CurrentModel.CurrentConfig.WorkingFolder) &&
                !string.IsNullOrEmpty(CurrentModel.CurrentConfig.Name))
            {
                SaveModel();
            }
            CurrentPage = _currentInstance.AppFrame.Navigate<TPage>(null, parameter);
        }
        public static MigrationViewModel CurrentModel { get; set; } = new MigrationViewModel();
        public static Page CurrentPage { get; set; }

        public static async Task<bool> Confirm(string msg = "")
        {
            _currentInstance.ConfirmationMessage = msg;
            return await _currentInstance.Dialog_Confirm.ShowAsync() == ContentDialogResult.Primary;
        }
        
        public static void SaveModel()
        {
            Directory.CreateDirectory(CurrentModel.CurrentConfig.WorkingFolder);
            File.WriteAllText(Path.Combine(CurrentModel.CurrentConfig.WorkingFolder, $"{CurrentModel.CurrentConfig.Name}.miproj"), JsonConvert.SerializeObject(CurrentModel.CurrentConfig, new JsonSerializerSettings()
            { 
                Formatting = Formatting.Indented
            }));
        }
        public static void LoadModel(string path)
        {
            // TODO: Add save changes dialog
            var config = JsonConvert.DeserializeObject<MigrationConfig>(File.ReadAllText(path)) as MigrationConfig;
            CurrentModel.CurrentConfig = config;
            InitializeHost();
            NavigateTo<GeneralPage>();
            _currentInstance.Bindings.Update();
        }

        private void NavigationViewControl_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            Type pageType = null;
            if (args.InvokedItemContainer == Menu_GettingStarted) 
            {
                pageType = typeof(GettingStartedPage);
            }
            else if (args.InvokedItemContainer == Menu_NewMigration)
            {
                pageType = typeof(GeneralPage);
            }
            else if (args.InvokedItemContainer == Menu_SourceConfig)
            {
                AppFrame.Navigate(typeof(EndpointPage), EndpointPageMode.Source);
            }
            else if (args.InvokedItemContainer == Menu_QueryPage)
            {
                pageType = typeof(QueryPage);
            }
            else if (args.InvokedItemContainer == Menu_Transformation)
            {
                pageType = typeof(TransformationsPage);
            }
            else if (args.InvokedItemContainer == Menu_TargetConfig)
            {
                AppFrame.Navigate(typeof(EndpointPage), EndpointPageMode.Target);
            }
            else if (args.InvokedItemContainer == Menu_RunMigration)
            {
                pageType = typeof(RunMigrationPage);
            }
            
            if (pageType != null)  AppFrame.Navigate(pageType);
        }

        private void OnNavigatingToPage(object sender, NavigatingCancelEventArgs e)
        {
            //if (e.NavigationMode == NavigationMode.Back)
            {
                if (e.SourcePageType == typeof(GettingStartedPage))
                {
                    NavigationViewControl.SelectedItem = Menu_GettingStarted;
                }
                else if (e.SourcePageType == typeof(GeneralPage))
                {
                    NavigationViewControl.SelectedItem = Menu_NewMigration;
                }
                else if (e.SourcePageType == typeof(EndpointPage) && e.Parameter.ToString() == Enum.GetName(EndpointPageMode.Source))
                {
                    NavigationViewControl.SelectedItem = Menu_SourceConfig;
                }
                else if (e.SourcePageType == typeof(QueryPage))
                {
                    NavigationViewControl.SelectedItem = Menu_QueryPage;
                }
                else if (e.SourcePageType == typeof(TransformationsPage))
                {
                    NavigationViewControl.SelectedItem = Menu_Transformation;
                }
                else if (e.SourcePageType == typeof(EndpointPage) && e.Parameter.ToString() == Enum.GetName(EndpointPageMode.Target))
                {
                    NavigationViewControl.SelectedItem = Menu_TargetConfig;
                }
                else if (e.SourcePageType == typeof(RunMigrationPage))
                {
                    NavigationViewControl.SelectedItem = Menu_RunMigration;
                }
                
            }
        }

        private void Window_Activated(object sender, WindowActivatedEventArgs args)
        {
            
        }


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
            Logged?.Invoke(null, new LogEventArgs($"[{Enum.GetName(logLevel)}] {state}", logLevel));
        }

        public void Dispose()
        {

        }
    }

    public static class FrameExtensions
    {
        /// <summary>
        /// Navigates to a page and returns the instance of the page if it succeeded,
        /// otherwise returns null.
        /// </summary>
        /// <typeparam name="TPage"></typeparam>
        /// <param name="frame"></param>
        /// <param name="transitionInfo">The navigation transition.
        /// Example: <see cref="DrillInNavigationTransitionInfo"/> or
        /// <see cref="SlideNavigationTransitionInfo"/></param>
        /// <returns></returns>
        public static TPage Navigate<TPage>(
            this Frame frame,
            NavigationTransitionInfo transitionInfo = null, object parameter = null)
            where TPage : Page
        {
            TPage view = null;
            void OnNavigated(object s, NavigationEventArgs args)
            {
                frame.Navigated -= OnNavigated;
                view = args.Content as TPage;
            }

            frame.Navigated += OnNavigated;

            frame.Navigate(typeof(TPage), parameter, transitionInfo);
            return view;
        }
    }
}
