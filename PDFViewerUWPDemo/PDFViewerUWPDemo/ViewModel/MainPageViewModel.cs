using System;
using System.Windows.Input;
using System.Threading.Tasks;
using Windows.Devices.Input;
using Windows.UI.Xaml;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Popups;
using Windows.ApplicationModel.Core;

using pdftron.PDF;
using pdftron.PDF.Tools;
using pdftron.PDF.Tools.Controls;

namespace PDFViewerUWP_PDFTron.ViewModel
{
    class MainPageViewModel : BaseViewModel
    {
        #region Private Properties
        ToolManager _toolManagerPDF;
        #endregion

        #region Defaults
        double DEFAULT_ZOOM_START_POSITION = 0.82;  // Initial document zoom
        double DEFAULT_ZOOM_FACTOR_STEP = 1.25;     // Amount of zoom when Zooming in or out by button click
        double DEFAULT_ZOOM_REGION_STEP = 2.5;      // Amount of zoom when zooming to a specific region by right mouse click
        #endregion

        public MainPageViewModel()
        {
            // Initialize commands
            CMDOpenFile = new RelayCommand(OpenFile);
            CMDExitApplication = new RelayCommand(ExitApplication);
            CMDTextAnnotation = new RelayCommand(TextAnnotationSample);
            CMDPreviousPage = new RelayCommand(PreviousPage);
            CMDNextPage = new RelayCommand(NextPage);
            CMDZoomIn = new RelayCommand(ZoomIn);
            CMDZoomOut = new RelayCommand(ZoomOut);

            // Set control background color to gray
            PDFViewCtrl.SetBackgroundColor(Windows.UI.Color.FromArgb(100, 49, 51, 53));

            // Initialize PDFView with a PDF document to be displayed
            PDFDoc doc = new PDFDoc("Resources/GettingStarted.pdf");
            PDFViewCtrl.SetDoc(doc);

            // ToolManager is initialized with the PDFViewCtrl and it activates all available tools
            _toolManagerPDF = new ToolManager(PDFViewCtrl);
            // AnnotationCommandBar is initialized with the ToolManager so it can attach all events to it
            _AnnotationCommandBar = new AnnotationCommandBar(_toolManagerPDF);
        }

        #region Public Properties

        PDFViewCtrl _pDFViewCtrl = new PDFViewCtrl();
        public PDFViewCtrl PDFViewCtrl
        { 
            get { return _pDFViewCtrl; } 
            set { _pDFViewCtrl = value; NotifyPropertyChanged(); } 
        }

        AnnotationCommandBar _AnnotationCommandBar;
        public AnnotationCommandBar AnnotationCommandBar
        {
            get { return _AnnotationCommandBar; }
            set { _AnnotationCommandBar = value; NotifyPropertyChanged(); }
        }

        bool _lockZoomMouse = false;

        public bool LockZoom
        {
            get { return _lockZoomMouse; }
            set 
            {               
                _lockZoomMouse = !_lockZoomMouse;
                if (_lockZoomMouse)
                    PDFViewCtrl.PointerPressed += PDFViewCtrl_PointerReleased;
                else
                    PDFViewCtrl.PointerPressed -= PDFViewCtrl_PointerReleased;

                NotifyPropertyChanged(); 
            }
        }

        #endregion

        #region Public Commands
        public ICommand CMDOpenFile { get; set; }  
        
        public ICommand CMDExitApplication { get; set; }

        public ICommand CMDTextAnnotation { get; set; }

        public ICommand CMDNextPage { get; set; }

        public ICommand CMDPreviousPage { get; set; }

        public ICommand CMDZoomIn { get; set; }

        public ICommand CMDZoomOut { get; set; }
        #endregion

        #region Operations
        /// <summary>
        /// Open dialog box to search and load PDF file
        /// 
        /// </summary>
        async private void OpenFile()
        {
            FileOpenPicker filePicker = new FileOpenPicker();
            filePicker.ViewMode = PickerViewMode.List;
            filePicker.FileTypeFilter.Add(".pdf");

            StorageFile file = await filePicker.PickSingleFileAsync();
            if (file != null)
            {
                Windows.Storage.Streams.IRandomAccessStream stream;
                try
                {
                    stream = await file.OpenAsync(FileAccessMode.ReadWrite);
                }
                catch(Exception e) 
                {
                    // NOTE: If file already opened it will cause an exception
                    var msg = new MessageDialog(e.Message);
                    _ = msg.ShowAsync();
                    return; 
                }

                PDFDoc doc = new PDFDoc(stream);
                doc.InitSecurityHandler();

                // Set loaded doc to PDFView Controler 
                PDFViewCtrl.SetDoc(doc);
            }
        }

        private void TextAnnotationSample()
        {
            if (!PDFViewCtrl.HasDocument)
                return;           
        }

        private void NextPage()
        {
            if (!PDFViewCtrl.HasDocument)
                return; 
            
            PDFViewCtrl.GotoNextPage();
        }

        private void PreviousPage()
        {
            if (!PDFViewCtrl.HasDocument)
                return;

            PDFViewCtrl.GotoPreviousPage();
        }

        private void ExitApplication()
        {
            CoreApplication.Exit();
        }

        private void ZoomIn()
        {
            PerformZoom(true);
        }
        
        private void ZoomOut()
        {
            PerformZoom(false);
        }

        private void PerformZoom(bool zoomIn)
        {
            if (!PDFViewCtrl.HasDocument)
                return;

            var zoomFactor = DEFAULT_ZOOM_FACTOR_STEP; // Amount of zoom on each call

            double currentZoom = PDFViewCtrl.GetZoom();
            var width = (int)(PDFViewCtrl.ActualWidth / 2); // Ensure to keep the zooming centered
            var height = (int)(PDFViewCtrl.ActualHeight / 2); // Ensure to keep the zooming centered

            if (zoomIn)
                PDFViewCtrl.SetZoom(width, height, currentZoom * zoomFactor);
            else
                PDFViewCtrl.SetZoom(width, height, currentZoom / zoomFactor);           
        }

        #endregion

        #region Events

        /// <summary>
        /// When LockZoom is enabled it will allow to ZoomIn into a specific clicked region using Mouse Right Button or
        /// return to overview by Mouse Left button click
        /// </summary>
        private void PDFViewCtrl_PointerReleased(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            if (LockZoom && PDFViewCtrl.HasDocument)
            {
                if (e.Pointer.PointerDeviceType == PointerDeviceType.Mouse)
                {
                    var p = e.GetCurrentPoint((UIElement)sender);
                    if (p.Properties.IsLeftButtonPressed)
                    {
                        Task.Delay(1);
                        PDFViewCtrl.SetZoom((int)(PDFViewCtrl.ActualWidth / 2), (int)(PDFViewCtrl.ActualWidth / 2), DEFAULT_ZOOM_START_POSITION);
                    }
                    else
                    {
                        Windows.Foundation.Point pt = e.GetCurrentPoint(_pDFViewCtrl).Position;
                        var zoomFactor = DEFAULT_ZOOM_REGION_STEP; // Amount of zoom on each call
                        double currentZoom = PDFViewCtrl.GetZoom(); // Get current zoom

                        var width = (int)(pt.X);    // Get selected X coordinate
                        var height = (int)(pt.Y);   // Get selected Y coordinate

                        PDFViewCtrl.SetZoom(width, height, currentZoom * zoomFactor);
                    }
                }
            }
            e.Handled = true;
        }
        #endregion
    }
}
