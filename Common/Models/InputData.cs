// <copyright file="InputData.cs" company="Scada International A/S">
// Copyright (c) Scada International A/S. All rights reserved.
// </copyright>

using System.Collections.Generic;

namespace Common.Models
{
    public class InputData
    {
        public List<double> Inputs { get; set; }

        public double Result { get; set; }

        public InputData()
        {
            Inputs = new List<double>();
        }
    }
}