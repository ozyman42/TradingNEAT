using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradingNEAT
{
    class TimeStepDataPiece
    {
        public readonly double price;
        // Various commodoties channel index values at different timestep lengths.
        // Between -1 to 1 rather than -100 to 100.
        public readonly ReadOnlyCollection<double> ccis;
        public readonly ReadOnlyCollection<int> cciTimestepLengths;

        public TimeStepDataPiece(double p, double[] ccis, int[] cciTimestepLengths) {
            this.price = p;
            this.ccis = Array.AsReadOnly<double>(ccis);
            this.cciTimestepLengths = Array.AsReadOnly<int>(cciTimestepLengths);
        }


    }
}
