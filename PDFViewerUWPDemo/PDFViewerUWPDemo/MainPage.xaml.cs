using Windows.UI.Xaml.Controls;
using PDFViewerUWP_PDFTron.ViewModel;


// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace PDFViewerUWP_PDFTron
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {

        MainPageViewModel mViewModel;

        public MainPage()
        {
            this.InitializeComponent();
            this.DataContext = mViewModel = new MainPageViewModel();            
        }

        bool mNeedClearQuery = false;
        private void SearchBox_QuerySubmitted(SearchBox sender, SearchBoxQuerySubmittedEventArgs args)
        {
            if (string.IsNullOrEmpty(args.QueryText))
                return;

            mViewModel.SearchTextAndHighlight(searchText.QueryText);
            mNeedClearQuery = true;
        }

        private void searchText_QueryChanged(SearchBox sender, SearchBoxQueryChangedEventArgs args)
        {
            if (string.IsNullOrEmpty(args.QueryText))
            {
                if (mNeedClearQuery)
                {
                    mViewModel.CancelSearchTextHighlight();
                    mNeedClearQuery = false;
                }
            }
        }
    }
}
