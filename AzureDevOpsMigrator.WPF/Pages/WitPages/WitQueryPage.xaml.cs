using AzureDevOpsMigrator.EndpointServices;
using AzureDevOpsMigrator.Models;
using AzureDevOpsMigrator.Services;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

namespace AzureDevOpsMigrator.WPF.Pages.WitPages
{
    public class WorkItemSummary
    {
        private WorkItem _wit;
        public WorkItemSummary(WorkItem wit)
        {
            _wit = wit;
        }
        public int Id => _wit.Id.HasValue ? _wit.Id.Value : 0;
        public int Rev => _wit.Rev.HasValue ? _wit.Rev.Value : 0;
        public string ProjectName => _wit.Fields["System.TeamProject"].ToString();
        public string Title => _wit.Fields["System.Title"].ToString();
        public string WorkItemType => _wit.Fields["System.WorkItemType"].ToString();
        public string AreaPath => _wit.Fields["System.AreaPath"].ToString();
        public string IterationPath => _wit.Fields["System.IterationPath"].ToString();
    }

    /// <summary>
    /// Interaction logic for WitQueryPage.xaml
    /// </summary>
    public partial class WitQueryPage : Page, INotifyPropertyChanged
    {
        private IEndpointService _endpoint;

        public event PropertyChangedEventHandler PropertyChanged;

        public MigrationConfig Model => MainWindow.CurrentModel.CurrentConfig;
        public ObservableCollection<WorkItemSummary> Results { get; set; } = new ObservableCollection<WorkItemSummary>();
        public int Total { get; set; }
        public bool HasRecords { get; set; }

        public WitQueryPage()
        {
            InitializeComponent();
            _endpoint = (MainWindow.ServiceProvider.GetService(typeof(IEndpointServiceResolver)) as IEndpointServiceResolver).Resolve(Model.SourceEndpointConfig);
            DataContext = this;
            Load();
        }

        private void Button_Load_Click(object sender, RoutedEventArgs e)
        {
            Load();
        }

        private async Task Load()
        {
            _endpoint.Initialize(Model.SourceEndpointConfig.EndpointUri, Model.SourceEndpointConfig.PersonalAccessToken);
            try
            {
                var result = await _endpoint.GetWorkItemsAsync(
                    $"select * from WorkItems {(string.IsNullOrEmpty(Model.SourceQuery) ? "" : "where " + Model.SourceQuery)}",
                    CancellationToken.None,
                    top: 100);
                Total = result.TotalCount;
                Results.Clear();
                Results.AddRange(result.Items.Select(item => new WorkItemSummary(item)));
                HasRecords = Total > 0;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Total)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasRecords)));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

    }
}
