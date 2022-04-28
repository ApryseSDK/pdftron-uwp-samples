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

        }

        public async Task InitAsync()
        {
            StorageFile file = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Resources/GettingStarted.pdf"));
            if (file != null)
                await ViewerControl.ActivateWithFileAsync(file);
        }

        ViewerControl _viewerControl = new ViewerControl();
        public ViewerControl ViewerControl 
        {
            get { return _viewerControl; }
            set { _viewerControl = value; NotifyPropertyChanged(); }
        }
    }
}
