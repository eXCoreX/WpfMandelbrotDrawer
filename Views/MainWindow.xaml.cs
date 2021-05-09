using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WpfMandelbrotDrawer.ViewModels;

namespace WpfMandelbrotDrawer.Views
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private bool DrawSelectionBox { get; set; }
        private Point MouseDownPos { get; set; }

        private void Image_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var mousePos = e.GetPosition(OuterGrid);
            var l = RenderImage.TranslatePoint(new Point(0, 0), OuterGrid);

            if (mousePos.X < l.X) return;
            // Capture and track the mouse.
            DrawSelectionBox = true;
            MouseDownPos = e.GetPosition(OuterGrid);
            OuterGrid.CaptureMouse();

            // Initial placement of the drag selection box.         
            Canvas.SetLeft(SelectionBox, MouseDownPos.X);
            Canvas.SetTop(SelectionBox, MouseDownPos.Y);
            SelectionBox.Width = 0;
            SelectionBox.Height = 0;

            // Make the drag selection box visible.
            SelectionBox.Visibility = Visibility.Visible;
        }

        private async void Image_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (!DrawSelectionBox) return;
            // Release the mouse capture and stop tracking it.
            DrawSelectionBox = false;
            OuterGrid.ReleaseMouseCapture();

            // Hide the drag selection box.
            SelectionBox.Visibility = Visibility.Collapsed;
            var l = RenderImage.TranslatePoint(new Point(0, 0), OuterGrid);
            var mouseUpPos = e.GetPosition(OuterGrid);

            if (mouseUpPos.X < l.X || mouseUpPos.X > l.X + RenderImage.ActualWidth) return;


            if (!(DataContext is MandelbrotViewModel dc) || !dc.CanRender()) return;
            var topLeft = SelectionBox.TranslatePoint(new Point(0, 0), RenderImage);
            var downRight = topLeft;
            downRight.X += SelectionBox.Width;
            downRight.Y += SelectionBox.Height;

            var oldL = dc.LeftEdge;
            var oldR = dc.RightEdge;
            var oldD = dc.BottomEdge;
            var oldU = dc.UpperEdge;

            var newL = oldL + (oldR - oldL) * topLeft.X / dc.CurrentBitmap.PixelWidth *
                MandelbrotViewModel.DpiScale;
            var newR = oldL + (oldR - oldL) * downRight.X / dc.CurrentBitmap.PixelWidth *
                MandelbrotViewModel.DpiScale;
            var newU = oldU - (oldU - oldD) * topLeft.Y / dc.CurrentBitmap.PixelHeight *
                MandelbrotViewModel.DpiScale;
            var newD = oldU - (oldU - oldD) * downRight.Y / dc.CurrentBitmap.PixelHeight *
                MandelbrotViewModel.DpiScale;
            dc.LeftEdge = newL;
            dc.RightEdge = newR;
            dc.UpperEdge = newU;
            dc.BottomEdge = newD;

            await dc.ExecuteRender();
        }

        private void Image_MouseMove(object sender, MouseEventArgs e)
        {
            if (DrawSelectionBox)
            {
                var mousePos = e.GetPosition(OuterGrid);
                var l = RenderImage.TranslatePoint(new Point(0, 0), OuterGrid);

                if (mousePos.X < l.X || mousePos.X > l.X + RenderImage.ActualWidth)
                {
                    DrawSelectionBox = false;
                    OuterGrid.ReleaseMouseCapture();
                    SelectionBox.Visibility = Visibility.Collapsed;
                    return;
                }

                if (MouseDownPos.X < mousePos.X)
                {
                    Canvas.SetLeft(SelectionBox, MouseDownPos.X);
                    SelectionBox.Width = mousePos.X - MouseDownPos.X;
                }
                else
                {
                    Canvas.SetLeft(SelectionBox, mousePos.X);
                    SelectionBox.Width = MouseDownPos.X - mousePos.X;
                }

                if (MouseDownPos.Y < mousePos.Y)
                {
                    Canvas.SetTop(SelectionBox, MouseDownPos.Y);
                    SelectionBox.Height = mousePos.Y - MouseDownPos.Y;
                }
                else
                {
                    Canvas.SetTop(SelectionBox, mousePos.Y);
                    SelectionBox.Height = MouseDownPos.Y - mousePos.Y;
                }
            }
        }

        private void MandelWindow_Loaded(object sender, RoutedEventArgs e)
        {
            MinHeight = ActualHeight;
            MinWidth = ActualWidth;
        }
    }
}