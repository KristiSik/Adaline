using System;
using System.Collections.Generic;
using System.Linq;
using Common.Models;
using Serilog;

namespace Adaline.Models
{
    public class EpochFinishedEventArgs : EventArgs
    {
        public List<double> Weights { get; set; }

        public int Epoch { get; set; }

        public bool Finished { get; set; }

        public double MeanSquare { get; set; }
    }

    public class Adaline
    {
        private readonly double _learningRate;
        private readonly double _desiredLms;
        private readonly List<double> _weights;
        private readonly List<Node> _nodes;

        private int _epochNumber;

        public event EventHandler<EpochFinishedEventArgs> EpochFinished;

        public Adaline(List<InputData> input, double learningRate, double desiredLms)
        {
            _learningRate = learningRate;
            _desiredLms = desiredLms;
            _nodes = FormNodes(input);
            _epochNumber = 0;
            _weights = GenerateRandomWeights(input.First().Inputs.Count);
            Log.Information(
                "Adaline instance created with {InputDataCount} inputs and learning rate {LearningRate}.",
                input.Count,
                learningRate);
        }

        public void Start()
        {
            EpochFinished?.Invoke(this, new EpochFinishedEventArgs { Weights = _weights.ToList(), Epoch = 0 });
            double meanSquare = double.MaxValue;
            _epochNumber = 0;

            while (meanSquare > _desiredLms)
            {
                ++_epochNumber;
                double ms = Iterate();
                if (Math.Abs(ms - meanSquare) < 1E-14)
                {
                    EpochFinished?.Invoke(this, new EpochFinishedEventArgs { Weights = _weights.ToList(), Epoch = _epochNumber, Finished = true, MeanSquare = ms });
                    return;
                }

                meanSquare = ms;
            }
        }

        public double CalculateForInput(List<double> inputs)
        {
            Node node = new Node(inputs, 0);
            return node.Calculate(_weights);
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

        private double Iterate()
        {
            ++_epochNumber;
            double meanSquare = GetMeanSquare();
            Log.Information("Epoch {Epoch}, mean square {MeanSquare}.", _epochNumber ,meanSquare);

            _nodes.ForEach(n => n.UpdateWeights(_learningRate, _weights));
            EpochFinished?.Invoke(this, new EpochFinishedEventArgs { Weights = _weights.ToList(), Epoch = _epochNumber, MeanSquare = meanSquare });
            return meanSquare;
        }

        public double GetMeanSquare() => _nodes.Average(n => Math.Pow(n.ExpectedResult - n.Calculate(_weights), 2));

        private List<Node> FormNodes(List<InputData> input)
        {
            return input.Select(i => new Node(i.Inputs, i.Result)).ToList();
        }
    }
}