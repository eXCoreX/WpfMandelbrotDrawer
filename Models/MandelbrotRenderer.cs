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
            x => (Math.Log(x + 1 / Math.E) + 1) / 1.31326168752,
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
                tasks[i] = Task.Run(() =>
                {
                    bool flag = rowDispencer.TryDequeue(out int row);
                    if (!flag)
                    {
                        return;
                    }
                beginning:
                    // Debug.WriteLine($"Starting row {row}, threadID = {Thread.CurrentThread.ManagedThreadId}");
                    byte[] pix = new byte[pixelWidth * 4];
                    double y1 = yd + (yu - yd) * (row) / pixelHeight;
                    double y2 = yd + (yu - yd) * (row + 1) / pixelHeight;
                    double dy = (y2 - y1) / subdiv;
                    for (int j = 0; j < pixelWidth; j++)
                    {
                        double tmp_y = y1;
                        double x1 = xl + (xr - xl) * (j) / pixelWidth;
                        double x2 = xl + (xr - xl) * (j + 1) / pixelWidth;
                        double dx = (x2 - x1) / subdiv;
                        double sum = 0;
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
                        byte hue = (byte)Math.Round(255 * mappingFunc((sum / subdiv / subdiv) / maxiter));
                        pix[4 * j] = 0x00;
                        pix[4 * j + 1] = hue == 0xFF ? (byte)0 : hue;
                        pix[4 * j + 2] = 0x00;
                        pix[4 * j + 3] = 0xFF;
                    }
                    //  Assign evaluated row

                    pixels[row] = pix;

                    // Take next row

                    bool flag2 = rowDispencer.TryDequeue(out int nextRow);

                    if (flag2)
                    {
                        row = nextRow;
                        goto beginning;
                    }
                });
            }
            for (int i = 0; i < tasks.Length; i++)
            {
                tasks[i].Wait();
            }

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
