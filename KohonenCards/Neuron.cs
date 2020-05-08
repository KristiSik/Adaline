// <copyright file="Neuron.cs" company="Scada International A/S">
// Copyright (c) Scada International A/S. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;

namespace KohonenCards
{
    public abstract class Neuron
    {
        protected Neuron()
        {
            InputSignals = new List<Signal>();
            OutputSignals = new List<Signal>();
        }

        public List<double> Weights { get; set; }

        public List<Signal> InputSignals { get; set; }

        public List<Signal> OutputSignals { get; set; }

        public abstract void GenerateOutputSignals();

        public double DistanceToWeightVector(List<double> weights)
        {
            if (weights.Count != Weights.Count)
            {
                throw new Exception("Neuron's number of weights doesn't match number of vector weights.");
            }

            // calculating Euclidean distance
            double result = 0;
            for (int i = 0; i < weights.Count; i++)
            {
                result += Math.Pow(weights[i] - Weights[i], 2);
            }

            result = Math.Sqrt(result);
            return result;
        }

        public double DistanceToNeuron(Neuron neuron)
        {
            return DistanceToWeightVector(neuron.Weights);
        }
    }
}