using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace AzureDevOpsMigrator.WPF.Pages
{
    /// <summary>
    /// Interaction logic for GettingStartedPage.xaml
    /// </summary>
    public partial class GettingStartedPage : Page, INotifyPropertyChanged
    {
        public Visibility RecentMigrationsVisible => RecentMigrations.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        public ObservableCollection<KeyValuePair<string, string>> RecentMigrations { get; set; } = new ObservableCollection<KeyValuePair<string, string>>();
        public GettingStartedPage()
        {
            InitializeComponent();
            DataContext = this;
            Properties.Settings.Default.Reload();
            Properties.Settings.Default.RecentMigrations = Properties.Settings.Default.RecentMigrations ?? new StringCollection();
            Load();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void Load()
        {
            RecentMigrations.Clear();
            foreach (var s in Properties.Settings.Default.RecentMigrations)
            {
                RecentMigrations.Add(new KeyValuePair<string, string>(s, s.Split('\\').LastOrDefault()));
            }
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(RecentMigrations)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(RecentMigrationsVisible)));
        }

        private void BtnGettingStarted_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.CurrentModel.CurrentConfig = new Models.MigrationConfig();
            MainWindow.NavigateTo<GeneralPage>();
        }

        private void BtnLoadExisting_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true) MainWindow.LoadModel(openFileDialog.FileName);
        }

        private void Button_Remove_Recent(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Properties.Settings.Default.RecentMigrations.Remove(((KeyValuePair<string, string>)(sender as TextBlock).Tag).Key);
            Load();
        }

        private void Button_Open_Migration(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            MainWindow.LoadModel(((KeyValuePair<string, string>)(sender as TextBlock).Tag).Key);
        }
    }
}
