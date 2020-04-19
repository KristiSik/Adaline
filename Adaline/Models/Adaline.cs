// <copyright file="Adaline.cs" company="Scada International A/S">
// Copyright (c) Scada International A/S. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ConsoleTable;
using Serilog;

namespace Adaline.Models
{
    public class Adaline
    {
        private readonly double _learningRate;
        private readonly List<double> _weights;
        private readonly List<Node> _nodes;
        private readonly string[] _tableHeaders;

        private int _epochNumber;

        public Adaline(List<InputData> input, double learningRate)
        {
            _learningRate = learningRate;
            _nodes = FormNodes(input);
            _epochNumber = 0;
            _weights = GenerateRandomWeights(input.First().Inputs.Count);

            Log.Information(
                "Adaline instance created with {InputDataCount} inputs and learning rate {LearningRate}.",
                input.Count,
                learningRate);
            for (var i = 0; i < 16000; i++)
            {
                Iterate();
            }

            CalculateForInput();
        }

        private List<double> GenerateRandomWeights(int count)
        {
            Random random = new Random();
            var result = new List<double>();
            for (var i = 0; i < count; i++)
            {
                result.Add(random.NextDouble());
            }

            return result;
        }

        private void CalculateForInput()
        {
            Console.Write("Enter inputs: ");
            List<double> inputs = Console.ReadLine().Split(" ").Select(s => double.Parse(s)).ToList();
            Node node = new Node(inputs, 0);
            var r = node.Calculate(_weights);
            Console.WriteLine(r);
        }

        private void Iterate()
        {
            ////PrintStatus();
            ++_epochNumber;
            double meanSquare = _nodes.Average(n => Math.Pow(n.ExpectedResult - n.Calculate(_weights), 2));
            Log.Information("Epoch {Epoch}, mean square {MeanSquare}.", _epochNumber ,meanSquare);

            _nodes.ForEach(n => n.UpdateWeights(_learningRate, _weights));
        }

        ////private string[] GenerateTableHeaders()
        ////{
        ////    var result = new List<string>
        ////    {
        ////        "Epoch",
        ////    };
        ////    result.AddRange(_weights.Select((w, index) => $"W{index}"));
        ////    return result.ToArray();
        ////}

        private List<Node> FormNodes(List<InputData> input)
        {
            return input.Select(i => new Node(i.Inputs, i.Result)).ToList();
        }

        ////private void PrintStatus()
        ////{
        ////    Log.Information("Result:");
        ////    var table = new Table();
        ////    table.SetHeaders(_tableHeaders);
        ////    table.AddRow(new [] { _epochNumber.ToString() }.Concat(_weights.Select(w => w.ToString(CultureInfo.InvariantCulture))).ToArray());
        ////    Console.WriteLine(table.ToString());
        ////}
    }
}