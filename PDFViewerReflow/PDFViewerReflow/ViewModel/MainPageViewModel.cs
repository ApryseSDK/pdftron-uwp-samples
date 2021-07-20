using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using pdftron.PDF;
using pdftron.PDF.Tools;
using pdftron.PDF.Tools.Controls;
using Windows.ApplicationModel.Core;
using Windows.UI.Xaml;

namespace PDFViewerReflow
{
    class MainPageViewModel : BaseViewModel
    {
        #region Private
        private PDFViewCtrl _pDFViewCtrl;
        private ReflowView _reflowViewCtrl;
        private bool _IsPDFVisible = false;
        #endregion

        public MainPageViewModel()
        {
            // Initialize commands
            CMDExitApplication = new RelayCommand(ExitApplication);
            CMDToggleButtonClicked = new RelayCommand(ToggleButtonClicked);

            // Load a document into PDFDoc
            PDFDoc doc = new PDFDoc("Resources/LoremIpsum.pdf");

            // Attach the doc to the Reflow view control
            _reflowViewCtrl = new ReflowView(doc, 1);

            // Attach the doc to the PDF view control
            _pDFViewCtrl = new PDFViewCtrl();
            _pDFViewCtrl.SetDoc(doc);
        }

        #region Public Properties
        public PDFViewCtrl PDFViewCtrl
        {
            get { return _pDFViewCtrl; }
            set { _pDFViewCtrl = value; NotifyPropertyChanged(); }
        }

        public ReflowView ReflowViewCtrl 
        { 
            get { return _reflowViewCtrl; }
            set { _reflowViewCtrl = value; NotifyPropertyChanged(); }
        }

        public bool IsPDFVisible
        {
            get { return _IsPDFVisible; }
            set { _IsPDFVisible = value; }
        }

        public Visibility ReflowVisibility { get; set; } = Visibility.Collapsed;

        public Visibility PDFVisibility { get; set; } = Visibility.Visible;

        public string ToggleButtonText { get; set; } = "Reflow";
        #endregion

        #region Public Commands
        public ICommand CMDExitApplication { get; set; }
        public ICommand CMDToggleButtonClicked { get; set; }
        #endregion

        #region Operations
        private void ToggleButtonClicked()
        {
            if (_IsPDFVisible)
            {
                // Update viewer to PDF view
                IsPDFVisible = false;
                ToggleButtonText = "Reflow";
                PDFVisibility = Visibility.Visible;
                ReflowVisibility = Visibility.Collapsed;
            }
            else
            {
                // Update viewer to Reflow view
                IsPDFVisible = true;
                ToggleButtonText = "PDF";
                PDFVisibility = Visibility.Collapsed;
                ReflowVisibility = Visibility.Visible;
            }

            NotifyPropertyChanged();
            NotifyPropertyChanged(nameof(ToggleButtonText));
            NotifyPropertyChanged(nameof(PDFVisibility));
            NotifyPropertyChanged(nameof(ReflowVisibility));
        }

        private void ExitApplication()
        {
            CoreApplication.Exit();
        }
        #endregion
    }
}
