using pdftron.PDF;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace PDFCompare.Controls
{
    public sealed partial class PDFCompareResult : UserControl
    {
        PDFViewCtrl pDFViewCtrl;

        public PDFCompareResult(PDFDoc doc)
        {
            this.InitializeComponent();

            pDFViewCtrl = new PDFViewCtrl();
            pdfViewBorder.Child = pDFViewCtrl;

            pDFViewCtrl.SetDoc(doc);
        }

        public void SetPresentationMode(PDFViewCtrlPagePresentationMode mode)
        {
            pDFViewCtrl.SetPagePresentationMode(mode);
        }

        private void SaveDocAs()
        {
            if (!pDFViewCtrl.HasDocument)
                return;

            var doc = pDFViewCtrl.GetDoc();

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

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            SaveDocAs();
        }

        public event EventHandler CloseButtonClicked;

        private void OnCloseButtonClicked(EventArgs e)
        {
            EventHandler handler = CloseButtonClicked;
            handler?.Invoke(this, e);
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            OnCloseButtonClicked(new EventArgs());
        }
    }
}
