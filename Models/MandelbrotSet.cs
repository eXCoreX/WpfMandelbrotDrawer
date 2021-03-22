using System.Numerics;

namespace WpfMandelbrotDrawer.Models
{
    /// <summary>
    /// Indexer returns min(MaxIterations, Number of steps to bailout from [x, y])
    /// Uses double as type
    /// </summary>
    public class MandelbrotSet
    {
        private readonly int _MaxIterations;
        public int MaxIterations
        {
            get
            {
                return _MaxIterations;
            }
        }

        public MandelbrotSet(int maxIterations)
        {
            _MaxIterations = maxIterations;
        }

        public int this[double x, double y]
        {
            get
            {
                if (CardioidTest(x, y) || BulbTest(x, y))
                {
                    return _MaxIterations;
                }
                int it = 0;
                double cx2 = default, cy2 = default;
                double cx = default, cy = default;

                while ((cx2 + cy2 < 4) && it < _MaxIterations)
                {
                    cy = 2 * cx * cy + y;
                    cx = cx2 - cy2 + x;
                    cx2 = cx * cx;
                    cy2 = cy * cy;
                    it++;
                }
                return it;
            }
        }

        private static bool CardioidTest(double x, double y)
        {
            double x_14 = x - 1 / 4.0;
            double q = x_14 * x_14 + y * y;
            return (q * (q + x_14)) < (1 / 4.0 * y * y);
        }

        private static bool BulbTest(double x, double y)
        {
            return ((x + 1) * (x + 1) + y * y) <= (1 / 16.0);
        }
    }
}
