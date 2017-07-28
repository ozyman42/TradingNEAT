using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.IO;

namespace TradingNEATServer
{
    class TradingData
    {
        private static TimeStepDataPiece[] allData;

        private int currentIndex;
        private TimeStepDataPiece[] data;
        private readonly double maximumGainFactor;
        private readonly double maximumLossFactor;

        public TradingData(TimeStepDataPiece[] inputData)
        {
            this.currentIndex = 0;
            this.data = inputData;
            this.maximumGainFactor = 1.0;
            this.maximumLossFactor = 1.0;
            double prevPrice = data[0].price;
            while (this.hasNextPrice())
            {
                double currPrice = this.getNextData().price;
                if (currPrice > prevPrice)
                {
                    this.maximumGainFactor *= currPrice / prevPrice;
                } else if (currPrice < prevPrice)
                {
                    this.maximumLossFactor *= prevPrice / currPrice;
                }
                prevPrice = currPrice;
            }
            this.resetPricePointer();
        }

        private TradingData(TimeStepDataPiece[] inputData, double maximumGainFactor, double maximumLossFactor)
        {
            this.currentIndex = 0;
            this.data = inputData;
            this.maximumGainFactor = maximumGainFactor;
            this.maximumLossFactor = maximumLossFactor;
        }

        public double MaximumGainFactor
        {
            get { return this.maximumGainFactor; }
        }

        public double MaximumLossFactor
        {
            get { return this.maximumLossFactor; }
        }

        public TradingData clone()
        {
            return new TradingData((TimeStepDataPiece[])data.Clone(), this.maximumGainFactor, this.maximumLossFactor);
        }

        public bool hasNextPrice()
        {
            return currentIndex <= data.Length - 1;
        }

        public TimeStepDataPiece getNextData()
        {
            this.currentIndex++;
            return data[currentIndex - 1];
        }

        public void resetPricePointer()
        {
            this.currentIndex = 0;
        }

        public static TradingData fetchRandomSeries(int timestepRange)
        {
            TradingData series;
            do
            {
                Random rnd = new Random();
                int startIndex = rnd.Next(0, allData.Length - timestepRange + 1);
                TimeStepDataPiece[] randomSet = new TimeStepDataPiece[timestepRange];
                for (int i = 0; i < timestepRange; ++i)
                {
                    randomSet[i] = allData[startIndex + i];
                }
                series = new TradingData(randomSet);
            } while (series.MaximumGainFactor < 1.3 || series.MaximumLossFactor < 1.6);
            return series;
        }

        public static void initializeData()
        {
            Console.Write("Loading data set... ");
            // A piece of data looks like the following.
            // {"date":1482565500,"high":7.22192054,"low":7.22192054,"open":7.22192054,"close":7.22192054,"volume":0,"quoteVolume":0,"weightedAverage":7.22192054}
            JArray dataArray = JArray.Parse(File.ReadAllText("../../eth_esdt_poloniex.json"));
            allData = new TimeStepDataPiece[dataArray.Count];
            int[] cciRanges = new int[]{2, 5, 10, 20, 50, 100, 200, 500, 1000 };
            MovingAverageTracker[] movingAverageTrackers = new MovingAverageTracker[cciRanges.Length];
            int index = 0;
            foreach (int cciRange in cciRanges)
            {
                movingAverageTrackers[index++] = new MovingAverageTracker(cciRange);
            }
            index = 0;
            foreach (JObject o in dataArray.Children<JObject>())
            {
                double high = -1.0;
                double low = -1.0;
                double close = -1.0;
                double weightedAverage = -1.0;
                foreach (JProperty p in o.Properties())
                {
                    string name = p.Name;
                    string value = (string)p.Value;
                    switch(name)
                    {
                        case "high":
                            high = Double.Parse(value);
                            break;
                        case "low":
                            low = Double.Parse(value);
                            break;
                        case "close":
                            close = Double.Parse(value);
                            break;
                        case "weightedAverage":
                            weightedAverage = Double.Parse(value);
                            break;
                    }
                }
                if(high < 0.0 || low < 0.0 || close < 0.0 || weightedAverage < 0.0) throw new Exception("One of the following properties: {\"high\", \"low\", \"close\". \"weightedAverage\"} was not found in the current piece of JSON data.\n " + o.ToString());
                double typicalPrice = (high + low + close) / 3.0;
                double[] ccis = new double[cciRanges.Length];
                int cciIndex = 0;
                foreach(MovingAverageTracker mat in movingAverageTrackers)
                {
                    mat.feedNextValue(typicalPrice);
                    // CCI calculation. Divided by 1.5 rather than .0015 so that the majority range from -1 to 1 rahter than -100 to 100.
                    ccis[cciIndex++] = (typicalPrice - mat.MovingAverage) / (mat.calcStdDev() * 1.5);
                }
                allData[index++] = new TimeStepDataPiece(typicalPrice, ccis, cciRanges);
            }
            Console.WriteLine("Loaded.");
        }

    }
}
