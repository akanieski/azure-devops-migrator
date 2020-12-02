using AzureDevOpsMigrator.EndpointServices;
using AzureDevOpsMigrator.Migrators;
using AzureDevOpsMigrator.Models;
using AzureDevOpsMigrator.Services;
using AzureDevOpsMigrator.Windows.Models;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
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

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace AzureDevOpsMigrator.Windows.Views
{
    public record WorkItemSummary
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
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class QueryPage : Page
    {
        private IEndpointService _endpoint;

        public MigrationConfig Model => MainWindow.CurrentModel.CurrentConfig;
        public IEnumerable<WorkItemSummary> WorkItemResults { get; set; }
        public int Total { get; set; }
        public string NextMessage => $"Proceed with <{Total}> Work Items";

        public QueryPage()
        {
            this.InitializeComponent();
            Model.SourceQuery = Model.SourceQuery ?? "";
            _endpoint = (MainWindow.ServiceProvider.GetService(typeof(IEndpointServiceResolver)) as IEndpointServiceResolver).Resolve(Model.SourceEndpointConfig);
            var _ = LoadResults();
        }

        private async Task LoadResults()
        {
            try
            {
                _endpoint.Initialize(Model.SourceEndpointConfig.EndpointUri, Model.SourceEndpointConfig.PersonalAccessToken);
                var result = await _endpoint.GetWorkItemsAsync(
                    $"select * from WorkItems {(string.IsNullOrEmpty(Model.SourceQuery) ? "" : "where " + Model.SourceQuery)}", 
                    CancellationToken.None,
                    top: 10);
                Total = result.TotalCount;
                WorkItemResults = result.Items.Select(i => new WorkItemSummary(i));
                Bindings.Update();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
            }
        }

        private void Button_Refresh_Click(object sender, RoutedEventArgs e)
        {
            var _ = LoadResults();
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            MainWindow.SaveModel();
            base.OnNavigatingFrom(e);
        }

        private void Button_Next_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.NavigateTo<TransformationsPage>();
        }
    }
}
