using AzureDevOpsMigrator.Windows.Models;
using AzureDevOpsMigrator.Windows.Views;
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
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT;
using static AzureDevOpsMigrator.Windows.Views.GeneralPage;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace AzureDevOpsMigrator.Windows
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class GettingStartedPage : Page
    {
        public MigrationViewModel Model => MainWindow.CurrentModel;
        public GettingStartedPage()
        {
            this.InitializeComponent();
        }

        private void BtnGettingStarted_Click(object sender, RoutedEventArgs e)
        {
            var config = new AzureDevOpsMigrator.Models.MigrationConfig();

            Model.CurrentConfig = config;
            MainWindow.NavigateTo<GeneralPage>();
        }

        private void BtnLoadExisting_Click(object sender, RoutedEventArgs e)
        {
            var _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                var filePicker = new FileOpenPicker();
                filePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
                filePicker.FileTypeFilter.Add(".miproj");


                // When running on win32, FileOpenPicker needs to know the top-level hwnd via IInitializeWithWindow::Initialize.
                if (Window.Current == null)
                {
                    IInitializeWithWindow initializeWithWindowWrapper = filePicker.As<IInitializeWithWindow>();
                    IntPtr hwnd = GetActiveWindow();
                    initializeWithWindowWrapper.Initialize(hwnd);
                }

                StorageFile file = await filePicker.PickSingleFileAsync();
                if (file != null)
                {
                    MainWindow.LoadModel(file.Path);
                }
            });
        }
    }
}
