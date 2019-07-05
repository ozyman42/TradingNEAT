using System;
using SharpNeat.Core;
using SharpNeat.Phenomes;

namespace TradingNEAT
{
    class TradingEvaluator : IPhenomeEvaluator<IBlackBox>
    {
        public static TradingData CURRENT_DATA_SET;
        public static double MeanFitnessOfGeneration = 0.0;
        public static double BestFitnessOfGeneration = 0.0;
        public static double BestFitnessOfGenerationGainPercent = 0.0;
        public static int BestFitnessOfGenerationNumberTrades = 0;
        public static double WorstFitnessOfGeneration = 1.0;
        private static int totalRun = 0;

        private static Object staticFieldsLock = new Object();

        public static void InitializeGenerationalDataSet(int timestepRange)
        {
            totalRun = 0;
            MeanFitnessOfGeneration = 0.0;
            BestFitnessOfGenerationGainPercent = 0.0;
            BestFitnessOfGeneration = 0.0;
            BestFitnessOfGenerationNumberTrades = 0;
            WorstFitnessOfGeneration = 1.0;
            //if(CURRENT_DATA_SET == null)
            //{
                CURRENT_DATA_SET = TradingData.fetchRandomSeries(timestepRange);
            //}
        }

        const double StopFitness = 1000.0;
        ulong _evalCount;
        bool _stopConditionSatisfied;

        #region IPhenomeEvaluator<IBlackBox> Members

        /// <summary>
        /// Gets the total number of evaluations that have been performed.
        /// </summary>
        public ulong EvaluationCount
        {
            get { return _evalCount; }
        }

        /// <summary>
        /// Gets a value indicating whether some goal fitness has been achieved and that
        /// the evolutionary algorithm/search should stop. This property's value can remain false
        /// to allow the algorithm to run indefinitely.
        /// </summary>
        public bool StopConditionSatisfied
        {
            get { return _stopConditionSatisfied; }
        }

        /// <summary>
        /// Evaluate the provided IBlackBox.
        /// </summary>
        public FitnessInfo Evaluate(IBlackBox box)
        {
            //Console.WriteLine("Evaluate being called");
            double startingOutBalance = 1000.00;
            double outBalance = startingOutBalance;
            double inBalance = 0.0;
            TradingData market = CURRENT_DATA_SET.clone();

            if(!market.hasNextPrice())
            {
                throw new Exception("No market data in the CURRENT_DATA_SET");
            }

            TimeStepDataPiece currentData;
            int index = 0;
            int numberOfTrades = 0;
            do
            {
                currentData = market.getNextData();

                // Provide state info to the black box inputs.
                // Unless I'm mistaken, this library will already provide a 1.0 as the first input to the black box at all times.
                for(int i = 0; i < currentData.ccis.Count; ++i)
                {
                    box.InputSignalArray[i] = currentData.ccis[i];
                }

                // Activate the network.
                box.Activate();

                // Read the network's trading recommendation signal output.
                bool allIn = (box.OutputSignalArray[0] - 0.5) > 0.0;
                //Console.WriteLine("output = " + box.OutputSignalArray[0]);
                // Take the action being recommended by the network.
                if (allIn && inBalance == 0.0) {
                    inBalance = outBalance / currentData.price;
                    outBalance = 0.0;
                    numberOfTrades++;
                } else if (!allIn && outBalance == 0.0) {
                    outBalance = inBalance * currentData.price;
                    inBalance = 0.0;
                    numberOfTrades++;
                }
                ++index;
            } while (market.hasNextPrice());
            market.resetPricePointer();
            _evalCount++;

            double effectiveOutValue = outBalance > 0.0 ? outBalance : inBalance * currentData.price;
            double percentChange = (effectiveOutValue - startingOutBalance) / startingOutBalance;

            // Fitness function 1. Award profit.
            //double fitness = (percentChange + 1.0) / 2.0;

            // Fitness function 2. Award approaching of maximum gainFactor.
            double gainFactor = effectiveOutValue / startingOutBalance;
            double fitness = gainFactor / market.MaximumGainFactor;

            // Fitness function 3. Award approaching of maximum gainFactor, Punish approaching of maximum lossFactor.
            //double gainFactor = percentChange > 0 ? effectiveOutValue / startingOutBalance : 0.0;
            //double lossFactor = percentChange < 0 ? startingOutBalance / effectiveOutValue : 0.0;
            //double fitness = (gainFactor - 1) / market.MaximumGainFactor - (lossFactor - 1) / market.MaximumLossFactor;
            //fitness = (fitness + 1) / 2.0;
            lock(staticFieldsLock)
            {
                if (fitness > BestFitnessOfGeneration)
                {
                    BestFitnessOfGeneration = fitness;
                    BestFitnessOfGenerationGainPercent = percentChange;
                    BestFitnessOfGenerationNumberTrades = numberOfTrades;
                }
                if (fitness < WorstFitnessOfGeneration)
                {
                    WorstFitnessOfGeneration = fitness;
                }
                MeanFitnessOfGeneration *= totalRun;
                MeanFitnessOfGeneration += fitness;
                MeanFitnessOfGeneration /= (double)++totalRun;
            }
            return new FitnessInfo(fitness, fitness);
        }

        /// <summary>
        /// Reset the internal state of the evaluation scheme if any exists.
        /// </summary>
        public void Reset()
        {
            Console.WriteLine("about to reset");
        }

        #endregion

    }
}
