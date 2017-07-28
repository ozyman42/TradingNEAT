using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradingNEAT
{
    class MovingAverageTracker
    {
        private int range;
        private double trackingSum;
        private Queue<double> stomach;
        public MovingAverageTracker(int range)
        {
            this.range = range;
            this.stomach = new Queue<double>();
        }

        public double MovingAverage
        {
            get { return trackingSum / this.stomach.Count; }
        }

        public void feedNextValue(double nextValue)
        {
            if (this.stomach.Count == range) this.trackingSum -= this.stomach.Dequeue();
            this.trackingSum += nextValue;
            this.stomach.Enqueue(nextValue);
            if (this.stomach.Count > range) throw new Exception("Stomach size is larger than range");
        }

        public double calcStdDev()
        {
            double currentMovingAverage = this.MovingAverage;
            double absoluteDeviationSum = 0.0;
            foreach(double value in this.stomach)
            {
                absoluteDeviationSum += Math.Abs(value - currentMovingAverage);
            }
            return absoluteDeviationSum / this.stomach.Count;
        }
    }
}
