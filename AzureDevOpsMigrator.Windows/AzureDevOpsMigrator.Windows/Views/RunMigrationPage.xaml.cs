using AzureDevOpsMigrator.Migrators;
using AzureDevOpsMigrator.Models;
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
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace AzureDevOpsMigrator.Windows.Views
{
    public enum PageMode
    {
        Planning,
        Running,
        Finished
    }
    public sealed partial class RunMigrationPage : Page
    {
        private IOrchestratorService _orchestrator;
        public bool PlanExpanded => Mode == PageMode.Planning;
        public bool LogsExpanded => Mode == PageMode.Running || Mode == PageMode.Finished;
        public bool ResultsExpanded => Mode == PageMode.Finished;
        public PageMode Mode { get; set; } = PageMode.Planning;
        public Visibility HiddenWhileRunning => LogsExpanded ? Visibility.Collapsed : Visibility.Visible;
        public Visibility PlanShown => PlanExpanded ? Visibility.Visible : Visibility.Collapsed;
        public Visibility LogsShown => LogsExpanded ? Visibility.Visible : Visibility.Collapsed;
        public Visibility ResultShown => ResultsExpanded ? Visibility.Visible : Visibility.Collapsed;
        public Visibility ShownWhileRunning => Mode == PageMode.Running ? Visibility.Visible : Visibility.Collapsed;
        public Visibility ShownWhileFinished => Mode == PageMode.Finished ? Visibility.Visible : Visibility.Collapsed;
        public MigrationConfig Config => MainWindow.CurrentModel.CurrentConfig;
        public MigrationPlan Plan { get; set; }
        public int CurrentCount { get; set; }
        public int CurrentMax { get; set; }
        private List<(int Increment, SyncState State, EntityType Type)> Logs { get; set; } = new List<(int Increment, SyncState State, EntityType Type)>();
        public RunMigrationPage()
        {
            this.InitializeComponent();
            PlanLoading = true;
            GUILogger.Logged += GUILogger_Logged;
            _orchestrator = MainWindow.ServiceProvider.GetService(typeof(IOrchestratorService)) as IOrchestratorService;
            _orchestrator.StatusChanged += Orchestrator_StatusChanged;
            
        }

        private const int MAX_LOG_SIZE = 1000000;
        private void GUILogger_Logged(object sender, LogEventArgs eventArgs)
        {
            var _ = Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
            {
                Text_Log.Text = $"{new string(Text_Log.Text?.TakeLast(MAX_LOG_SIZE).ToArray())}{Environment.NewLine}{eventArgs.Message}";
                Scroll_Log.ScrollToVerticalOffset(Scroll_Log.ScrollableHeight);
            });
        }

        private void Orchestrator_StatusChanged(object source, (int Increment, SyncState State, EntityType Type) eventArgs)
        {
            var _ = Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
            {
                CurrentCount += eventArgs.Increment;
                ProgressBar.Value = CurrentCount;
                Logs.Add(eventArgs);
            });
        }


        public bool PlanLoading { get; set; }
        public Visibility ShownWhenLoading => PlanLoading ? Visibility.Visible : Visibility.Collapsed;
        public Visibility HiddenWhenLoading => !PlanLoading ? Visibility.Visible : Visibility.Collapsed;
        private void LoadPlan()
        {
            PlanLoading = true;
            var _ = Dispatcher.RunAsync(CoreDispatcherPriority.High, () =>
            {
                ResetProgressBar();
                Bindings.Update();
                Task.Run(async () =>
                {
                    try
                    {
                        Plan = await _orchestrator.BuildMigrationPlan(CancellationToken.None);
                        PlanLoading = false;
                        var _ = Dispatcher.RunAsync(CoreDispatcherPriority.High, () =>
                        {
                            ResetProgressBar();
                            Bindings.Update();
                        });
                    }
                    catch (Exception ex)
                    {
                        // TODO: Add error handling
                    }
                });
            });
        }

        private void ResetProgressBar()
        {
            CurrentCount = 0;
            ProgressBar.Maximum = (Plan?.WorkItemsCount ?? 0) + (Plan?.AreaPathsCount ?? 0) + (Plan?.IterationsCount ?? 0);
            ProgressBar.Value = 0;
            ProgressBar.Minimum = 0;
        }

        private CancellationTokenSource _cancellationTokenSource;
        private void Button_Run_Click(object sender, RoutedEventArgs e)
        {
            Mode = PageMode.Running;
            ResetProgressBar();
            _cancellationTokenSource = new CancellationTokenSource();
            var _ = Task.Run(async () =>
            {
                try
                {
                    await _orchestrator.ExecuteAsync(Plan, _cancellationTokenSource.Token);

                    var _ = Dispatcher.RunAsync(CoreDispatcherPriority.High, () =>
                    {
                        Mode = PageMode.Finished;
                        Bindings.Update();
                        Scroll_Log.ScrollToVerticalOffset(Scroll_Log.ScrollableHeight);
                        Bindings.Update();
                    });
                }
                catch (OperationCanceledException opCanceledEx)
                {
                    var _ = Dispatcher.RunAsync(CoreDispatcherPriority.High, () =>
                    {
                        Mode = PageMode.Finished;
                        Bindings.Update();
                        Scroll_Log.ScrollToVerticalOffset(Scroll_Log.ScrollableHeight);
                        Bindings.Update();
                    });
                }
                catch (Exception ex)
                {
                    var _ = Dispatcher.RunAsync(CoreDispatcherPriority.High, () =>
                    {
                        Mode = PageMode.Finished;
                        Bindings.Update();
                        Scroll_Log.ScrollToVerticalOffset(Scroll_Log.ScrollableHeight);
                        Bindings.Update();
                    });
                }
            });

            Bindings.Update();
        }

        private void Button_RefreshPlan_Click(object sender, RoutedEventArgs e)
        {
            Mode = PageMode.Planning;
            ResetProgressBar();
            LoadPlan();
            Bindings.Update();
        }

        private void Button_Cancel_Click(object sender, RoutedEventArgs e)
        {
            _cancellationTokenSource.Cancel();
        }

        private void Button_ScrollTop_Click(object sender, RoutedEventArgs e)
        {
            Scroll_Log.ScrollToVerticalOffset(0);
        }

        private void Button_ScrollBottom_Click(object sender, RoutedEventArgs e)
        {
            Scroll_Log.ScrollToVerticalOffset(Scroll_Log.ScrollableHeight);
        }

        private void Scroll_Log_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Scroll_Log.ScrollToVerticalOffset(Scroll_Log.ScrollableHeight);
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            LoadPlan();
        }
    }
}
