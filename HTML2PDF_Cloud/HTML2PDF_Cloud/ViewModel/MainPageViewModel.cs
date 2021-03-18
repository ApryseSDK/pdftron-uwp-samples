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

namespace HTML2PDF_Cloud
{
    class MainPageViewModel : BaseViewModel
    {
        #region Private
        Client mClient;
        #endregion

        #region Defaults
        string CLIENT_ID = "05ee808265f24c66b2b8e31d90c31ab1";
        string CLIENT_SECRET = "16ABE87E052059F147BB2A491944BF7EA7876D0F843DED105CBA094C887CBC99";
        #endregion

        public MainPageViewModel()
        {
            // Initialize commands
            CMDExitApplication = new RelayCommand(ExitApplication);
            CMDHTML2PDF = new RelayCommand(ConvertHTML);
            CMDSaveDocAs = new RelayCommand(SaveDocAs);

            // Set control background color to gray
            PDFViewCtrl.SetBackgroundColor(Windows.UI.Color.FromArgb(100, 49, 51, 53));

            mClient = new Client(CLIENT_ID, CLIENT_SECRET);
        }

        #region Public Properties

        PDFViewCtrl _pDFViewCtrl = new PDFViewCtrl();
        public PDFViewCtrl PDFViewCtrl
        { 
            get { return _pDFViewCtrl; } 
            set { _pDFViewCtrl = value; NotifyPropertyChanged(); } 
        }

        string mURL = "https://www.pdftron.com/";

        public string URL 
        { 
            get { return mURL; } 
            set { mURL = value; NotifyPropertyChanged(); } 
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
        public ICommand CMDHTML2PDF { get; set; }

        public ICommand CMDExitApplication { get; set; }

        public ICommand CMDSaveDocAs { get; set; }

        #endregion

        #region Operations

        private void ConvertHTML()
        {
            // Already converting so just wait
            if (IsProgressRingActive)
                return;

            _ = ConvertHTML2PDF("pdftron");
        }

        /// <summary>
        /// Convert HTML URL to PDF
        /// </summary>
        /// <param name="url">The HTTP or HTTPS URL</param>
        /// <param name="name">Name of the PDF file</param>
        /// <returns></returns>
        public async Task<bool> ConvertHTML2PDF(string name)
        {
            if (string.IsNullOrWhiteSpace(URL))
                return false;

            // Check if URL is valid HTTP or HTTPS
            Uri uriResult;
            bool result = Uri.TryCreate(URL, UriKind.Absolute, out uriResult)
                && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);

            if (result == false)
                return false;

            IsProgressRingActive = true;
            ProgressVisible = Visibility.Visible;

            var workflowSetting = new WorkflowSetting();
            var wfConvertToPDF = workflowSetting.AddNewConvertToPdfTask();

            string ddd = @"{ ""Url"": """ + URL + @""" }"; // @"{ ""Url"": ""https://www.pdftron.com/"" }";
            byte[] byteArray = Encoding.UTF8.GetBytes(ddd);
            MemoryStream stream = new MemoryStream(byteArray);

            var job = await mClient.StartNewJobAsync(workflowSetting, stream, "pdftron.bclurl");
            try
            {
                // Wait until job execution is completed and return PDF file
                var job_rsl = await job.WaitForJobExecutionCompletionAsync();

                var folder = ApplicationData.Current.LocalFolder;
                StorageFile outputFile = await folder.CreateFileAsync(name + ".pdf",
                            CreationCollisionOption.ReplaceExisting);

                using (var memoryStream = new MemoryStream())
                {
                    await job_rsl.FileData.Stream.CopyToAsync(memoryStream);
                    byte[] outputBytes = memoryStream.ToArray();
                    await FileIO.WriteBytesAsync(outputFile, outputBytes);
                }

                // Open PDFDoc on PDFViewer
                PDFDoc doc = new PDFDoc(outputFile);
                PDFViewCtrl.SetDoc(doc);
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

        private void ExitApplication()
        {
            CoreApplication.Exit();
        }

        #endregion
    }
}
