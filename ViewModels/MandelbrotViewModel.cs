using System;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using WpfMandelbrotDrawer.Commands;
using WpfMandelbrotDrawer.Models;

using static System.Math;
using static System.Numerics.BigRational;

namespace WpfMandelbrotDrawer.ViewModels
{
    public class MandelbrotViewModel : BaseViewModel
    {
        private static readonly int renderWidth = 1600;
        private static readonly int renderHeight = 900;

        private readonly MandelbrotRenderer mRenderer = new MandelbrotRenderer(renderWidth, renderHeight);

        private WriteableBitmap _CurrentBitmap;
        public WriteableBitmap CurrentBitmap
        {
            get
            {
                return _CurrentBitmap ?? new WriteableBitmap(1, 1, 96, 96, PixelFormats.Bgra32, null);
            }
            private set
            {
                _CurrentBitmap = value;
                OnPropertyChanged(nameof(CurrentBitmap));
            }
        }

        private int _Subdivision = 1;
        public int Subdivision
        {
            get { return _Subdivision; }
            set
            {
                _Subdivision = value;
                OnPropertyChanged(nameof(Subdivision));
            }
        }

        private int _MaxIters = 20;
        public int MaxIters
        {
            get { return _MaxIters; }
            set
            {
                _MaxIters = value;
                OnPropertyChanged(nameof(MaxIters));
            }
        }

        private string _StatusText = "";
        public string StatusText
        {
            get
            {
                return _StatusText ?? "";
            }
            set
            {
                _StatusText = value;
                CommandManager.InvalidateRequerySuggested();
                OnPropertyChanged(nameof(StatusText));
            }
        }

        private Double _LeftEdge = 0.0;
        public Double LeftEdge
        {
            get { return _LeftEdge; }
            set
            {
                _LeftEdge = value;
                OnPropertyChanged(nameof(LeftEdge));
            }
        }

        private Double _RightEdge = 0.0;
        public Double RightEdge
        {
            get { return _RightEdge; }
            set
            {
                _RightEdge = value;
                OnPropertyChanged(nameof(RightEdge));
            }
        }

        private Double _BottomEdge = 0.0;
        public Double BottomEdge
        {
            get { return _BottomEdge; }
            set
            {
                _BottomEdge = value;
                OnPropertyChanged(nameof(BottomEdge));
            }
        }

        private Double _UpperEdge = 0.0;
        public Double UpperEdge
        {
            get { return _UpperEdge; }
            set
            {
                _UpperEdge = value;
                OnPropertyChanged(nameof(UpperEdge));
            }
        }

        enum MappingFuncs
        {
            Squareroot = 0,
            Log,
            Linear,
            Square
        }

        private string _MappingFuncString = "Linear";
        public string MappingFuncString
        {
            get { return _MappingFuncString; }
            set
            {
                _MappingFuncString = value;
                switch (value)
                {
                    case string a when a.Contains("Squareroot"):
                        mRenderer.SetMappingFunc(MandelbrotRenderer.MappingFuncsEnum.Squareroot);
                        break;
                    case string a when a.Contains("Logarithm"):
                        mRenderer.SetMappingFunc(MandelbrotRenderer.MappingFuncsEnum.Log);
                        break;
                    case string a when a.Contains("Square"):
                        mRenderer.SetMappingFunc(MandelbrotRenderer.MappingFuncsEnum.Square);
                        break;
                    default:
                        mRenderer.SetMappingFunc(MandelbrotRenderer.MappingFuncsEnum.Linear);
                        break;
                }
                OnPropertyChanged(nameof(MappingFuncString));
            }
        }

        public RelayCommand RenderCommand { get; private set; }

        public RelayCommand ResetEverythingCommand { get; private set; }
        public RelayCommand ResetViewCommand { get; private set; }

        private WriteableBitmap RenderSet()
        {
            WriteableBitmap bmp = new WriteableBitmap(renderWidth, renderHeight, 96, 96, PixelFormats.Bgra32, null);

            byte[] pixelArray = mRenderer.RenderSet(LeftEdge, RightEdge, UpperEdge, BottomEdge, Subdivision, MaxIters);

            bmp.WritePixels(new Int32Rect(0, 0, renderWidth, renderHeight), pixelArray, renderWidth * 4, 0);
            bmp.Freeze();
            return bmp;
        }

        public async Task ExecuteRender()
        {
            StatusText = "Waiting...";
            CurrentBitmap.Freeze();
            CurrentBitmap = await Task.Run(() => RenderSet());
            StatusText = "Ready";
        }

        public bool CanRender()
        {
            return StatusText == "Ready";
        }

        static MandelbrotViewModel()
        {
            var rect = SystemParameters.WorkArea;
            renderWidth = (int)(rect.Width * 0.8);
            renderHeight = renderWidth * 9 / 16;
        }

        public MandelbrotViewModel()
        {
            
            mRenderer = new MandelbrotRenderer(renderWidth, renderHeight);
            CurrentBitmap = new WriteableBitmap(renderWidth, renderHeight, 96, 96, PixelFormats.Bgra32, null);
            var pixels = from x in Enumerable.Range(0, renderWidth * renderHeight)
                         from x2 in new byte[] { 0x00, 0xFF, 0x00, 0xFF }
                         select x2;
            var pixelArray = pixels.ToArray();
            CurrentBitmap.WritePixels(new Int32Rect(0, 0, renderWidth, renderHeight), pixelArray, renderWidth * 4, 0);
            StatusText = "Ready";
            LeftEdge = -2; BottomEdge = -1; RightEdge = 1; UpperEdge = 1;

            RenderCommand = new RelayCommand(async o => await ExecuteRender(), o => CanRender());
            ResetEverythingCommand = new RelayCommand(async o => await ResetEverything(), o => CanRender());
            ResetViewCommand = new RelayCommand(async o => await ResetView(), o => CanRender());
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
