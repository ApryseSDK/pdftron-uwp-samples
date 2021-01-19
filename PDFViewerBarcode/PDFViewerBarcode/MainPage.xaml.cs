using System;
using System.Runtime.InteropServices.WindowsRuntime;

using pdftron.PDF;
using ZXing;
using System.Threading.Tasks;

using PDFRect = pdftron.PDF.Rect;
using Windows.Graphics.Imaging;
using Windows.UI.Xaml.Media.Imaging;
using System.Drawing;
using Windows.Storage.Streams;
using PDFViewerBarcode.ViewModel;


// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace PDFViewerBarcode
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Windows.UI.Xaml.Controls.Page
    {
        public MainPage()
        {
            this.InitializeComponent();
            this.DataContext = new MainPageViewModel();
        }
    }
}
