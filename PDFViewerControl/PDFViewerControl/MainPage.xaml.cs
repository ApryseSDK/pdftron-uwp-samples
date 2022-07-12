using PDFViewerControl.ViewModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;

using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;


namespace PDFViewerControl
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPageViewModel ViewModel { get; set; }

        public MainPage()
        {
            this.InitializeComponent();
            this.DataContext = ViewModel = new MainPageViewModel();

            this.Loaded += MainPage_Loaded;
        }

        private async void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            await ViewModel.InitAsync();
        }

        protected override async void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            if (ViewModel != null)
            {
                ViewModel.ViewerControl.Dispose();

                // To ensure all temporary files created and managed by the ViewerControl are deleted, just call Full cleanup
                await pdftron.PDF.Tools.Utilities.DocumentManager.Instance.PerformFullCleanupAsync();
            }

            base.OnNavigatingFrom(e);
        }
    }
}
