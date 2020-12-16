using AzureDevOpsMigrator.Migrators;
using AzureDevOpsMigrator.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
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
    /// <summary>
    /// Interaction logic for RunMigrationPage.xaml
    /// </summary>
    public partial class RunMigrationPage : Page, INotifyPropertyChanged
    {
        private IOrchestratorService _orchestrator;
        private const int MAX_LOG_SIZE = 1000000;
        private CancellationTokenSource _cancellationTokenSource;

        public event PropertyChangedEventHandler PropertyChanged;

        private List<(int Increment, SyncState State, EntityType Type)> Logs { get; set; } = new List<(int Increment, SyncState State, EntityType Type)>();

        public bool? IsRunning { get; set; } = null;
        public Visibility RunShown => !IsRunning.HasValue ? Visibility.Visible : Visibility.Collapsed;
        public Visibility CancelShown => IsRunning.HasValue && IsRunning.Value ? Visibility.Visible : Visibility.Collapsed;
        public Visibility ResetShown => IsRunning.HasValue && !IsRunning.Value ? Visibility.Visible : Visibility.Collapsed;
        public MigrationPlan Plan { get; set; }
        public int CurrentCount { get; set; }
        public int CurrentMax { get; set; }
        public MigrationConfig Config => MainWindow.CurrentModel.CurrentConfig;


        public RunMigrationPage()
        {
            InitializeComponent();
            DataContext = this;
            GUILogger.Logged += GUILogger_Logged;
            _orchestrator = MainWindow.ServiceProvider.GetService(typeof(IOrchestratorService)) as IOrchestratorService;
            _orchestrator.StatusChanged += Orchestrator_StatusChanged;
        }

        private void Orchestrator_StatusChanged(object source, (int Increment, SyncState State, EntityType Type) eventArgs)
        {
            Dispatcher.Invoke(() =>
            {
                CurrentCount += eventArgs.Increment;
                Bar_Progress.Value = CurrentCount;
                Text_Progress.Text = ($"{CurrentCount}/{CurrentMax}");
                Logs.Add(eventArgs);
            });
        }
        private void GUILogger_Logged(object sender, LogEventArgs eventArgs)
        {
            Dispatcher.Invoke(() =>
            {
                Text_Logs.Text = $"{new string(Text_Logs.Text?.TakeLast(MAX_LOG_SIZE).ToArray())}{eventArgs.Message}{Environment.NewLine}";
                ScrollBottom();
            });
        }
        private void ScrollTop() => Dispatcher.Invoke(() => Scroll_Log.ScrollToVerticalOffset(0));
        private void ScrollBottom() => Dispatcher.Invoke(() => Scroll_Log.ScrollToVerticalOffset(Scroll_Log.ScrollableHeight));
        
        private void RefreshBindings()
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(RunShown)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CancelShown)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ResetShown)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsRunning)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentCount)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentMax)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Plan)));
        }
        private void Run()
        {
            if (!IsRunning.HasValue || IsRunning.Value == false)
            {
                IsRunning = true;
                Logs.Clear();
                RefreshBindings();
                _cancellationTokenSource = new CancellationTokenSource();
                var _ = Task.Run(async () =>
                {
                    try
                    {
                        Plan = await _orchestrator.BuildMigrationPlan(CancellationToken.None);
                        await Dispatcher.InvokeAsync(() => ResetProgressBar());
                        RefreshBindings();
                        await _orchestrator.ExecuteAsync(Plan, _cancellationTokenSource.Token);
                        IsRunning = false;
                        RefreshBindings();
                        await Task.Delay(500);
                        ScrollBottom();
                    }
                    catch (OperationCanceledException opCanceledEx)
                    {
                        IsRunning = false;
                        RefreshBindings();
                        await Task.Delay(500);
                        ScrollBottom();
                    }
                    catch (Exception ex)
                    {
                        GUILogger_Logged(this, new LogEventArgs(ex.ToString(), Microsoft.Extensions.Logging.LogLevel.Error));
                        IsRunning = false;
                        RefreshBindings();
                        await Task.Delay(500);
                        ScrollBottom();
                    }
                });
            }
        }

        private void ResetProgressBar()
        {
            CurrentCount = 0;
            Bar_Progress.Maximum = (Plan?.WorkItemsCount ?? 0) + (Plan?.AreaPathsCount ?? 0) + (Plan?.IterationsCount ?? 0);
            CurrentMax = (int)Bar_Progress.Maximum;
            Text_Progress.Text = $"{CurrentCount}/{CurrentMax}";
            Bar_Progress.Value = 0;
            Bar_Progress.Minimum = 0;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
        }

        private void Btn_Cancel(object sender, RoutedEventArgs e)
        {
            _cancellationTokenSource.Cancel();
        }

        private void Btn_Reset(object sender, RoutedEventArgs e)
        {
            Text_Logs.Text = "";
            IsRunning = null;
            RefreshBindings();
        }

        private void Btn_Run(object sender, RoutedEventArgs e)
        {
            Run();
        }
    }
}
