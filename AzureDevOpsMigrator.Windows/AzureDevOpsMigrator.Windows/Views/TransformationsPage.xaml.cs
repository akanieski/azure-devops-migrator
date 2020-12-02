using AzureDevOpsMigrator.Models;
using AzureDevOpsMigrator.Migrators;
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
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using AzureDevOpsMigrator.Migrators.Transformation;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace AzureDevOpsMigrator.Windows.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class TransformationsPage : Page
    {
        public string CurrentTransformationType => $"{(ListView_Transformations.SelectedValue as ITransformation)?.Display} Transformation";
        public FieldToFieldTransformation CurrentFieldToField => ListView_Transformations.SelectedValue as FieldToFieldTransformation;
        public FieldToTagTransformation CurrentFieldToTag => ListView_Transformations.SelectedValue as FieldToTagTransformation;
        public MigrationConfig Model => MainWindow.CurrentModel.CurrentConfig;
        public TransformationsPage()
        {
            InitializeComponent();
        }

        private void Button_AddFieldToTag_Click(object sender, RoutedEventArgs e)
        {
            Model.Transformations.Add(new FieldToTagTransformation());
            MainWindow.SaveModel();
            Bindings.Update();
        }

        private void Button_AddFieldToField_Click(object sender, RoutedEventArgs e)
        {
            Model.Transformations.Add(new FieldToFieldTransformation());
            MainWindow.SaveModel();
            Bindings.Update();
        }

        private void ListView_Transformations_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Bindings.Update();
        }

        private void Pivot_Transformation_Shown(object sender, object e)
        {
            if (ListView_Transformations.Items.Count > 0)
            {
                ListView_Transformations.SelectedIndex = 0;
            }
        }

        private void Button_DeleteTransformation_Click(object sender, RoutedEventArgs e)
        {
            var index = ListView_Transformations.SelectedIndex;
            var _ = Dispatcher.RunAsync(CoreDispatcherPriority.High, async () =>
            {
                if (await MainWindow.Confirm())
                {
                    Model.Transformations.RemoveAt(index);
                    MainWindow.SaveModel();
                }
            });;
        }

        private void Pivot_PivotItemUnloading(Pivot sender, PivotItemEventArgs args)
        {
            MainWindow.SaveModel();
        }

        private void Button_Next_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.NavigateTo<EndpointPage>(EndpointPageMode.Target);
        }
    }
    public class TransformationTemplateSelector : DataTemplateSelector
    {
        public DataTemplate FieldToField { get; set; }
        //public DataTemplate FieldToTag { get; set; }

        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
        {
            if (item is FieldToFieldTransformation)
            {
                return FieldToField;
            }
            else
            {
                //return FieldToTag;
            }
            return null;
        }
    }
}
