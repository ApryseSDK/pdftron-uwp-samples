using System;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Media.Imaging;

using pdftron.PDF;
using ZXing;
using Windows.Devices.Input;
using Windows.UI.Xaml;
using pdftron.Common;
using Windows.UI.Popups;
using Windows.Storage;
using Windows.Storage.Pickers;
using pdftron.PDF.Tools;

namespace PDFViewerBarcode.ViewModel
{
    class MainPageViewModel : BaseViewModel
    {
        #region Private properties
        BarcodeViewModel barcodeViewModel;
        BarcodeType barcodeSelected = BarcodeType.None;
        string barcodeData = string.Empty;
        ToolManager toolManager;
        #endregion

        public MainPageViewModel()
        {
            // Register commands
            CMDOpenFile = new RelayCommand(OpenFile);
            CMDAddBarCode = new RelayCommand(AddBarcode);

            // Initialize PDF View Control
            PDFViewCtrl = new PDFViewCtrl();
            PDFViewCtrl.PointerPressed += PDFViewCtrl_PointerPressed;

            // Open getting started PDF file
            PDFDoc doc = new PDFDoc("Resources/GettingStarted.pdf");
            doc.InitSecurityHandler();

            // Load document into PDF View Control
            PDFViewCtrl.SetDoc(doc);

            // Init ToolManager
            toolManager = new ToolManager(PDFViewCtrl); 

            // Init Dialog ViewModel
            barcodeViewModel = new BarcodeViewModel();
            BarcodeViewModel = new BarcodeDialogViewModel(new BarcodeDialogService(barcodeViewModel));            
        }

        #region Public Properties
        PDFViewCtrl _pdfViewCtrl;
        public PDFViewCtrl PDFViewCtrl
        {
            get 
            {
                return _pdfViewCtrl;
            }
            set 
            {
                _pdfViewCtrl = value;
                NotifyPropertyChanged();
            }
        }

        BarcodeDialogViewModel _barcodeDialogViewModel;
        public BarcodeDialogViewModel BarcodeViewModel
        {
            get { return _barcodeDialogViewModel; }
            set
            {
                _barcodeDialogViewModel = value;
                NotifyPropertyChanged();
            }
        }

        bool _IsAddBarcodeSelected = false;
        public bool IsAddBarcodeSelected
        {
            get { return _IsAddBarcodeSelected; }
            set
            {
                _IsAddBarcodeSelected = value;
                NotifyPropertyChanged();
            }
        }

        #endregion

        #region Public Commands
        public ICommand CMDOpenFile { get; set; }
        public ICommand CMDAddBarCode { get; set; }
        public ICommand CMDScanBarcode { get; set; }
        #endregion

        #region Operations
        async private void OpenFile()
        {
            FileOpenPicker filePicker = new FileOpenPicker();
            filePicker.ViewMode = PickerViewMode.List;
            filePicker.FileTypeFilter.Add(".pdf");

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

            PDFDoc doc = new PDFDoc(stream);
            doc.InitSecurityHandler();

            // Set loaded doc to PDFView Controler 
            PDFViewCtrl.SetDoc(doc);
        }

        async private void AddBarcode()
        {
            await BarcodeViewModel.ShowDialog();

            if ((barcodeViewModel.BarcodeSelected == null) || (barcodeViewModel.BarcodeSelected.Type == BarcodeType.None))
                return;

            barcodeSelected = barcodeViewModel.BarcodeSelected.Type;
            barcodeData = barcodeViewModel.InputData;
        }

        #endregion

        #region Utilities
        /// <summary>
        /// Convert WriteableBitmap to JPEG byte array
        /// </summary>
        /// <param name="bmp">The writeable image to convert</param>
        /// <returns>Converted JPEG byte array</returns>
        private async Task<byte[]> EncodeJpeg(WriteableBitmap bmp)
        {
            SoftwareBitmap soft = SoftwareBitmap.CreateCopyFromBuffer(bmp.PixelBuffer, BitmapPixelFormat.Bgra8, bmp.PixelWidth, bmp.PixelHeight);
            byte[] array = null;

            using (var ms = new InMemoryRandomAccessStream())
            {
                BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, ms);
                encoder.SetSoftwareBitmap(soft);

                try
                {
                    await encoder.FlushAsync();
                }
                catch { }

                array = new byte[ms.Size];
                await ms.ReadAsync(array.AsBuffer(), (uint)ms.Size, InputStreamOptions.None);
            }

            return array;
        }

        /// <summary>
        /// Stamp a JPEG image to a PDF document in a specific position
        /// </summary>
        /// <param name="doc">PDF Document to add the barcode</param>
        /// <param name="jpegBytes">The encoded JPEG byte array</param>
        /// <param name="h">Horizontal position</param>
        /// <param name="v">Vertical position</param>
        private void StampToPage(PDFDoc doc, byte[] jpegBytes, double h, double v)
        {
            // Stamping PDF file
            pdftron.PDF.Image barcodeImage = pdftron.PDF.Image.Create(doc.GetSDFDoc(), jpegBytes);

            Stamper barcodeStamp = new Stamper(StamperSizeType.e_absolute_size, 150, 150);

            //set position of the image to the center, left of PDF pages
            barcodeStamp.SetAlignment(StamperHorizontalAlignment.e_horizontal_left, StamperVerticalAlignment.e_vertical_bottom);
            barcodeStamp.SetAsAnnotation(true);

            double xPos = h - (150 / 2);
            double yPos = v - (150 / 2);

            barcodeStamp.SetPosition(xPos, yPos);
            barcodeStamp.SetFontColor(new ColorPt(0, 0, 0, 0));
            //barcodeStamp.SetRotation(180);
            barcodeStamp.SetAsBackground(false);
            // Stamp current viewing page
            barcodeStamp.StampImage(doc, barcodeImage, new PageSet(PDFViewCtrl.GetCurrentPage(), PDFViewCtrl.GetCurrentPage()));

            // Render current visible region
            PDFViewCtrl.Update();
        }
        #endregion

        #region Events
        /// <summary>
        /// Listen to any click events on the PDFViewCtrl
        /// </summary>
        private async void PDFViewCtrl_PointerPressed(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            if (e.Pointer.PointerDeviceType == PointerDeviceType.Mouse)
            {
                if (barcodeSelected == BarcodeType.None)
                    return;

                // Get click position
                Windows.Foundation.Point pt = e.GetCurrentPoint(PDFViewCtrl).Position;

                int pageNumber = PDFViewCtrl.GetPageNumberFromScreenPoint(pt.X, pt.Y);

                // Check if clicked outside the page bounds
                if (pageNumber == -1)
                    return;

                var doc = PDFViewCtrl.GetDoc();
                Page page = doc.GetPage(pageNumber);

                DoubleRef ref_x = new DoubleRef(pt.X);
                DoubleRef ref_y = new DoubleRef(pt.Y);
                PDFViewCtrl.ConvScreenPtToPagePt(ref_x, ref_y, pageNumber);

                int pgX = (int)ref_x.Value;
                int pgY = (int)ref_y.Value;

                pdftron.Common.Matrix2D mtx = page.GetDefaultMatrix();
                mtx.Mult(ref_x, ref_y);

                switch(barcodeSelected)
                {
                    case BarcodeType.UPCA:
                        await PerformAddBarcode(doc, BarcodeFormat.UPC_A, barcodeData, ref_x.Value, ref_y.Value);                        
                        break;

                    case BarcodeType.QRCode:
                        await PerformAddBarcode(doc, BarcodeFormat.QR_CODE, barcodeData, ref_x.Value, ref_y.Value);
                        break;

                    case BarcodeType.DataMatrix:
                        await PerformAddBarcode(doc, BarcodeFormat.DATA_MATRIX, barcodeData, ref_x.Value, ref_y.Value);
                        break;
                }

                barcodeSelected = BarcodeType.None;
                IsAddBarcodeSelected = false;
            }
        }

        private async Task PerformAddBarcode(PDFDoc doc, BarcodeFormat type, string data, double h, double v)
        {
            // Create barcode image
            var write = new BarcodeWriter();
            write.Format = type;
            var wb = write.Write(data);
            var jpegBytes = await EncodeJpeg(wb);

            // Stamp
            StampToPage(doc, jpegBytes, h, v);
        }
        #endregion
    }
}
