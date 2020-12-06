using System.Diagnostics;
using System.Reflection;
using System.Windows;
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

        private void GoUrl(object sender, RoutedEventArgs e)
        {
            Process.Start(new System.Diagnostics.ProcessStartInfo("cmd", "/C start " + (sender as Control).Tag.ToString())
            {
                WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden,
                CreateNoWindow = true,
                UseShellExecute = true
            });
        }
    }
}
