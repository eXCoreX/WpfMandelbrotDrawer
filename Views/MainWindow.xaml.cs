using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WpfMandelbrotDrawer.Models;
using WpfMandelbrotDrawer.ViewModels;

namespace WpfMandelbrotDrawer.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private bool DrawSelectionBox { get;  set; }
        private Point MouseDownPos { get;  set; }

        private void Image_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Point mousePos = e.GetPosition(outerGrid);
            var l = renderImage.TranslatePoint(new Point(0, 0), outerGrid);

            if (mousePos.X < l.X)
            {
                return;
            }
            // Capture and track the mouse.
            DrawSelectionBox = true;
            MouseDownPos = e.GetPosition(outerGrid);
            outerGrid.CaptureMouse();

            // Initial placement of the drag selection box.         
            Canvas.SetLeft(selectionBox, MouseDownPos.X);
            Canvas.SetTop(selectionBox, MouseDownPos.Y);
            selectionBox.Width = 0;
            selectionBox.Height = 0;

            // Make the drag selection box visible.
            selectionBox.Visibility = Visibility.Visible;
        }

        private async void Image_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (!DrawSelectionBox)
            {
                return;
            }
            // Release the mouse capture and stop tracking it.
            DrawSelectionBox = false;
            outerGrid.ReleaseMouseCapture();

            // Hide the drag selection box.
            selectionBox.Visibility = Visibility.Collapsed;
            var l = renderImage.TranslatePoint(new Point(0, 0), outerGrid);
            Point mouseUpPos = e.GetPosition(outerGrid);

            if (mouseUpPos.X < l.X || mouseUpPos.X > (l.X + renderImage.ActualWidth))
            {
                return;
            }

            var dc = DataContext as MandelbrotViewModel;

            if (dc.CanRender())
            {
                Point TopLeft = selectionBox.TranslatePoint(new Point(0, 0), renderImage);
                Point DownRight = TopLeft;
                DownRight.X += selectionBox.Width;
                DownRight.Y += selectionBox.Height;

                var oldL = dc.LeftEdge;
                var oldR = dc.RightEdge;
                var oldD = dc.BottomEdge;
                var oldU = dc.UpperEdge;

                var newL = oldL + (oldR - oldL) * TopLeft.X / dc.CurrentBitmap.PixelWidth * MandelbrotViewModel.DpiScale;
                var newR = oldL + (oldR - oldL) * DownRight.X / dc.CurrentBitmap.PixelWidth * MandelbrotViewModel.DpiScale;
                var newU = oldU - (oldU - oldD) * TopLeft.Y / dc.CurrentBitmap.PixelHeight * MandelbrotViewModel.DpiScale;
                var newD = oldU - (oldU - oldD) * DownRight.Y / dc.CurrentBitmap.PixelHeight * MandelbrotViewModel.DpiScale;
                dc.LeftEdge = newL;
                dc.RightEdge = newR;
                dc.UpperEdge = newU;
                dc.BottomEdge = newD;
            
                await dc.ExecuteRender();
            }
        }

        private void Image_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (DrawSelectionBox)
            {
                Point mousePos = e.GetPosition(outerGrid);
                var l = renderImage.TranslatePoint(new Point(0, 0), outerGrid);

                if (mousePos.X < l.X || mousePos.X > (l.X + renderImage.ActualWidth))
                {
                    DrawSelectionBox = false;
                    outerGrid.ReleaseMouseCapture();
                    selectionBox.Visibility = Visibility.Collapsed;
                    return;
                }

                if (MouseDownPos.X < mousePos.X)
                {
                    Canvas.SetLeft(selectionBox, MouseDownPos.X);
                    selectionBox.Width = mousePos.X - MouseDownPos.X;
                }
                else
                {
                    Canvas.SetLeft(selectionBox, mousePos.X);
                    selectionBox.Width = MouseDownPos.X - mousePos.X;
                }

                if (MouseDownPos.Y < mousePos.Y)
                {
                    Canvas.SetTop(selectionBox, MouseDownPos.Y);
                    selectionBox.Height = mousePos.Y - MouseDownPos.Y;
                }
                else
                {
                    Canvas.SetTop(selectionBox, mousePos.Y);
                    selectionBox.Height = MouseDownPos.Y - mousePos.Y;
                }
            }
        }
        

        public MainWindow()
        {
            InitializeComponent();
        }

        private void MandelWindow_Loaded(object sender, RoutedEventArgs e)
        {
            MinHeight = ActualHeight;
            MinWidth = ActualWidth;
        }
    }
}
