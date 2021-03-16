using System;
using System.Windows.Input;
using System.Threading.Tasks;
using Windows.Devices.Input;
using Windows.UI.Xaml;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Popups;
using Windows.ApplicationModel.Core;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Resources;
using Windows.Graphics.Printing;

using pdftron.PDF;
using pdftron.PDF.Tools;
using pdftron.PDF.Tools.Controls;
using System.Collections.Generic;
using pdftron.Filters;
using Windows.ApplicationModel.DataTransfer;

namespace PDFViewerUWP_PDFTron.ViewModel
{
    class MainPageViewModel : BaseViewModel
    {
        #region Private Properties
        ToolManager _toolManagerPDF;
        PDFPrintManager _printManager;
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
            CMDConvertFile = new RelayCommand(ConvertFile);
            CMDPrintDoc = new RelayCommand(PrintDoc);
            CMDExitApplication = new RelayCommand(ExitApplication);
            CMDTextAnnotation = new RelayCommand(TextAnnotationSample);
            CMDPreviousPage = new RelayCommand(PreviousPage);
            CMDNextPage = new RelayCommand(NextPage);
            CMDZoomIn = new RelayCommand(ZoomIn);
            CMDZoomOut = new RelayCommand(ZoomOut);
            CMDSaveDocAs = new RelayCommand(SaveDocAs);

            // Set control background color to gray
            PDFViewCtrl.SetBackgroundColor(Windows.UI.Color.FromArgb(100, 49, 51, 53));

            // Initialize PDFView with a PDF document to be displayed
            PDFDoc doc = new PDFDoc("Resources/GettingStarted.pdf");
            PDFViewCtrl.SetDoc(doc);

            PDFViewCtrl.AllowDrop = true;
            PDFViewCtrl.Drop += PDFViewCtrl_Drop;
            PDFViewCtrl.DragOver += PDFViewCtrl_DragOver;

            // ToolManager is initialized with the PDFViewCtrl and it activates all available tools
            _toolManagerPDF = new ToolManager(PDFViewCtrl);

            // Set Undo and Redo Manager
            UndoRedoManager undoRedoManager = new UndoRedoManager();
            _toolManagerPDF.SetUndoRedoManager(undoRedoManager);

            // AnnotationCommandBar is initialized with the ToolManager so it can attach all events to it
            _AnnotationCommandBar = new AnnotationCommandBar(_toolManagerPDF);

            // ThumbnailViewer is initialized by passing the PDFViewerCtrl and a document tag/name
            ThumbnailViewer = new ThumbnailViewer(PDFViewCtrl, "GettingStarted");

            // Set up resource path for conversion
            pdftron.PDFNet.AddResourceSearchPath(System.IO.Path.Combine(Package.Current.InstalledLocation.Path, "Resources"));

            // Set up Print Manager
            InitPrintManager();

        }

        #region Init
        private void InitPrintManager()
        {
            _printManager = PDFPrintManager.GetInstance();

            // Load resources from "/Strings/en-US/Printing.resw"
            ResourceLoader loader = ResourceLoader.GetForCurrentView("/Printing");
            _printManager.SetResourceLoader(loader);

            // standard options
            _printManager.AddStandardPrintOption(StandardPrintTaskOptions.MediaSize);
            _printManager.AddStandardPrintOption(StandardPrintTaskOptions.Orientation);
            _printManager.AddStandardPrintOption(StandardPrintTaskOptions.Copies);

            // PDFTron options
            _printManager.AddUserOptionAnnotations();
            _printManager.AddUserOptionAutoRotate();
            _printManager.AddUserOptionPageRange();
        }

        #endregion

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

        ThumbnailViewer _ThumbnailViewer;
        public ThumbnailViewer ThumbnailViewer
        {
            get { return _ThumbnailViewer; }
            set
            {
                if (_ThumbnailViewer != null && _ThumbnailViewer != value)
                    _ThumbnailViewer.ViewModel.CleanUp();
                
                _ThumbnailViewer = value;

                ThumbnailViewer.Width = 250;
                ThumbnailViewer.ListViewItemWidth = 250;
                ThumbnailViewer.NumberOfColumns = 2;
                ThumbnailViewer.FitItemsToWidth = true;
                ThumbnailViewer.NavigationOnly = true;                

                NotifyPropertyChanged();
            }
        }

        bool _ShowThumbnails = false;

        public bool ShowThumbnails
        {
            get { return _ShowThumbnails; }
            set
            {
                _ShowThumbnails = value;

                NotifyPropertyChanged();
                NotifyPropertyChanged(nameof(IsThumbnailOpen));
            }
        }

        public Visibility IsThumbnailOpen
        {
            get { return (ShowThumbnails == true ? Visibility.Visible : Visibility.Collapsed); }
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

        public ICommand CMDConvertFile { get; set; }

        public ICommand CMDPrintDoc { get; set; }

        public ICommand CMDExitApplication { get; set; }

        public ICommand CMDTextAnnotation { get; set; }

        public ICommand CMDNextPage { get; set; }

        public ICommand CMDPreviousPage { get; set; }

        public ICommand CMDZoomIn { get; set; }

        public ICommand CMDZoomOut { get; set; }

        public ICommand CMDSaveDocAs { get; set; }
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

            await OpenFilePDFViewer(file, FileAccessMode.ReadWrite);
        }

        private async Task OpenFilePDFViewer(IStorageFile file, FileAccessMode mode)
        {
            if (file == null)
                return;

            Windows.Storage.Streams.IRandomAccessStream stream;
            try
            {
                stream = await file.OpenAsync(mode);
            }
            catch (Exception e)
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

            ThumbnailViewer = new ThumbnailViewer(PDFViewCtrl, file.Path);
        }

        /// <summary>
        /// Convert a document to PDF format
        /// </summary>
        async private void ConvertFile()
        {
            FileOpenPicker filePicker = new FileOpenPicker();
            filePicker.ViewMode = PickerViewMode.List;
            filePicker.FileTypeFilter.Add(".doc");
            filePicker.FileTypeFilter.Add(".docx");

            StorageFile file = await filePicker.PickSingleFileAsync();

            if (file == null)
                return;

            Windows.Storage.Streams.IRandomAccessStream stream;
            try
            {
                stream = await file.OpenAsync(FileAccessMode.ReadWrite);
            }
            catch (Exception e)
            {
                // NOTE: If file already opened it will cause an exception
                var msg = new MessageDialog(e.Message);
                _ = msg.ShowAsync();
                return;
            }

            // Convert Logic
            IFilter filter = new RandomAccessStreamFilter(stream);
            WordToPDFOptions opts = new WordToPDFOptions();
            DocumentConversion conversion = pdftron.PDF.Convert.UniversalConversion(filter, opts);

            var convRslt = conversion.TryConvert();

            if (convRslt == DocumentConversionResult.e_document_conversion_success)
            {
                PDFDoc doc = conversion.GetDoc();
                doc.InitSecurityHandler();

                PDFViewCtrl.SetDoc(doc);

                ThumbnailViewer = new ThumbnailViewer(PDFViewCtrl, file.Path);
            }
        }
              
        /// <summary>
        /// Bring Print Manager Dialog for printing the document
        /// </summary>
        private async void PrintDoc()
        {
            if (PDFViewCtrl.HasDocument == false)
                return;

            var doc = PDFViewCtrl.GetDoc();
            var fileName = doc.GetFileName();

            // Register current loaded document for printing
            _printManager.RegisterForPrintingContract(doc, fileName);

            try
            {
                // bring the Windows Print Dialog
                await PrintManager.ShowPrintUIAsync();
            }
            catch
            {
                //Swallow exception
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

        private void SaveDocAs()
        {
            if (!PDFViewCtrl.HasDocument)
                return;

            var doc = PDFViewCtrl.GetDoc();

            _ = PerformSave(doc);
        }

        private async Task PerformSave(PDFDoc doc)
        {
            // Start File Picker so we can SaveAs/Export document
            var savePicker = new Windows.Storage.Pickers.FileSavePicker();
            savePicker.FileTypeChoices.Add("PDF", new List<string> { ".pdf" });

            var storageFile = await savePicker.PickSaveFileAsync();

            if (storageFile != null)
                await doc.SaveToNewLocationAsync(storageFile, pdftron.SDF.SDFDocSaveOptions.e_incremental);
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

        /// <summary>
        /// 
        /// </summary>
        private void PDFViewCtrl_DragOver(object sender, DragEventArgs e)
        {
            e.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Copy;
            e.DragUIOverride.Caption = "Open PDF file";
        }

        /// <summary>
        /// Handle drang and drop PDF file onto PDFViewCtrl
        /// </summary>>
        private async void PDFViewCtrl_Drop(object sender, DragEventArgs e)
        {
            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                IReadOnlyList<IStorageItem> items = await e.DataView.GetStorageItemsAsync();
                if (items.Count == 1)
                {
                    StorageFile storageFile = items[0] as StorageFile;

                    if (storageFile.FileType.Equals(".pdf", StringComparison.OrdinalIgnoreCase))
                    {
                        // Note: Drag and drop on UWP only allows Read-Only access
                        await OpenFilePDFViewer(storageFile, FileAccessMode.Read);
                    }
                }
            }
        }
        #endregion
    }
}
