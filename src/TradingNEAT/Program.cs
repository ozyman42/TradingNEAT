using System;
using System.Collections.Generic;
using System.Xml;
using System.IO;
using System.Net.Sockets;
using System.Net;
using System.Security.Cryptography;
using System.Threading;

using log4net.Config;
using SharpNeat.Core;
using SharpNeat.EvolutionAlgorithms;
using SharpNeat.Genomes.Neat;
using SharpNeat.Genomes.HyperNeat;

namespace TradingNEAT
{
    class Program
    {

        static IGenomeFactory<NeatGenome> _genomeFactory;
        static List<NeatGenome> _genomeList;
        static NeatEvolutionAlgorithm<NeatGenome> _ea;

        static void Main(string[] args)
        {

            // Initialise log4net (log to console).
            XmlConfigurator.Configure(new FileInfo("log4net.properties"));

            // Experiment classes encapsulate much of the nuts and bolts of setting up a NEAT search.
            TraderExperiment experiment = new TraderExperiment();

            // Initialize the data to be used.
            TradingData.initializeData();

            // Load config XML.
            XmlDocument xmlConfig = new XmlDocument();
            xmlConfig.Load("../../trading.config.xml");
            experiment.Initialize("Trading", xmlConfig.DocumentElement);

            //WriteHelp();
            // Read key commands from the console.
            for (;;)
            {
                // Read command.
                Console.Write(">");
                string cmdstring = Console.ReadLine();

                // Parse command.
                string[] cmdArgs = cmdstring.Split(' ');

                try
                {
                    // Process command.
                    switch (cmdArgs[0])
                    {
                        // Init commands.
                        case "randpop":
                            {
                                if (null != _ea && _ea.RunState == RunState.Running)
                                {
                                    Console.WriteLine("Error. Cannot create population while algorithm is running.");
                                    break;
                                }

                                // Attempt to parse population size arg.
                                if (cmdArgs.Length <= 1)
                                {
                                    Console.WriteLine("Error. Missing [size] argument.");
                                    break;
                                }

                                int populationSize;
                                if (!int.TryParse(cmdArgs[1], out populationSize))
                                {
                                    Console.WriteLine($"Error. Invalid [size] argument [{cmdArgs[1]}].");
                                    break;
                                }

                                // Create a genome factory with our neat genome parameters object and the appropriate number of input and output neuron genes.
                                _genomeFactory = experiment.CreateGenomeFactory();

                                // Create an initial population of randomly generated genomes.
                                _genomeList = _genomeFactory.CreateGenomeList(populationSize, 0);
                                Console.WriteLine($"Created [{populationSize}] random genomes.");
                                break;
                            }
                        case "loadpop":
                            {
                                if (null != _ea && _ea.RunState == RunState.Running)
                                {
                                    Console.WriteLine("Error. Cannot load population while algorithm is running.");
                                    break;
                                }

                                // Attempt to get population filename arg.
                                if (cmdArgs.Length <= 1)
                                {
                                    Console.WriteLine("Error. Missing {filename} argument.");
                                    break;
                                }

                                // Open and load population XML file.
                                using (XmlReader xr = XmlReader.Create(cmdArgs[1]))
                                {
                                    _genomeList = experiment.LoadPopulation(xr);
                                }
                                _genomeFactory = _genomeList[0].GenomeFactory;
                                Console.WriteLine($"Loaded [{_genomeList.Count}] genomes.");
                                break;
                            }
                        case "loadseed":
                            {
                                if (null != _ea && _ea.RunState == RunState.Running)
                                {
                                    Console.WriteLine("Error. Cannot load population while algorithm is running.");
                                    break;
                                }

                                // Attempt to get genome filename arg.
                                if (cmdArgs.Length <= 1)
                                {
                                    Console.WriteLine("Error. Missing {filename} argument.");
                                    break;
                                }

                                // Attempt to parse population size arg.
                                if (cmdArgs.Length <= 2)
                                {
                                    Console.WriteLine("Error. Missing [size] argument.");
                                    break;
                                }

                                int populationSize;
                                if (!int.TryParse(cmdArgs[2], out populationSize))
                                {
                                    Console.WriteLine($"Error. Invalid [size] argument [{cmdArgs[2]}].");
                                    break;
                                }

                                // Open and load genome XML file.
                                using (XmlReader xr = XmlReader.Create(cmdArgs[1]))
                                {
                                    _genomeList = experiment.LoadPopulation(xr);
                                }

                                if (_genomeList.Count == 0)
                                {
                                    Console.WriteLine($"No genome loaded from file [{cmdArgs[1]}]");
                                    _genomeList = null;
                                    break; ;
                                }

                                // Create genome list from seed.
                                _genomeFactory = _genomeList[0].GenomeFactory;
                                _genomeList = _genomeFactory.CreateGenomeList(populationSize, 0u, _genomeList[0]);
                                Console.WriteLine($"Created [{_genomeList.Count}] genomes from loaded seed genome.");
                                break;
                            }

                        // Execution control commands.
                        case "start":
                            {
                                if (null == _ea)
                                {   // Create new evolution algorithm.
                                    if (null == _genomeList)
                                    {
                                        Console.WriteLine("Error. No loaded genomes");
                                        break;
                                    }
                                    ea_NextGenerationEvent();
                                    _ea = experiment.CreateEvolutionAlgorithm(_genomeFactory, _genomeList);
                                    _ea.NextGenerationEvent += new EventHandler(ea_NextGenerationEvent);
                                    _ea.GenerationFinishedEvent += new EventHandler(ea_GenerationFinishedEvent);
                                    _ea.UpdateEvent += new EventHandler(ea_UpdateEvent);
                                }
                                else
                                {
                                    Console.WriteLine("No new EA to create");
                                }

                                Console.WriteLine("Starting...");
                                _ea.StartContinue();
                                break;
                            }
                        case "stop":
                            {
                                Console.WriteLine("Stopping. Please wait...");
                                _ea.RequestPauseAndWait();
                                Console.WriteLine("Stopped.");
                                break;
                            }
                        case "reset":
                            {
                                if (null != _ea && _ea.RunState == RunState.Running)
                                {
                                    Console.WriteLine("Error. Cannot reset while algorithm is running.");
                                    break;
                                }

                                _ea = null;
                                _genomeFactory = null;
                                _genomeList = null;
                                Console.WriteLine("Reset completed.");
                                break;
                            }

                        // Genome saving commands.
                        case "savepop":
                            {
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
                                break;
                            }
                        case "savebest":
                            {
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
                            }

                        // Misc commands
                        case "help":
                            {
                                WriteHelp();
                                break;
                            }
                        case "quit":
                        case "exit":
                            {
                                Console.WriteLine("Stopping. Please wait...");
                                _ea.RequestPauseAndWait();
                                Console.WriteLine("Stopped.");
                                goto quit;
                            }
                        case "":
                            {
                                // Do nothing.
                                break;
                            }
                        default:
                            {
                                Console.WriteLine($"Unknown command [{cmdArgs[0]}]");
                                break;
                            }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Exception [{ex.Message}]");
                }
            }

            quit:
            Console.WriteLine("bye!");

        }

        static void WriteHelp()
        {
            Console.Write(
@"SharpNEAT 2.0 command line interface (CLI).

Commands...
Initialisation
    randpop {size}                  Create random population.
    loadpop {filename}              Load existing population from file.
    loadseed {filename} {popSize}   Load seed genome from file.

Execution control:
    start                   Start/Continue evolution algorithm.
    stop                    Stop/Pause evolution algorithm.
    reset                   Discard any current population and evolution algorithm.

Genome saving commands:
    savepop {filename}      Save population to XML file.
    savebest {filename}     Save best genome to XML file.

Misc
    help                    Show this help text.  
    exit, quit              Exit CLI.

");
        }

        static void ea_UpdateEvent(object sender, EventArgs e)
        {
            //Console.WriteLine(string.Format("gen={0:N0} bestFitness={1:N6}", _ea.CurrentGeneration, _ea.Statistics._maxFitness));
        }

        static void ea_NextGenerationEvent(object sender, EventArgs e)
        {
            ea_NextGenerationEvent();
        }

        static void ea_NextGenerationEvent()
        {
            // TODO: add this constant to configuration XML, or move all the configuration xml to constants.
            TradingEvaluator.InitializeGenerationalDataSet(2000);
        }

        static void ea_GenerationFinishedEvent(object sender, EventArgs e)
        {
            int currentGeneration = (int)_ea.CurrentGeneration - 1;
            if (currentGeneration == 0) Console.WriteLine();
            string genString = string.Format("{0:N0}", currentGeneration);
            Console.Write(string.Format("{0,6}. maxGain={1:000.0000} maxLoss={2:000.0000} genWorstFitness={3:00.0000} ", genString, TradingEvaluator.CURRENT_DATA_SET.MaximumGainFactor, TradingEvaluator.CURRENT_DATA_SET.MaximumLossFactor, TradingEvaluator.WorstFitnessOfGeneration));
            Console.WriteLine(string.Format("genMeanFitness={0:00.0000} genBestFitness={1:00.0000} genBestGains={2:+00.0000;-00.0000} genBestTrades={3,4} eaBestFitness={4:00.0000}", TradingEvaluator.MeanFitnessOfGeneration, TradingEvaluator.BestFitnessOfGeneration, TradingEvaluator.BestFitnessOfGenerationGainPercent, TradingEvaluator.BestFitnessOfGenerationNumberTrades, _ea.Statistics._maxFitness));
        }
    }
}