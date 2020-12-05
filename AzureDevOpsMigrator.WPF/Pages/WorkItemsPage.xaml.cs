using AzureDevOpsMigrator.WPF.Pages.WitPages;
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

namespace AzureDevOpsMigrator.WPF.Pages
{
    /// <summary>
    /// Interaction logic for WorkItemsPage.xaml
    /// </summary>
    public partial class WorkItemsPage : Page
    {
        public static WorkItemTabs PreviousTab { get; set; } = WorkItemTabs.GeneralTab;
        private WorkItemTabs _activeTab;
        public WorkItemTabs ActiveTab
        {
            get => _activeTab;
            set
            {
                _activeTab = value;
            }
        }

        public WitGeneralPage GeneralPage = new WitGeneralPage() { Tag = WorkItemTabs.GeneralTab };
        public WitQueryPage QueryPage = new WitQueryPage() { Tag = WorkItemTabs.QueryTab };
        public WitAreaPathsPage AreaPathsPage = new WitAreaPathsPage() { Tag = WorkItemTabs.AreaPathsTab };
        public WitIterationsPage IterationsPage = new WitIterationsPage() { Tag = WorkItemTabs.IterationsTab };
        public WitTransformationsPage TransformationsPage = new WitTransformationsPage() { Tag = WorkItemTabs.TransformationsTab };
        public Page[] Pages;
        public WorkItemsPage()
        {
            InitializeComponent();
            ActiveTab = PreviousTab;
            Pages = new Page[]
            {
                GeneralPage,
                QueryPage,
                AreaPathsPage,
                IterationsPage,
                TransformationsPage
            };
            Loaded += WorkItemsPage_Loaded;
        }

        private void WorkItemsPage_Loaded(object sender, RoutedEventArgs e)
        {
            _setTabOpacity();
        }

        private void _setTabOpacity()
        {
            foreach (Page page in Pages)
            {
                if (page.Tag.ToString() == Enum.GetName(typeof(WorkItemTabs), _activeTab))
                    WitPage.Navigate(page);
            }
            foreach (TextBlock tab in Tabs.Children)
            {
                if (tab.Name == Enum.GetName(typeof(WorkItemTabs), _activeTab))
                {
                    tab.SetValue(TextBlock.ForegroundProperty, Brushes.CadetBlue);
                }
                else
                {
                    tab.SetValue(TextBlock.ForegroundProperty, Brushes.Gray);
                }
            }
        }

        private void Tab_Selected(object sender, MouseButtonEventArgs e)
        {
            ActiveTab = Enum.Parse<WorkItemTabs>((sender as TextBlock).Name);
            PreviousTab = ActiveTab;
            _setTabOpacity();
        }

        private void Button_Back_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.NavigateTo<EndpointPage>(EndpointConfigMode.Source);
        }

        private void Button_Next_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.NavigateTo<EndpointPage>(EndpointConfigMode.Target);
        }
    }
    public enum WorkItemTabs
    {
        GeneralTab,
        QueryTab,
        AreaPathsTab,
        IterationsTab,
        TransformationsTab
    }
}
