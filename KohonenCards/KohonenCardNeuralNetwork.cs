// <copyright file="KohonenCardNeuralNetwork.cs" company="Scada International A/S">
// Copyright (c) Scada International A/S. All rights reserved.
// </copyright>

using System.Collections.Generic;

namespace KohonenCards
{
    public class KohonenCardNeuralNetwork
    {
        private readonly List<Layer> _layers;
        private readonly int _kohonenCardWidth;
        private readonly int _kohonenCardHeight;
        private readonly double _learningRate;

        public KohonenCardNeuralNetwork(int kohonenCardWidth, int kohonenCardHeight, int learningRate)
        {
            _kohonenCardWidth = kohonenCardWidth;
            _kohonenCardHeight = kohonenCardHeight;
            _learningRate = learningRate;

            _layers = new List<Layer>();
        }

        public void Learn()
        {

        }

        /// <summary>
        ///     Initializes two layers in neural network and neurons inside.
        /// </summary>
        /// <param name="dataDimension">Number of attributes each vector has. Affects number of neurons in first (distributive) layer.</param>
        public void InitializeLayers(int dataDimension)
        {
            // initializing neurons in distributive (first) layer
            var firstLayer = new Layer();
            for (int i = 0; i < dataDimension; i++)
            {
                firstLayer.Neurons.Add(new DistributiveLayerNeuron());
            }

            // initializing neurons in Kohonen (second) layer
            var secondLayer = new Layer();
            int x = 0;
            int y = 0;
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

                secondLayer.Neurons.Add(neuron);

                if (y == _kohonenCardWidth)
                {
                    y = 0;
                    x++;
                }
            }

            _layers.Add(firstLayer);
            _layers.Add(secondLayer);
        }
    }
}