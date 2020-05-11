// <copyright file="Layer.cs" company="Scada International A/S">
// Copyright (c) Scada International A/S. All rights reserved.
// </copyright>

using System.Collections.Generic;

namespace KohonenCards.Models
{
    public class Layer
    {
        public List<Neuron> Neurons { get; set; }

        public Layer()
        {
            Neurons = new List<Neuron>();
        }

        public Layer(List<Neuron> neurons)
        {
            Neurons = neurons;
        }
    }
}