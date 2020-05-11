// <copyright file="KohonenCardNeuralNetwork.cs" company="Scada International A/S">
// Copyright (c) Scada International A/S. All rights reserved.
// </copyright>

using System;
using Common.Models;
using KohonenCards.Models;
using Serilog;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace KohonenCards
{
    public class KohonenCardNeuralNetwork
    {
        private readonly ILogger _logger;
        private readonly List<Layer> _layers;
        private readonly int _kohonenCardWidth;
        private readonly int _kohonenCardHeight;
        private readonly double _learningRateConstA;
        private readonly double _learningRateConstB;
        private readonly double _initialNeighborhoodParameter;

        public IReadOnlyList<KohonenLayerNeuron> KohonenLayerNeurons =>
            _layers[1].Neurons.OfType<KohonenLayerNeuron>().ToList();

        public KohonenCardNeuralNetwork(
            int kohonenCardWidth,
            int kohonenCardHeight,
            double learningRateConstA,
            double learningRateConstB,
            double initialNeighborhoodParameter,
            ILogger logger)
        {
            _logger = logger;

            _kohonenCardWidth = kohonenCardWidth;
            _kohonenCardHeight = kohonenCardHeight;
            _learningRateConstA = learningRateConstA;
            _learningRateConstB = learningRateConstB;
            _initialNeighborhoodParameter = initialNeighborhoodParameter;

            _layers = new List<Layer>();
        }

        public object InputDataProcessed { get; set; }

        public Task<List<InputDataResult>> Learn(List<InputData> learnData)
        {
            _logger.Information("Started learning. Test data has {NumberOfRecords} records.", learnData.Count);

            var result = new List<InputDataResult>();
            var sw = new Stopwatch();
            sw.Start();

            // initialize weights
            _layers[1].Neurons.ForEach(n => learnData[0].Inputs.ForEach(n.Weights.Add));

            int dataSize = learnData.Count;

            for (int iteration = 0; iteration < learnData.Count; iteration++)
            {
                InputData inputData = learnData[iteration];

                // setting input values
                for (int i = 0; i < inputData.Inputs.Count; i++)
                {
                    _layers[0].Neurons[i].InputSignals[0].Value = inputData.Inputs[i];
                    _layers[0].Neurons[i].FeedForward();
                }

                double minDistance = double.MaxValue;
                int indexOfMinDistanceNeuron = 0;

                for (int i = 0; i < _layers[1].Neurons.Count; i++)
                {
                    double distanceToNeuron = _layers[1].Neurons[i].DistanceToWeightVector(inputData.Inputs);
                    if (distanceToNeuron < minDistance)
                    {
                        minDistance = distanceToNeuron;
                        indexOfMinDistanceNeuron = i;
                    }
                }

                KohonenLayerNeuron winner = _layers[1].Neurons[indexOfMinDistanceNeuron] as KohonenLayerNeuron;
                result.Add(new InputDataResult(inputData, winner));

                double neighborhoodRadius = NeighborhoodRadius(iteration, dataSize);
                double learningRate = LearningRate(iteration);

                foreach (KohonenLayerNeuron neuron in _layers[1].Neurons.Cast<KohonenLayerNeuron>())
                {
                    var neighborsWeightCoefficient = neuron == winner
                        ? 1
                        : NeighborsWeightCoefficient(neighborhoodRadius, winner, neuron);

                    for (int i = 0; i < neuron.Weights.Count; i++)
                    {
                        neuron.Weights[i] +=
                            learningRate * neighborsWeightCoefficient * (_layers[1].Neurons[i].Weights[0] - neuron.Weights[i]);
                    }
                }
            }

            _logger.Information("Learning finished in {TimeElapsed}.", sw.Elapsed);

            return Task.FromResult(result);
        }

        /// <summary>
        ///     Initializes two layers in neural network and neurons inside.
        /// </summary>
        /// <param name="dataDimension">Number of attributes each vector has. Affects number of neurons in first (distributive) layer.</param>
        public List<KohonenLayerNeuron> InitializeLayers(int dataDimension)
        {
            // initializing neurons in distributive (first) layer
            var firstLayer = new Layer();
            for (int i = 0; i < dataDimension; i++)
            {
                firstLayer.Neurons.Add(new DistributiveLayerNeuron());
            }

            // initializing neurons in Kohonen (second) layer
            var secondLayer = new Layer();
            int x = 1;
            int y = 1;
            for (int i = 0; i < _kohonenCardWidth * _kohonenCardHeight; i++)
            {
                var neuron = new KohonenLayerNeuron(x, y);

                // connecting current neuron with each neuron from first layer
                foreach (var neuronFromFirstLayer in firstLayer.Neurons)
                {
                    var signal = new Signal();
                    neuron.InputSignals.Add(signal);
                    neuronFromFirstLayer.OutputSignals.Add(signal);
                }

                ////neuron.InitializeRandomWeights(firstLayer.Neurons.Count);

                secondLayer.Neurons.Add(neuron);

                if (y == _kohonenCardWidth)
                {
                    y = 0;
                    x++;
                }

                y++;
            }

            _layers.Add(firstLayer);
            _layers.Add(secondLayer);

            _logger.Information(
                "Initialized 2 layers: distributive ({NeuronsInFirstLayer} neurons) and Kohonen ({NeuronsInKohonenLayer} neurons).",
                dataDimension,
                _kohonenCardHeight * _kohonenCardWidth);

            return _layers[1].Neurons.OfType<KohonenLayerNeuron>().ToList();
        }

        private double LearningRate(int iteration) =>
            _learningRateConstA / (_learningRateConstB + iteration);

        private double NeighborhoodRadius(int iteration, int totalIterations) =>
            _initialNeighborhoodParameter / (1 + (double)iteration / totalIterations);

        private double NeighborsWeightCoefficient(double neighborhoodParameter, KohonenLayerNeuron winner, KohonenLayerNeuron neighbor)
        {
            double result = Math.Exp(- Math.Pow(winner.DistanceToNeuron(neighbor), 2) / (2 * Math.Pow(neighborhoodParameter, 2)));
            return result > neighborhoodParameter
                ? 0
                : result;
        }
    }
}