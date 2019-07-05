using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

using log4net.Config;
using SharpNeat.Core;
using SharpNeat.EvolutionAlgorithms;
using SharpNeat.Genomes.Neat;
using SharpNeat.Genomes.HyperNeat;

namespace TradingNEATServer
{
    public class TrainingSession
    {
        private static TrainingSession instance;

        public static TrainingSession Instance
        {
            get
            {
                if(instance == null) instance = new TrainingSession();
                return instance;
            }
        }

        private IGenomeFactory<NeatGenome> _genomeFactory;
        private List<NeatGenome> _genomeList;
        private NeatEvolutionAlgorithm<NeatGenome> _ea;
        private TradingExperiment experiment;

        private TrainingSession()
        {
            // Initialise log4net (log to console).
            XmlConfigurator.Configure(new FileInfo("log4net.properties"));

            // Experiment classes encapsulate much of the nuts and bolts of setting up a NEAT search.
            this.experiment = new TradingExperiment();

            // Initialize the data to be used.
            TradingData.initializeData();

            // Load config XML.
            XmlDocument xmlConfig = new XmlDocument();
            xmlConfig.Load(StorageLayer.FILE_IO_PATH + "trading.config.xml");
            this.experiment.Initialize("Trading", xmlConfig.DocumentElement);
        }

        # region Getters

        public bool Started
        {
            get { return this._ea != null; }
        }

        public bool Running
        {
            get { return this.Started && this._ea.RunState == RunState.Running; }
        }

        public bool PopulationLoaded
        {
            get { return this._genomeList != null; }
        }

        # endregion

        public string loadRandomPopulation(int populationSize)
        {
            if (this.Running) throw new Exception("Cannot load a population into a session while it is running.");
            // Create a genome factory with our neat genome parameters object and the appropriate number of input and output neuron genes.
            this._genomeFactory = this.experiment.CreateGenomeFactory();

            // Create an initial population of randomly generated genomes.
            this._genomeList = _genomeFactory.CreateGenomeList(populationSize, 0);
            return $"Created [{populationSize}] random genomes.";
        }

        public string loadPopulationFromFile(string filename)
        {
            if (this.Running) throw new Exception("Cannot load a population into a session while it is running.");
            // Open and load population XML file.
            using (XmlReader xr = XmlReader.Create(filename))
            {
                this._genomeList = experiment.LoadPopulation(xr);
            }
            this._genomeFactory = this._genomeList[0].GenomeFactory;
            return $"Loaded [{this._genomeList.Count}] genomes.";
        }

        public string loadPopulationFromSeed(string filename, int populationSize)
        {
            if (this.Running) throw new Exception("Cannot load a population into a session while it is running.");
            
            // Open and load genome XML file.
            using (XmlReader xr = XmlReader.Create(filename))
            {
                this._genomeList = this.experiment.LoadPopulation(xr);
            }

            if (this._genomeList.Count == 0)
            {
                this._genomeList = null;
                throw new Exception($"No genome loaded from file [{filename}]");
            }

            // Create genome list from seed.
            this._genomeFactory = this._genomeList[0].GenomeFactory;
            this._genomeList = this._genomeFactory.CreateGenomeList(populationSize, 0u, this._genomeList[0]);
            return $"Created [{this._genomeList.Count}] genomes from loaded seed genome.";
        }

        public string startTraining()
        {
            if (this.Running) throw new Exception("Cannot start training session that is already running");
            string response = "";
            if (!this.Started)
            {   // Create new evolution algorithm.
                if (!this.PopulationLoaded) throw new Exception("Attempting to start training session without having loaded a population.");
                this.ea_NextGenerationEvent();
                this._ea = experiment.CreateEvolutionAlgorithm(this._genomeFactory, this._genomeList);
                this._ea.NextGenerationEvent += new EventHandler(this.ea_NextGenerationEvent);
                this._ea.GenerationFinishedEvent += new EventHandler(this.ea_GenerationFinishedEvent);
                this._ea.UpdateEvent += new EventHandler(this.ea_UpdateEvent);
            }
            else
            {
                response += "No new EA to create. ";
            }
            this._ea.StartContinue();
            response += "Started.";
            return response;
        }

        public string pause()
        {
            if (!this.Running) throw new Exception("Cannot pause training session which is not currently running.");
            this._ea.RequestPauseAndWait();
            return "Stopped.";
        }

        public string export()
        {
            throw new Exception("Method not implemented");
            /*
            if (!this.started) throw new Exception("Cannot export from a session which is either reset or has never run.");
            if (this.running) throw new Exception("Cannot export from a session which is currently running.");
            // Save pop
            if (null != _ea && _ea.RunState == RunState.Running)
            {
                Console.WriteLine("Error. Cannot save population while algorithm is running.");
                break;
            }
            if (null == _genomeList)
            {
                Console.WriteLine("Error. No population to save.");
                break;
            }

            // Attempt to get population filename arg.
            if (cmdArgs.Length <= 1)
            {
                Console.WriteLine("Error. Missing {filename} argument.");
                break;
            }

            // Save genomes to xml file.
            XmlWriterSettings xwSettings = new XmlWriterSettings();
            xwSettings.Indent = true;
            using (XmlWriter xw = XmlWriter.Create(cmdArgs[1], xwSettings))
            {
                experiment.SavePopulation(xw, _genomeList);
            }
            Console.WriteLine($"[{_genomeList.Count}] genomes saved to file [{cmdArgs[1]}]");
            // Save Best
            if (null != _ea && _ea.RunState == RunState.Running)
            {
                Console.WriteLine("Error. Cannot save population while algorithm is running.");
                break;
            }
            if (null == _ea || null == _ea.CurrentChampGenome)
            {
                Console.WriteLine("Error. No best genome to save.");
                break;
            }

            // Attempt to get genome filename arg.
            if (cmdArgs.Length <= 1)
            {
                Console.WriteLine("Error. Missing {filename} argument.");
                break;
            }

            // Save genome to xml file.
            XmlWriterSettings xwSettings = new XmlWriterSettings();
            xwSettings.Indent = true;
            using (XmlWriter xw = XmlWriter.Create(cmdArgs[1], xwSettings))
            {
                experiment.SavePopulation(xw, new NeatGenome[] { _ea.CurrentChampGenome });
            }

            Console.WriteLine($"Best genome saved to file [{cmdArgs[1]}]");
            break;
            */
        }

        public string reset()
        {
            if (this.Running) throw new Exception("Cannot reset session while it is running.");
            this._ea = null;
            this._genomeFactory = null;
            this._genomeList = null;
            return "Reset completed.";
        }

        # region EventListeners

        private void ea_UpdateEvent(object sender, EventArgs e)
        {
            //Console.WriteLine(string.Format("gen={0:N0} bestFitness={1:N6}", this._ea.CurrentGeneration, this._ea.Statistics._maxFitness));
        }

        private void ea_NextGenerationEvent(object sender, EventArgs e)
        {
            this.ea_NextGenerationEvent();
        }

        private void ea_NextGenerationEvent()
        {
            // TODO: add this constant to configuration XML, or move all the configuration xml to constants.
            TradingEvaluator.InitializeGenerationalDataSet(2000);
        }

        private void ea_GenerationFinishedEvent(object sender, EventArgs e)
        {
            int currentGeneration = (int)this._ea.CurrentGeneration - 1;
            if (currentGeneration == 0) Console.WriteLine();
            string genString = string.Format("{0:N0}", currentGeneration);
            Console.Write(string.Format("{0,6}. maxGain={1:000.0000} maxLoss={2:000.0000} genWorstFitness={3:00.0000} ", genString, TradingEvaluator.CURRENT_DATA_SET.MaximumGainFactor, TradingEvaluator.CURRENT_DATA_SET.MaximumLossFactor, TradingEvaluator.WorstFitnessOfGeneration));
            Console.WriteLine(string.Format("genMeanFitness={0:00.0000} genBestFitness={1:00.0000} genBestGains={2:+00.0000;-00.0000} genBestTrades={3,4} eaBestFitness={4:00.0000}", TradingEvaluator.MeanFitnessOfGeneration, TradingEvaluator.BestFitnessOfGeneration, TradingEvaluator.BestFitnessOfGenerationGainPercent, TradingEvaluator.BestFitnessOfGenerationNumberTrades, this._ea.Statistics._maxFitness));
            TrainingWebSocketHandler.HandleGenerationCompletion();
        }

        # endregion

    }
}