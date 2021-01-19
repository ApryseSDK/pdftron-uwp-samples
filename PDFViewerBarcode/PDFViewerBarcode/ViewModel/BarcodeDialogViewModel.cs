using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.UI;
using Windows.UI.Xaml.Media;

namespace PDFViewerBarcode.ViewModel
{
    // Interface + Service to allow displaying the content dialog using MVVM pattern
    public interface IBarcodeDialogService
    {
        Task ShowAsync();
    }

    public class BarcodeDialogService : IBarcodeDialogService
    {
        PDFViewerBarcode.Control.BarcodeDialog _barcodeDialog;

        public BarcodeDialogService(BarcodeViewModel viewModel)
        {
            _barcodeDialog = new PDFViewerBarcode.Control.BarcodeDialog();
            _barcodeDialog.DataContext = viewModel;
        }
        public async Task ShowAsync()
        {            
            await _barcodeDialog.ShowAsync();
        }
    }

    class BarcodeDialogViewModel : BaseViewModel
    {
        readonly IBarcodeDialogService _dialog;

        public BarcodeDialogViewModel(IBarcodeDialogService dialog)
        {
            _dialog = dialog;
        }

        public async Task ShowDialog()
        {
            await _dialog.ShowAsync();
        }
    }

    public enum BarcodeType
    {
        None,
        UPCA,
        DataMatrix,
        QRCode
    }

    public class Barcode
    {
        public string Name { get; set; }
        public BarcodeType Type { get; set; }
    }

    public class BarcodeViewModel : BaseViewModel
    {
        private static Brush DEFAULT_INPUT_COLOR = new SolidColorBrush(Windows.UI.Colors.Black);
        private static Brush ERROR_INPUT_COLOR = new SolidColorBrush(Windows.UI.Colors.Red);
        private const string DEFAULT_DATAMATRIX_DATA = "https://www.pdftron.com/";
        private const string DEFAULT_QRCODE_DATA = "https://www.pdftron.com/";
        private const string DEFAULT_UPCA_DATA = "01234567890";

        public BarcodeViewModel()
        {
            CMDPrimaryButtonClick = new RelayCommand(PrimaryButtonCommand);
        }

        #region Commands
        public ICommand CMDPrimaryButtonClick { get; set; }
        #endregion

        #region Operations
        private void PrimaryButtonCommand()
        {

        }

        #endregion

        #region public Properties
        public ObservableCollection<Barcode> Barcodes { get; set; } = new ObservableCollection<Barcode>()
        {
            new Barcode
            {
                Name = "UPC-A",
                Type = BarcodeType.UPCA
            },
            new Barcode
            {
                Name = "Data Matrix",
                Type = BarcodeType.DataMatrix
            },
            new Barcode
            {
                Name = "QR Code",
                Type = BarcodeType.QRCode
            }
        };

        string _title = "Barcode Selection";
        public string Title
        {
            get { return _title; }
            set { _title = value; NotifyPropertyChanged(); }
        }

        string _primaryButtontext = "OK";
        public string PrimaryButtontext
        {
            get { return _primaryButtontext; }
            set { _primaryButtontext = value; NotifyPropertyChanged(); }
        }

        string _secondaryButtonText = "Cancel";
        public string SecondaryButtonText
        {
            get { return _secondaryButtonText; }
            set { _secondaryButtonText = value; NotifyPropertyChanged(); }
        }

        string _barcodeData = string.Empty;
        public string BarcodeData
        {
            get { return _barcodeData; }
            set
            {
                _barcodeData = value;
                NotifyPropertyChanged();
            }
        }

        bool _inputDataVisible = false;
        public bool InputDataVisible
        {
            get { return _inputDataVisible; }
            set
            {
                _inputDataVisible = value;
                NotifyPropertyChanged();
            }
        }

        Barcode _barcodeSelected = null;
        public Barcode BarcodeSelected
        {
            get { return _barcodeSelected; }
            set
            {
                _barcodeSelected = value;
                NotifyPropertyChanged();

                if (_barcodeSelected != null)
                {
                    InputDataVisible = true;
                    PrimaryButtonEnabled = true;

                    switch (_barcodeSelected.Type)
                    {
                        case BarcodeType.UPCA:
                            InputData = DEFAULT_UPCA_DATA;
                            break;
                        case BarcodeType.DataMatrix:
                            InputData = DEFAULT_DATAMATRIX_DATA;
                            break;

                        case BarcodeType.QRCode:
                            InputData = DEFAULT_QRCODE_DATA;
                            break;
                    }
                }
            }
        }

        string _inputData = string.Empty;
        public string InputData
        {
            get { return _inputData; }
            set
            {
                if (_inputData != value)
                {
                    _inputData = value;

                    switch (BarcodeSelected.Type)
                    {
                        case BarcodeType.UPCA:
                            var count = _inputData.Count(x => Char.IsDigit(x));
                            if (count >= 11)
                            {
                                PrimaryButtonEnabled = true;
                                InputDataColor = DEFAULT_INPUT_COLOR;
                            }
                            else
                            {
                                PrimaryButtonEnabled = false;
                                InputDataColor = ERROR_INPUT_COLOR;
                            }
                            break;
                    }

                    NotifyPropertyChanged();
                }
            }
        }

        public Brush _inputDataColor = DEFAULT_INPUT_COLOR;
        public Brush InputDataColor
        {
            get { return _inputDataColor; }
            set
            {
                if (_inputDataColor != value)
                {
                    _inputDataColor = value;
                    NotifyPropertyChanged();
                }
            }
        }

        bool _primaryButtonEnabled = false;
        public bool PrimaryButtonEnabled 
        {
            get { return _primaryButtonEnabled; }
            set
            {
                _primaryButtonEnabled = value;
                NotifyPropertyChanged();
            }
        }

        #endregion
    }
}
