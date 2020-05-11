// <copyright file="KohonenLayerNeuron.cs" company="Scada International A/S">
// Copyright (c) Scada International A/S. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;

namespace KohonenCards.Models
{
    public class KohonenLayerNeuron : Neuron
    {
        public KohonenLayerNeuron(int posX, int posY)
        {
            Position = new Point(posX, posY);
            Weights = new List<double>();
            InputSignals = new List<Signal>();
        }

        public Point Position { get; set; }

        public override void FeedForward()
        {
            throw new System.NotImplementedException();
        }

        public double DistanceToNeuron(KohonenLayerNeuron neuron)
        {
            return Math.Sqrt(Math.Pow(Position.X, neuron.Position.X) + Math.Pow(Position.Y, neuron.Position.Y));
        }
    }
}