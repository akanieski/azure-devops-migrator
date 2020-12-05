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

namespace AzureDevOpsMigrator.WPF.Common
{
    /// <summary>
    /// Interaction logic for NavButton.xaml
    /// </summary>
    public partial class NavButton : UserControl
    {
        public event EventHandler Clicked;
        public static DependencyProperty LabelProperty =
            DependencyProperty.Register(nameof(Label), typeof(string), typeof(NavButton), new PropertyMetadata(null));

        public static DependencyProperty IsActiveProperty =
            DependencyProperty.Register(nameof(IsActive), typeof(bool), typeof(NavButton), new PropertyMetadata(false));

        public static DependencyProperty PageNameProperty =
            DependencyProperty.Register(nameof(PageName), typeof(string), typeof(NavButton), new PropertyMetadata(null));

        public static DependencyProperty ActiveStyleProperty =
            DependencyProperty.Register(nameof(ActiveStyle), typeof(Style), typeof(NavButton), new PropertyMetadata(null));

        private void IsActiveChanged(object sender, object eventArgs) { }

        public Style ActiveStyle
        {
            get
            {
                return GetValue(ActiveStyleProperty) as Style;
            }
            set
            {
                SetValue(ActiveStyleProperty, value);
            }
        }

        public string Label
        {
            get
            {
                return (string)GetValue(LabelProperty);
            }
            set
            {
                SetValue(LabelProperty, value);
            }
        }

        public string PageName
        {
            get
            {
                return (string)GetValue(PageNameProperty);
            }
            set
            {
                SetValue(PageNameProperty, value);
            }
        }

        public bool IsActive
        {
            get 
            { 
                return (bool)GetValue(IsActiveProperty); 
            }
            set 
            { 
                SetValue(IsActiveProperty, value);
            }
        }

        public NavButton()
        {
            InitializeComponent();

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (Clicked != null)
            {
                Clicked(this, EventArgs.Empty);
            }
        }
    }
}
