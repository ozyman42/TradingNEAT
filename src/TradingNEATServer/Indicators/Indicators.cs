using System;
using System.Collections.Generic;

namespace TradingNEATServer
{
    public class Indicators
    {
        private static Indicators instance;
        public enum INDICATOR_TYPE { MA, EMA, CCI, MACD, High, Low, RSI, SRSI, BAS };
        public static readonly IReadOnlyList<string> INDICATOR_NAMES = new List<string>
        {
            "Moving Average",
            "Exponential Moving Average",
            "Commodity Channel Index",
            "Moving Average Convergence Divergence",
            "Price High",
            "Price Low",
            "Relative Strength Index",
            "Stochastic Relative Strength Index",
            "Bid Ask Spread"
        }.AsReadOnly();
        public static readonly IReadOnlyList<int> TIME_FRAMES = new List<int>
        {
            5, 7, 10, 15, 20,
            30, 40, 60, 80, 100, 150, 200, 250, 300, 400,
            470, 570, 650, 800, 1000, 1250, 1500, 1750,
            2000, 2500, 3000, 3500, 4000
        }.AsReadOnly();
        // TODO: add support for sell wall volume indicator
        // 0to1, 1to3, 3to5, 5to7, 7to12, 12to24, 24to35, 35to50, 50to75, 75to100
        // 100to150, 150to250, 250to500, over500
        // TODO: add support for distance to fib retracement indicator.
        // TODO: add support for distance to elliot wave indicator.
        public static Indicators Instance {
            get
            {
                if (instance == null) instance = new Indicators();
                return instance;
            }
        }

        private Dictionary<string, string> dbColumns;
        private object dbColumnsInitializingLock = new object();

        private Indicators()
        {
            
        }

        public Dictionary<string, string> DBColumns
        {
            get
            {
                lock(this.dbColumnsInitializingLock) // Prevents double computations when mutiple threads request these columns
                {
                    if (this.dbColumns == null)
                    {
                        this.dbColumns = new Dictionary<string, string>();
                        for (int indicator = (int)INDICATOR_TYPE.MA; indicator <= (int)INDICATOR_TYPE.BAS; ++indicator)
                        {
                            string indicatorColumnPrefix = INDICATOR_NAMES[indicator].ToLower().Replace(" ", "_");
                            for (int time_frame_index = 0; time_frame_index < TIME_FRAMES.Count; ++time_frame_index)
                            {
                                for (int derivative = 0; derivative <= 2; ++derivative)
                                {
                                    dbColumns[$"{indicatorColumnPrefix}_{TIME_FRAMES[time_frame_index]}frame_{derivative}deriv"] = "REAL";
                                }
                            }
                        }
                    }
                }
                return new Dictionary<string, string>(this.dbColumns);
            }
        }
    }
}