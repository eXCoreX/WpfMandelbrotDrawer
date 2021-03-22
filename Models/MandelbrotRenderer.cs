using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace WpfMandelbrotDrawer.Models
{
    public class MandelbrotRenderer
    {
        private readonly int pixelWidth;
        private readonly int pixelHeight;

        public enum MappingFuncsEnum
        {
            Squareroot = 0,
            Log,
            Linear,
            Square
        }

        private static readonly Func<double, double>[] mappingFuncs = new Func<double, double>[]
        {
            x => Math.Sqrt(x),
            x => (Math.Log(x + 1 / Math.E) + 1) / 1.31326168752, // Modified natural log which returns 0 for x = 0 and 1 for x = 1
            x => x,
            x => x * x
        };

        private Func<double, double> mappingFunc = x => x;

        public byte[] RenderSet(double xl, double xr, double yd, double yu, int subdiv, int maxiter)
        {
            var ms = new MandelbrotSet(maxiter);
            byte[][] pixels = new byte[pixelHeight][];
            object _pixels_lock = new object();

            ConcurrentQueue<int> rowDispencer = new ConcurrentQueue<int>(Enumerable.Range(0, pixelHeight));

            var tasks = new Task[Environment.ProcessorCount];
            for (int i = 0; i < tasks.Length; i++)
            {
                // These tasks manage themselves, grabbing rows that need rendering at their own demand
                // This way threads are fully loaded and not waiting for others to finish until the last few rows (at most tasks.Length - 1)
                tasks[i] = Task.Run(() =>
                {
                    bool flag = rowDispencer.TryDequeue(out int row);
                    if (!flag)
                    {
                        // There are no rows left to render, or something went wrong
                        return;
                    }
                beginning:
                    // Debug.WriteLine($"Starting row {row}, threadID = {Thread.CurrentThread.ManagedThreadId}");
                    byte[] pix = new byte[pixelWidth * 4];
                    double y1 = yd + (yu - yd) * (row) / pixelHeight;     // Lower bound in coordinates
                    double y2 = yd + (yu - yd) * (row + 1) / pixelHeight; // Upper bound in coordinates
                    double dy = (y2 - y1) / subdiv;
                    for (int j = 0; j < pixelWidth; j++)
                    {
                        double tmp_y = y1;
                        double x1 = xl + (xr - xl) * (j) / pixelWidth;    // Left bound in coordinates
                        double x2 = xl + (xr - xl) * (j + 1) / pixelWidth;// Right bound in coordinates
                        double dx = (x2 - x1) / subdiv;
                        double sum = 0;

                        // Sum up the results for subdiv * subdiv grid of points inside the given pixel (row, j)
                        for (int d1 = 0; d1 < subdiv; d1++)
                        {
                            double tmp_x = x1;
                            for (int d2 = 0; d2 < subdiv; d2++)
                            {
                                sum += ms[tmp_x, tmp_y];
                                tmp_x += dx;
                            }
                            tmp_y += dy;
                        }

                        // Map from 0...maxiter to 0...255 with given mapping function, which maps the intermediate 0...1 value
                        // Essentially changing gamma, except it doesn't have to be exponential function
                        byte hue = (byte)Math.Round(255 * mappingFunc((sum / subdiv / subdiv) / maxiter));
                        pix[4 * j] = 0x00;
                        pix[4 * j + 1] = hue == 0xFF ? (byte)0 : hue; // 0xFF would be if we didn't escape yet, so paint it black
                        pix[4 * j + 2] = 0x00;
                        pix[4 * j + 3] = 0xFF;
                    }
                    //  Assign evaluated row

                    lock (_pixels_lock)
                    {
                        pixels[row] = pix;
                    }

                    // Take next row

                    bool flag2 = rowDispencer.TryDequeue(out int nextRow);

                    if (flag2)
                    {
                        row = nextRow;
                        goto beginning;
                    }
                    // There are no rows left to render, or something went wrong
                    return;
                });
            }
            for (int i = 0; i < tasks.Length; i++)
            {
                tasks[i].Wait();
            }

            // Flatten the two-dimensional byte array to one-dimensional
            return pixels.SelectMany(x => x).ToArray();
        }

        public void SetMappingFunc(MappingFuncsEnum func)
        {
            mappingFunc = mappingFuncs[(int)func];
        }

        public MandelbrotRenderer(int pixelWidth, int pixelHeight)
        {
            this.pixelWidth = pixelWidth;
            this.pixelHeight = pixelHeight;
        }
    }
}
