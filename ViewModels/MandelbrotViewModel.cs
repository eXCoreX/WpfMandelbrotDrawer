using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WpfMandelbrotDrawer.Commands;
using WpfMandelbrotDrawer.Models;

namespace WpfMandelbrotDrawer.ViewModels
{
    public class MandelbrotViewModel : BaseViewModel
    {
        private static readonly int RenderWidth;
        private static readonly int RenderHeight;
        public static readonly double DpiScale = VisualTreeHelper.GetDpi(new ContainerVisual()).DpiScaleX;

        private readonly MandelbrotRenderer _mRenderer;

        private double _bottomEdge;

        private WriteableBitmap _currentBitmap;

        private double _leftEdge;

        private string _mappingFuncString = "Linear";

        private int _maxIters = 20;

        private double _rightEdge;

        private string _statusText = "";

        private int _subdivision = 1;

        private double _upperEdge;

        static MandelbrotViewModel()
        {
            var rect = SystemParameters.WorkArea;
            RenderWidth = (int) (rect.Width * 0.8 * DpiScale);
            RenderHeight = RenderWidth * 9 / 16;
        }

        public MandelbrotViewModel()
        {
            _mRenderer = new MandelbrotRenderer(RenderWidth, RenderHeight);
            CurrentBitmap = new WriteableBitmap(RenderWidth, RenderHeight, 96 * DpiScale, 96 * DpiScale,
                PixelFormats.Bgra32, null);
            var pixels = from x in Enumerable.Range(0, RenderWidth * RenderHeight)
                from x2 in new byte[] {0x00, 0xFF, 0x00, 0xFF}
                select x2;
            var pixelArray = pixels.ToArray();
            CurrentBitmap.WritePixels(new Int32Rect(0, 0, RenderWidth, RenderHeight), pixelArray, RenderWidth * 4, 0);
            StatusText = "Ready";
            LeftEdge = -2;
            BottomEdge = -1;
            RightEdge = 1;
            UpperEdge = 1;

            RenderCommand = new RelayCommand(async o => await ExecuteRender(), o => CanRender());
            ResetEverythingCommand = new RelayCommand(async o => await ResetEverything(), o => CanRender());
            ResetViewCommand = new RelayCommand(async o => await ResetView(), o => CanRender());
        }

        public WriteableBitmap CurrentBitmap
        {
            get => _currentBitmap ?? new WriteableBitmap(1, 1, 96 * DpiScale, 96 * DpiScale, PixelFormats.Bgra32, null);
            private set
            {
                _currentBitmap = value;
                OnPropertyChanged(nameof(CurrentBitmap));
            }
        }

        public int Subdivision
        {
            get => _subdivision;
            set
            {
                _subdivision = value;
                OnPropertyChanged(nameof(Subdivision));
            }
        }

        public int MaxIters
        {
            get => _maxIters;
            set
            {
                _maxIters = value;
                OnPropertyChanged(nameof(MaxIters));
            }
        }

        public string StatusText
        {
            get => _statusText ?? "";
            set
            {
                _statusText = value;
                CommandManager.InvalidateRequerySuggested();
                OnPropertyChanged(nameof(StatusText));
            }
        }

        public double LeftEdge
        {
            get => _leftEdge;
            set
            {
                _leftEdge = value;
                OnPropertyChanged(nameof(LeftEdge));
            }
        }

        public double RightEdge
        {
            get => _rightEdge;
            set
            {
                _rightEdge = value;
                OnPropertyChanged(nameof(RightEdge));
            }
        }

        public double BottomEdge
        {
            get => _bottomEdge;
            set
            {
                _bottomEdge = value;
                OnPropertyChanged(nameof(BottomEdge));
            }
        }

        public double UpperEdge
        {
            get => _upperEdge;
            set
            {
                _upperEdge = value;
                OnPropertyChanged(nameof(UpperEdge));
            }
        }

        public string MappingFuncString
        {
            get => _mappingFuncString;
            set
            {
                _mappingFuncString = value;
                switch (value)
                {
                    case string a when a.Contains("Squareroot"):
                        _mRenderer.SetMappingFunc(MandelbrotRenderer.MappingFuncsEnum.Squareroot);
                        break;
                    case string a when a.Contains("Logarithm"):
                        _mRenderer.SetMappingFunc(MandelbrotRenderer.MappingFuncsEnum.Log);
                        break;
                    case string a when a.Contains("Square"):
                        _mRenderer.SetMappingFunc(MandelbrotRenderer.MappingFuncsEnum.Square);
                        break;
                    default:
                        _mRenderer.SetMappingFunc(MandelbrotRenderer.MappingFuncsEnum.Linear);
                        break;
                }

                OnPropertyChanged(nameof(MappingFuncString));
            }
        }

        public RelayCommand RenderCommand { get; }

        public RelayCommand ResetEverythingCommand { get; }

        public RelayCommand ResetViewCommand { get; }

        private WriteableBitmap RenderSet()
        {
            var bmp = new WriteableBitmap(RenderWidth, RenderHeight, 96 * DpiScale, 96 * DpiScale, PixelFormats.Bgra32,
                null);

            var pixelArray = _mRenderer.RenderSet(LeftEdge, RightEdge, UpperEdge, BottomEdge, Subdivision, MaxIters);

            bmp.WritePixels(new Int32Rect(0, 0, RenderWidth, RenderHeight), pixelArray, RenderWidth * 4, 0);
            bmp.Freeze();
            return bmp;
        }

        public async Task ExecuteRender()
        {
            StatusText = "Waiting...";
            CurrentBitmap.Freeze();
            CurrentBitmap = await Task.Run(RenderSet);
            StatusText = "Ready";
        }

        public bool CanRender()
        {
            return StatusText == "Ready";
        }

        private async Task ResetEverything()
        {
            LeftEdge = -2;
            RightEdge = 1;
            BottomEdge = -1;
            UpperEdge = 1;
            Subdivision = 1;
            MaxIters = 20;
            MappingFuncString = "Linear";
            await ExecuteRender();
        }

        private async Task ResetView()
        {
            LeftEdge = -2;
            RightEdge = 1;
            BottomEdge = -1;
            UpperEdge = 1;
            await ExecuteRender();
        }
    }
}