using System;
using System.Collections.Generic;
using System.Linq;
using Common.Models;
using Common.Utility;
using Serilog;

namespace KohonenCards
{
    class Program
    {
        static void Main(string[] args)
        {
            ConfigureLogger();
            
            List<InputData> inputData;
            try
            {
                inputData = DataManager.ReadInputData("absenteeism at work.csv", null).ToList();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to read input data.");
                return;
            }
            
            KohonenCardNeuralNetwork n = new KohonenCardNeuralNetwork(
                5,
                5,
                1,
                10,
                0.5,
                Log.Logger);
            
            n.InitializeLayers(inputData[0].Inputs.Count);
            n.Learn(inputData);
        }

        private static void ConfigureLogger()
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.Console()
                .CreateLogger();
        }
    }
}
