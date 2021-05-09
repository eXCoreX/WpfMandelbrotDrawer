namespace WpfMandelbrotDrawer.Models
{
    /// <summary>
    ///     Indexer returns min(MaxIterations, Number of steps to bailout from [x, y])
    ///     Uses double as type
    /// </summary>
    public class MandelbrotSet
    {
        public MandelbrotSet(int maxIterations)
        {
            MaxIterations = maxIterations;
        }

        public int MaxIterations { get; }

        /// <summary>
        ///     Get number of iterateration to bailout from [x,y], or MaxIterations if no escape is reached
        /// </summary>
        /// <param name="x">x-coordinate, or real part</param>
        /// <param name="y">y-coordinate, or imaginary part</param>
        /// <returns> min(MaxIteration, iterations to bailout)</returns>
        public int this[double x, double y]
        {
            get
            {
                if (CardioidTest(x, y) || BulbTest(x, y)) return MaxIterations;
                var it = 0;
                double cx2 = default, cy2 = default;
                double cx = default, cy = default;

                while (cx2 + cy2 < 4 && it < MaxIterations)
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
            var x_14 = x - 1 / 4.0;
            var q = x_14 * x_14 + y * y;
            return q * (q + x_14) < 1 / 4.0 * y * y;
        }

        private static bool BulbTest(double x, double y)
        {
            return (x + 1) * (x + 1) + y * y <= 1 / 16.0;
        }
    }
}