using System.Collections.Generic;

namespace Adaline.Models
{
    public class Node
    {
        private double _lastCalculatedResult;

        public List<double> Inputs { get; }

        public double ExpectedResult { get; }

        public double Calculate(List<double> weights)
        {
            _lastCalculatedResult = CalculateForInputs(Inputs, weights);
            return _lastCalculatedResult;
        }

        public double CalculateForInputs(List<double> inputs, List<double> weights)
        {
            double result = 0;
            for (var i = 0; i < weights.Count; i++)
            {
                result += weights[i] * inputs[i];
            }

            return result;
        }

        public void UpdateWeights(double learningRate, List<double> weights)
        {
            double delta = ExpectedResult - _lastCalculatedResult;
            for (var i = 0; i < weights.Count; i++)
            {
                weights[i] += learningRate * delta * Inputs[i];
            }
        }

        public Node(List<double> inputs, double result)
        {
            Inputs = inputs;
            ExpectedResult = result;
        }
    }
}