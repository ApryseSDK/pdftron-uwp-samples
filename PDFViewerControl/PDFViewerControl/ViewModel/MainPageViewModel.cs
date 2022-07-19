using pdftron.PDF.Tools.Controls.Viewer;
using PDFViewerUWP_PDFTron.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace PDFViewerControl.ViewModel
{
    public class MainPageViewModel : BaseViewModel
    {
        public MainPageViewModel()
        {
            _viewerControl = new ViewerControl();
        }

        public async Task InitAsync()
        {
            // NOTE: here is a good place for customizing the UI of the ViewerControl
            //ViewerControl.ShowPrintOption = false;
            //ViewerControl.ShowSaveOption = false;

            // NOTE: initialize with PDF, Image, Office, etc...
            //var file = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Resources/GettingStarted.pdf"));
            var file = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Resources/windows_bg.jpg"));
            if (file != null)
                await ViewerControl.ActivateWithFileAsync(file);
        }

        ViewerControl _viewerControl;
        public ViewerControl ViewerControl 
        {
            get { return _viewerControl; }
            set { _viewerControl = value; NotifyPropertyChanged(); }
        }
    }
}
