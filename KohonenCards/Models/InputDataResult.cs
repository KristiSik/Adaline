// <copyright file="InputDataResult.cs" company="Scada International A/S">
// Copyright (c) Scada International A/S. All rights reserved.
// </copyright>

using Common.Models;

namespace KohonenCards.Models
{
    public class InputDataResult
    {
        public InputDataResult(InputData inputData, KohonenLayerNeuron neuron)
        {
            InputData = inputData;
            Neuron = neuron;
        }

        public InputData InputData { get; set; }

        public KohonenLayerNeuron Neuron { get; set; }
    }
}