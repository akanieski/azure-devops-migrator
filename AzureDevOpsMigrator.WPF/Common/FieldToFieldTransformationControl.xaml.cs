using AzureDevOpsMigrator.Migrators.Transformation;
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

namespace AzureDevOpsMigrator.WPF.Common
{
    /// <summary>
    /// Interaction logic for FieldToFieldTransformationControl.xaml
    /// </summary>
    public partial class FieldToFieldTransformationControl : UserControl
    {
        public FieldToFieldTransformationControl()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty ConfigurationProperty =
            DependencyProperty.Register(nameof(Configuration), typeof(FieldToFieldTransformation),
                typeof(FieldToFieldTransformationControl));

        public FieldToFieldTransformation Configuration
        {
            get { return GetValue(ConfigurationProperty) as FieldToFieldTransformation; }
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

    }
}
