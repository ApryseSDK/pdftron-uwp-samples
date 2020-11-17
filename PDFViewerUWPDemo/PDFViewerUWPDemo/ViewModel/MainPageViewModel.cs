using System;
using System.Windows.Input;
using pdftron.PDF;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.System;
using Windows.UI.Popups;
using pdftron.PDF.Tools;
using pdftron.PDF.Annots;
using Windows.ApplicationModel.Core;
using Windows.UI.Xaml.Media;

namespace PDFViewerUWP_PDFTron.ViewModel
{
    class MainPageViewModel : BaseViewModel
    {
        #region Private Properties

        ToolManager _toolManagerPDF;

        #endregion

        public MainPageViewModel()
        {
            // Initialize commands
            CMDOpenFile = new RelayCommand(OpenFile);
            CMDExitApplication = new RelayCommand(ExitApplication);
            CMDTextAnnotation = new RelayCommand(TextAnnotationSample);
            CMDPreviousPage = new RelayCommand(PreviousPage);
            CMDNextPage = new RelayCommand(NextPage);

            // Set control background color to gray
            PDFViewCtrl.SetBackgroundColor(Windows.UI.Color.FromArgb(100, 49, 51, 53));

            // ToolManager is initialized with the PDFViewCtrl and it activates all available tools
            _toolManagerPDF = new ToolManager(PDFViewCtrl);
        }

        #region Public Properties

        PDFViewCtrl _pDFViewCtrl = new PDFViewCtrl();
        public PDFViewCtrl PDFViewCtrl
        { 
            get { return _pDFViewCtrl; } 
            set { _pDFViewCtrl = value; NotifyPropertyChanged(); } 
        }

        #endregion

        #region Public Commands
        public ICommand CMDOpenFile { get; set; }  
        
        public ICommand CMDExitApplication { get; set; }

        public ICommand CMDTextAnnotation { get; set; }

        public ICommand CMDNextPage { get; set; }

        public ICommand CMDPreviousPage { get; set; }
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

        #endregion
    }
}
