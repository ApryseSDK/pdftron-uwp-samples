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
using System.Collections.Generic;
using pdftron.Filters;
using Windows.ApplicationModel.DataTransfer;
using System.IO;
using Bcl.EasyPdfCloud;
using System.Text;
using Windows.Data.Json;

namespace Pdf2Word_cloud
{
    class MainPageViewModel : BaseViewModel
    {
        #region Private
        Client mClient;
        IStorageFile mInputFile;
        #endregion

        #region Defaults
        string CLIENT_ID = "05ee808265f24c66b2b8e31d90c31ab1";
        string CLIENT_SECRET = "16ABE87E052059F147BB2A491944BF7EA7876D0F843DED105CBA094C887CBC99";
        #endregion

        public MainPageViewModel()
        {
            // Initialize commands
            CMDExitApplication = new RelayCommand(ExitApplication);
            CMDPdf2Word = new RelayCommand(ConvertToWord);

            _ = Init();

            // Set control background color to gray
            PDFViewCtrl.SetBackgroundColor(Windows.UI.Color.FromArgb(100, 49, 51, 53));

            mClient = new Client(CLIENT_ID, CLIENT_SECRET);
        }

        private async Task Init()
        {
            mInputFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Resources/LoremIpsum.pdf"));

            if (mInputFile != null)
            {
                PDFDoc doc = new PDFDoc(mInputFile);
                PDFViewCtrl.SetDoc(doc);
            }
        }

        #region Public Properties

        PDFViewCtrl _pDFViewCtrl = new PDFViewCtrl();
        public PDFViewCtrl PDFViewCtrl
        { 
            get { return _pDFViewCtrl; } 
            set { _pDFViewCtrl = value; NotifyPropertyChanged(); } 
        }

        Visibility _progressRingVis =  Visibility.Collapsed;

        public Visibility ProgressVisible 
        {
            get { return _progressRingVis; }
            set { _progressRingVis = value; NotifyPropertyChanged(); }
        }

        bool _isProgressRingActive = false;

        public bool IsProgressRingActive 
        {
            get { return _isProgressRingActive; }
            set { _isProgressRingActive = value; NotifyPropertyChanged(); }

        }

        #endregion

        #region Public Commands
        public ICommand CMDPdf2Word { get; set; }

        public ICommand CMDExitApplication { get; set; }

        #endregion

        #region Operations

        private void ConvertToWord()
        {
            // Already converting so just wait
            if (IsProgressRingActive)
                return;

            _ = ConvertPdf2Word();
        }

        /// <summary>
        /// Convert HTML URL to PDF
        /// </summary>
        /// <param name="url">The HTTP or HTTPS URL</param>
        /// <param name="name">Name of the PDF file</param>
        /// <returns></returns>
        public async Task<bool> ConvertPdf2Word()
        {
            IsProgressRingActive = true;
            ProgressVisible = Visibility.Visible;

            var workflowSetting = new WorkflowSetting();
            var wfConvertPdfToWord = workflowSetting.AddNewConvertPdfToWordTask();
            wfConvertPdfToWord.Revision = 1;
            wfConvertPdfToWord.KeepInputFile = false;

            Stream stream = await mInputFile.OpenStreamForReadAsync();


            var job = await mClient.StartNewJobAsync(workflowSetting, stream, mInputFile.Name);
            try
            {
                // Wait until job execution is completed and return PDF file
                var job_rsl = await job.WaitForJobExecutionCompletionAsync();

                // Save the job result to memory, and then prompt to save on disk
                using (var memoryStream = new MemoryStream())
                {
                    await job_rsl.FileData.Stream.CopyToAsync(memoryStream);
                    byte[] outputBytes = memoryStream.ToArray();

                    var outputFile = await PromptSaveDialoge(mInputFile.Name);

                    await FileIO.WriteBytesAsync(outputFile, outputBytes);
                }
            }
            catch (Exception ex)
            {
                // swallow exception
            }
            finally 
            { 
                IsProgressRingActive = false;
                ProgressVisible = Visibility.Collapsed;
            }

            return false;
        }

        private async Task<IStorageFile> PromptSaveDialoge(string name)
        {
            if (string.IsNullOrEmpty(name))
                return null;

            // Start File Picker so we can SaveAs/Export document
            var savePicker = new Windows.Storage.Pickers.FileSavePicker();
            savePicker.FileTypeChoices.Add(name, new List<string> { ".docx" });

            var storageFile = await savePicker.PickSaveFileAsync();

            return storageFile;
        }

        private void ExitApplication()
        {
            CoreApplication.Exit();
        }

        #endregion
    }
}
