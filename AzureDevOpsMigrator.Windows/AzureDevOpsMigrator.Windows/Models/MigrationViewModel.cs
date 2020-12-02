using AzureDevOpsMigrator.Models;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.UI.Xaml;
using Microsoft.VisualStudio.Services.Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureDevOpsMigrator.Windows.Models
{
    public class BaseViewModel : INotifyPropertyChanged
    {

        public event PropertyChangedEventHandler PropertyChanged;
        public T SignalChange<T>(T value, params string[] propertyNames)
        {
            propertyNames.ForEach(prop => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop)));
            return value;
        }
    }
    public class MigrationViewModel : BaseViewModel
    {
        public Visibility DependsOnCurrentConfigBeingSet => _currentConfig != null ? Visibility.Visible : Visibility.Collapsed;
        private MigrationConfig _currentConfig = null;
        public MigrationConfig CurrentConfig
        {
            get => _currentConfig;
            set => SignalChange(_currentConfig = value, 
                "CurrentConfig", 
                "DependsOnCurrentConfigBeingSet");
        }
        private ObservableCollection<WorkItem> _previewedWorkItems;
        public ObservableCollection<WorkItem> PreviewedWorkItems
        {
            get => _previewedWorkItems;
            set => SignalChange(_previewedWorkItems = value, "PreviewedWorkItems");
        }
    }
}
