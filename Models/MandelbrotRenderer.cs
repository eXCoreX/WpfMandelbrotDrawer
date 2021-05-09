using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;

namespace WpfMandelbrotDrawer.Models
{
    public class MandelbrotRenderer
    {
        public enum MappingFuncsEnum
        {
            Squareroot = 0,
            Log,
            Linear,
            Square
        }

        private static readonly Func<double, double>[] MappingFuncs =
        {
            x => Math.Sqrt(x),
            x => (Math.Log(x + 1 / Math.E) + 1) /
                 1.31326168752, // Modified natural log which returns 0 for x = 0 and 1 for x = 1
            x => x,
            x => x * x
        };

        private readonly int _pixelHeight;
        private readonly int _pixelWidth;

        private Func<double, double> _mappingFunc = x => x;

        public MandelbrotRenderer(int pixelWidth, int pixelHeight)
        {
            _pixelWidth = pixelWidth;
            _pixelHeight = pixelHeight;
        }

        public byte[] RenderSet(double xl, double xr, double yd, double yu, int subdiv, int maxiter)
        {
            var ms = new MandelbrotSet(maxiter);
            var pixels = new byte[_pixelHeight][];

            var rowDispencer = new ConcurrentQueue<int>(Enumerable.Range(0, _pixelHeight));

            var tasks = new Task[Environment.ProcessorCount];
            for (int i = 0; i < tasks.Length; i++)
                // These tasks manage themselves, grabbing rows that need rendering at their own demand
                // This way threads are fully loaded and not waiting for others to finish until the last few rows (at most tasks.Length - 1)
                tasks[i] = Task.Run(() =>
                {
                    var haveNextRow = rowDispencer.TryDequeue(out var row);
                    while (haveNextRow)
                    {
                        // Debug.WriteLine($"Starting row {row}, threadID = {Thread.CurrentThread.ManagedThreadId}");

                        var pix = new byte[_pixelWidth * 4];
                        var y1 = yd + (yu - yd) * row / _pixelHeight; // Lower bound in coordinates
                        var y2 = yd + (yu - yd) * (row + 1) / _pixelHeight; // Upper bound in coordinates
                        var dy = (y2 - y1) / subdiv;
                        for (int j = 0; j < _pixelWidth; j++)
                        {
                            var tmpY = y1;
                            var x1 = xl + (xr - xl) * j / _pixelWidth; // Left bound in coordinates
                            var x2 = xl + (xr - xl) * (j + 1) / _pixelWidth; // Right bound in coordinates
                            var dx = (x2 - x1) / subdiv;
                            double sum = 0;

                            // Sum up the results for subdiv * subdiv grid of points inside the given pixel (row, j)
                            for (int d1 = 0; d1 < subdiv; d1++)
                            {
                                var tmpX = x1;
                                for (var d2 = 0; d2 < subdiv; d2++)
                                {
                                    sum += ms[tmpX, tmpY];
                                    tmpX += dx;
                                }

                                tmpY += dy;
                            }

                            // Map from 0...maxiter to 0...255 with given mapping function, which maps the intermediate 0...1 value
                            // Essentially changing contrast/gamma, except it doesn't have to be exponential function
                            var brightness = (byte) Math.Round(255 * _mappingFunc(sum / subdiv / subdiv / maxiter));
                            pix[4 * j] = 0x00;
                            pix[4 * j + 1] =
                                brightness == 0xFF
                                    ? (byte) 0 // 0xFF would be if we didn't escape yet, so paint it black
                                    : brightness; 
                            pix[4 * j + 2] = 0x00;
                            pix[4 * j + 3] = 0xFF;
                        }
                        //  Assign evaluated row
                        
                        pixels[row] = pix;

                        // Take next row

                        haveNextRow = rowDispencer.TryDequeue(out row);
                    }
                    // There are no rows left to render, or something went wrong
                });

            // Wait until all rows are finished
            foreach (var t in tasks)
                t.Wait();

            // Flatten the two-dimensional byte array to one-dimensional
            return pixels.SelectMany(x => x).ToArray();
        }

        public void SetMappingFunc(MappingFuncsEnum func)
        {
            _mappingFunc = MappingFuncs[(int) func];
        }
    }
}