// <copyright file="KohonenLayerNeuron.cs" company="Scada International A/S">
// Copyright (c) Scada International A/S. All rights reserved.
// </copyright>

using System.Collections.Generic;

namespace KohonenCards
{
    public class KohonenLayerNeuron : Neuron
    {
        public KohonenLayerNeuron(int posX, int posY)
        {
            Position = new Point(posX, posY);
            Weights = new List<double>();
        }

        public Point Position { get; set; }

        public override void GenerateOutputSignals()
        {
            throw new System.NotImplementedException();
        }
    }
}