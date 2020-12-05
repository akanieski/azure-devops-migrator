using System.Reflection;
using System.Windows.Controls;

namespace AzureDevOpsMigrator.WPF.Pages
{
    /// <summary>
    /// Interaction logic for About.xaml
    /// </summary>
    public partial class AboutPage : Page
    {
        public string Version { get; set; }
        public AboutPage()
        {
            InitializeComponent();
            DataContext = this;
            Version = GetAssemblyVersion();
        }
        public string GetAssemblyVersion()
        {
            return Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyFileVersionAttribute>().Version;
        }
    }
}
