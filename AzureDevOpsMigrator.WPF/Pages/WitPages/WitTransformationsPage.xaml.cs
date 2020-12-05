using AzureDevOpsMigrator.Migrators.Transformation;
using AzureDevOpsMigrator.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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

namespace AzureDevOpsMigrator.WPF.Pages.WitPages
{
    /// <summary>
    /// Interaction logic for WitTransformationsPage.xaml
    /// </summary>
    public partial class WitTransformationsPage : Page, INotifyPropertyChanged
    {
        public string CurrentTransformationType => $"{(ListView_Transformations.SelectedValue as ITransformation)?.Display} Transformation";
        public string CurrentTransformationIndex => ListView_Transformations.SelectedIndex > -1 ? ListView_Transformations.SelectedIndex.ToString() : "";
        public Visibility CurrentTransformVisibility => ListView_Transformations.SelectedValue != null ? Visibility.Visible : Visibility.Collapsed;
        public FieldToFieldTransformation CurrentFieldToField => ListView_Transformations.SelectedValue as FieldToFieldTransformation;
        public Visibility FieldToFieldVisible => ListView_Transformations.SelectedValue is FieldToFieldTransformation ? Visibility.Visible : Visibility.Collapsed;
        public FieldToTagTransformation CurrentFieldToTag => ListView_Transformations.SelectedValue as FieldToTagTransformation;
        public Visibility FieldToTagVisible => ListView_Transformations.SelectedValue is FieldToTagTransformation ? Visibility.Visible : Visibility.Collapsed;
        public MigrationConfig Model => MainWindow.CurrentModel.CurrentConfig;
        public WitTransformationsPage()
        {
            InitializeComponent();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void Menu_RemoveTransformation(object sender, RoutedEventArgs e)
        {
            var index = ListView_Transformations.SelectedIndex;
            Model.Transformations.RemoveAt(index);
            MainWindow.SaveModel();
        }

        private void Menu_AddFieldToField(object sender, RoutedEventArgs e)
        {

            Model.Transformations.Add(new FieldToFieldTransformation());
            MainWindow.SaveModel();
        }

        private void Menu_AddFieldToTag(object sender, RoutedEventArgs e)
        {

            Model.Transformations.Add(new FieldToTagTransformation());
            MainWindow.SaveModel();
        }

        private void ListView_Transformations_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentTransformationIndex)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentTransformationType)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentFieldToField)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentFieldToTag)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FieldToTagVisible)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FieldToFieldVisible)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentTransformVisibility))); 
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (ListView_Transformations.Items.Count > 0)
            {
                ListView_Transformations.SelectedIndex = 0;
            }
        }
    }
}
