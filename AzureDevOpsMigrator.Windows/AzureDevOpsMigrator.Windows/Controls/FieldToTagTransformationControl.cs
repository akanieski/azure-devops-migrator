using AzureDevOpsMigrator.Migrators;
using AzureDevOpsMigrator.Migrators.Transformation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace AzureDevOpsMigrator.Windows.Controls
{
    public sealed class FieldToTagTransformationControl : Control
    {
        public static readonly DependencyProperty ConfigurationProperty =
            DependencyProperty.Register(nameof(Configuration), typeof(FieldToTagTransformation),
                typeof(FieldToTagTransformationControl), new PropertyMetadata(DependencyProperty.UnsetValue));
        public FieldToTagTransformation Configuration
        {
            get { return GetValue(ConfigurationProperty) as FieldToTagTransformation; }
            set
            {
                if (Configuration != value)
                {
                    SetValue(ConfigurationProperty, value);
                    RaisePropertyChanged("Configuration");

                }
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        public void RaisePropertyChanged(string name)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(name));
            }
        }
        private static void OnConfigurationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("Configuration changed.");
        }

        public delegate void DeleteRequestedEventHandler(object Source, int Index);
        public event DeleteRequestedEventHandler DeleteRequested;
        public delegate void CollapsedEventHandler(object Source, int Index);
        public event CollapsedEventHandler Collapsed;

        public void Button_Delete_Click(object sender, RoutedEventArgs e)
        {
            DeleteRequested?.Invoke(sender, (int)Tag);
        }

        public void Button_Collapse_Click(object sender, RoutedEventArgs e)
        {
            Collapsed?.Invoke(sender, (int)Tag);
        }
        

        public FieldToTagTransformationControl()
        {
            this.DefaultStyleKey = typeof(FieldToTagTransformationControl);
            this.DataContext = this;
        }
    }
}
