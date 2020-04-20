using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Adaline.Models;
using Adaline.Utility;
using Serilog;

namespace Adaline
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 3)
            {
                Console.WriteLine("Usage: <learning rate> <file.csv> <result column name> [<inputColumnName1> <inputColumnName2> ...]");
                return;
            }

            if (!double.TryParse(args[0], out double learningRate))
            {
                Console.WriteLine("Learning rate should be of double type.");
                return;
            }

            ConfigureLogger();
            Log.Information("Application started.");

            List<InputData> inputData;
            try
            {
                inputData = DataManager.ReadInputData(args[1], args[2], args.Skip(3).ToArray()).ToList();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to read input data.");
                return;
            }
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
