using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using PDFViewerUWP_PDFTron.ViewModel;

using pdftron.PDF;
using System.Windows.Input;
using Windows.Storage.Pickers;
using Windows.Storage;
using Windows.UI.Popups;
using Windows.UI.Xaml.Controls;
using PDFCompare.Controls;
using Windows.UI.Xaml;

namespace PDFCompare.ViewModel
{
    class MainPageViewModel : BaseViewModel
    {
        // Reference: https://stackoverflow.com/questions/15151974/synchronized-scrolling-of-two-scrollviewers-whenever-any-one-is-scrolled-in-wpf/39425257
        private const int ScrollLoopbackTimeout = 250;
        private object _lastScrollingElement;
        private int _lastScrollChange = Environment.TickCount;

        private double mLastVerticalOffsetA;
        private double mLastVerticalOffsetB;

        ScrollViewer mScrollViewerA;
        ScrollViewer mScrollViewerB;

        public MainPageViewModel()
        {
            mPDFViewCtrlA = new PDFViewCtrl();
            mPDFViewCtrlB = new PDFViewCtrl();

            // Set control background color to gray
            mPDFViewCtrlA.SetBackgroundColor(Windows.UI.Color.FromArgb(100, 49, 51, 53));
            mPDFViewCtrlB.SetBackgroundColor(Windows.UI.Color.FromArgb(100, 49, 51, 53));

            // Setup commands
            CMDOpenPdfA = new RelayCommand(OpenFileA);
            CMDOpenPdfB = new RelayCommand(OpenFileB);
            CMDProcessTextDiff = new RelayCommand(ProcessTextDiff);
            CMDProcessVisualDiff = new RelayCommand(ProcessVisualDiff);
            CMDSyncScroll = new RelayCommand(SyncScroll);
            CMDOpenFileA = new RelayCommand(OpenFileA);
            CMDOpenFileB = new RelayCommand(OpenFileB);

            mPDFViewCtrlA.OnSetDoc += MPDFViewA_OnSetDoc;
            mPDFViewCtrlB.OnSetDoc += MPDFViewB_OnSetDoc;

            // Initialize PDFView with a PDF document to be displayed
            PDFDoc doc_a = new PDFDoc("Resources/GettingStarted_rev1.pdf");
            mPDFViewCtrlA.SetDoc(doc_a);
            PDFDoc doc_b = new PDFDoc("Resources/GettingStarted_rev2.pdf");
            mPDFViewCtrlB.SetDoc(doc_b);
        }

        #region Public Properties

        PDFViewCtrl mPDFViewCtrlA;
        public PDFViewCtrl PDFViewCtrlA
        {
            get { return mPDFViewCtrlA; }
            set { mPDFViewCtrlA = value; }
        }

        PDFViewCtrl mPDFViewCtrlB;
        public PDFViewCtrl PDFViewCtrlB
        {
            get { return mPDFViewCtrlB; }
            set { mPDFViewCtrlB = value; }
        }

        bool mSyncScrolling = true;
        public bool SyncScrolling
        {
            get { return mSyncScrolling; }
            set
            {
                if (mSyncScrolling != value)
                {
                    mSyncScrolling = value;
                    SyncScroll();

                    NotifyPropertyChanged();
                }
            }
        }

        bool mSyncPage = true;
        public bool SyncPage
        {
            get { return mSyncPage; }
            set { mSyncPage = value; NotifyPropertyChanged(); }
        }

        #endregion

        #region Public Commands
        public ICommand CMDProcessVisualDiff { get; set; }

        public ICommand CMDProcessTextDiff { get; set; }

        public ICommand CMDOpenPdfA { get; set; }

        public ICommand CMDOpenPdfB { get; set; }

        public ICommand CMDSyncScroll { get; set; }

        public ICommand CMDOpenFileA { get; set; }

        public ICommand CMDOpenFileB { get; set; }
        #endregion

        #region Operations
        async private void OpenFileA()
        {
            FileOpenPicker filePicker = new FileOpenPicker();
            filePicker.ViewMode = PickerViewMode.List;
            filePicker.FileTypeFilter.Add(".pdf");

            StorageFile file = await filePicker.PickSingleFileAsync();

            var doc = await OpenFilePDFViewer(file, FileAccessMode.ReadWrite);
            if (doc == null)
                return;

            mPDFViewCtrlA.SetDoc(doc);
        }

        async private void OpenFileB()
        {
            FileOpenPicker filePicker = new FileOpenPicker();
            filePicker.ViewMode = PickerViewMode.List;
            filePicker.FileTypeFilter.Add(".pdf");

            StorageFile file = await filePicker.PickSingleFileAsync();

            var doc = await OpenFilePDFViewer(file, FileAccessMode.ReadWrite);
            if (doc == null)
                return;

            mPDFViewCtrlB.SetDoc(doc);
        }

        private async Task<PDFDoc> OpenFilePDFViewer(IStorageFile file, FileAccessMode mode)
        {
            if (file == null)
                return null;

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
                return null;
            }

            PDFDoc doc = new PDFDoc(stream);
            doc.InitSecurityHandler();

            return doc;
        }

        private async void ProcessTextDiff()
        {
            var docA = mPDFViewCtrlA.GetDoc();
            var docB = mPDFViewCtrlB.GetDoc();

            try
            {
                PDFDoc outputDocument = new PDFDoc();
                outputDocument.AppendTextDiff(docA, docB);

                Controls.PDFResultDialog contentDialog = new Controls.PDFResultDialog();
                var pdfCompare = new PDFCompareResult(outputDocument);
                pdfCompare.SetPresentationMode(PDFViewCtrlPagePresentationMode.e_facing_continuous);
                contentDialog.Content = pdfCompare;
                pdfCompare.CloseButtonClicked += delegate
                {
                    contentDialog.Hide();
                };

                await contentDialog.ShowAsync();
            }
            catch (Exception ex)
            { }
        }

        // NOTE: Overlay two PDF files and give the visual color diff between them
        // Used to review two document revisions or drawing blueprints that are overlayed with updated data
        private async void ProcessVisualDiff()
        {
            var docA = mPDFViewCtrlA.GetDoc();
            var docB = mPDFViewCtrlB.GetDoc();

            DiffOptions diffOptions = new DiffOptions();
            diffOptions.SetColorA(new ColorPt(1, 0, 0));
            diffOptions.SetColorB(new ColorPt(0, 0, 0));
            diffOptions.SetBlendMode(GStateBlendMode.e_bl_normal);

            pdftron.PDF.Page docAPage = docA.GetPage(1);
            pdftron.PDF.Page docBPage = docB.GetPage(1);

            try
            {
                PDFDoc outputDocument = new PDFDoc();
                outputDocument.AppendVisualDiff(docAPage, docBPage, diffOptions);                

                Controls.PDFResultDialog contentDialog = new Controls.PDFResultDialog();
                var pdfCompare = new PDFCompareResult(outputDocument);
                pdfCompare.CloseButtonClicked += delegate 
                {
                    contentDialog.Hide();
                };

                contentDialog.Content = pdfCompare;
                await contentDialog.ShowAsync();
            }
            catch (Exception ex)
            { }
        }



        private void SyncScroll()
        {
            if (mSyncScrolling)
            {
                mScrollViewerA = mPDFViewCtrlA.GetScrollViewer();
                if (mScrollViewerA != null)
                {
                    mScrollViewerA.ViewChanged += scrollViewer_ViewChanged;
                    mLastVerticalOffsetA = mScrollViewerA.VerticalOffset;
                }

                mScrollViewerB = mPDFViewCtrlB.GetScrollViewer();
                if (mScrollViewerB != null)
                {
                    mScrollViewerB.ViewChanged += scrollViewer_ViewChanged;
                    mLastVerticalOffsetB = mScrollViewerB.VerticalOffset;
                }
            }
            else
            {
                if (mScrollViewerA != null)
                    mScrollViewerA.ViewChanged -= scrollViewer_ViewChanged;

                if (mScrollViewerB != null)
                    mScrollViewerB.ViewChanged -= scrollViewer_ViewChanged;
            }
        }
        #endregion

        #region Events

        private void scrollViewer_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
        {
            if (mSyncScrolling)
            {
                double sourceLastVerticalOffset = 0;

                // Note: Using the scroll wheel of a mouse causes the built-in ScrollViewer animation to trigger a few times, so the 
                // timeout helps to make it smooth
                if (_lastScrollingElement != sender && Environment.TickCount - _lastScrollChange < ScrollLoopbackTimeout) return;

                _lastScrollingElement = sender;
                _lastScrollChange = Environment.TickCount;

                ScrollViewer sourceScrollViewer;
                ScrollViewer targetScrollViewer;
                if (sender == mScrollViewerA)
                {
                    sourceScrollViewer = mScrollViewerA;
                    targetScrollViewer = mScrollViewerB;

                    sourceLastVerticalOffset = mLastVerticalOffsetA;
                }
                else
                {
                    sourceScrollViewer = mScrollViewerB;
                    targetScrollViewer = mScrollViewerA;

                    sourceLastVerticalOffset = mLastVerticalOffsetB;
                }

                if (mSyncPage)
                {
                    targetScrollViewer.ChangeView(null, sourceScrollViewer.VerticalOffset, null, false);
                }
                else
                {
                    var verticalOffset = targetScrollViewer.VerticalOffset;

                    if (sourceScrollViewer.VerticalOffset > sourceLastVerticalOffset)
                    {
                        verticalOffset += sourceScrollViewer.VerticalOffset - sourceLastVerticalOffset;
                    }
                    else
                    {
                        verticalOffset -= Math.Abs(sourceScrollViewer.VerticalOffset - sourceLastVerticalOffset);
                    }

                    targetScrollViewer.ChangeView(null, verticalOffset, null, true);
                }
            }

            mLastVerticalOffsetA = mScrollViewerA.VerticalOffset;
            mLastVerticalOffsetB = mScrollViewerB.VerticalOffset;
        }

        private void MPDFViewB_OnSetDoc()
        {
            if (!mSyncScrolling)
                return;

            mScrollViewerB = mPDFViewCtrlB.GetScrollViewer();
            mScrollViewerB.ViewChanged += scrollViewer_ViewChanged;

            mLastVerticalOffsetB = mScrollViewerB.VerticalOffset;

            mPDFViewCtrlB.SetZoomEnabled(false);
        }

        private void MPDFViewA_OnSetDoc()
        {
            if (!mSyncScrolling)
                return;

            mScrollViewerA = mPDFViewCtrlA.GetScrollViewer();
            mScrollViewerA.ViewChanged += scrollViewer_ViewChanged;

            mLastVerticalOffsetA = mScrollViewerA.VerticalOffset;

            mPDFViewCtrlA.SetZoomEnabled(false);
        }
        #endregion

    }
}
