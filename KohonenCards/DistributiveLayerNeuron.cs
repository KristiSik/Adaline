// <copyright file="DistributiveLayerNeuron.cs" company="Scada International A/S">
// Copyright (c) Scada International A/S. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;

namespace KohonenCards
{
    public class DistributiveLayerNeuron : Neuron
    {
        public DistributiveLayerNeuron()
        {
            Weights = new List<double>
            {
                1,
            };
        }

        public override void GenerateOutputSignals()
        {
            if (Weights.Count != InputSignals.Count)
            {
                throw new Exception("Number of weights doesn't match number of input signals in neuron.");
            }

            double signalValue = 0;
            for (int i = 0; i < Weights.Count; i++)
            {
                signalValue += Weights[i] * InputSignals[i].Value;
            }

            foreach (var outputSignal in OutputSignals)
            {
                outputSignal.Value = signalValue;
            }
        }
    }
}