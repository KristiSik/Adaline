// <copyright file="DataManager.cs" company="Scada International A/S">
// Copyright (c) Scada International A/S. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Adaline.Models;
using Serilog;

namespace Adaline.Utility
{
    public static class DataManager
    {
        private const string DEFAULT_CSV_SEPARATOR = ";";

        /// <summary>
        ///     Read data from csv file and convert that into list of <see cref="InputData"/> objects.
        /// </summary>
        /// <param name="filePath">Path to the csv file.</param>
        /// <param name="resultColumnName">Name of the column in csv file which contains result.</param>
        /// <param name="inputColumnNames">Columns, which should be taken as inputs. If empty, all columns will be taken (except result).</param>
        /// <returns></returns>
        public static List<InputData> ReadInputData(string filePath, string resultColumnName,
            params string[] inputColumnNames)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            if (string.IsNullOrEmpty(resultColumnName))
            {
                throw new ArgumentNullException(nameof(resultColumnName));
            }

            var result = new List<InputData>();
            List<string> inputColumnNamesList = new List<string>();
            using (var sr = new StreamReader(filePath))
            {
                List<string> columnNames = sr.ReadLine()?.Split(DEFAULT_CSV_SEPARATOR).ToList();
                if (columnNames == null)
                {
                    throw new Exception("Can't read column names.");
                }

                int indexOfResultColumn = columnNames.IndexOf(resultColumnName);
                if (indexOfResultColumn < 0)
                {
                    throw new Exception($"Result column '{resultColumnName}' is not present in the list of columns.");
                }

                if (inputColumnNames == null || inputColumnNames.Length == 0)
                {
                    inputColumnNamesList = columnNames.Except(new[] {resultColumnName}).ToList();
                }
                else
                {
                    inputColumnNamesList = inputColumnNames.ToList();
                }

                inputColumnNamesList.ForEach(i =>
                {
                    if (columnNames.All(c => c != i))
                    {
                        throw new Exception($"Input column '{i}' is not present in the list of columns.");
                    }
                });

                int rawIndex = 2;
                while (!sr.EndOfStream)
                {
                    var inputData = new InputData();
                    List<string> values = sr.ReadLine()?.Split(DEFAULT_CSV_SEPARATOR).ToList();
                    if (values == null)
                    {
                        throw new Exception($"Can't read values from {rawIndex} raw.");
                    }

                    inputColumnNamesList.ForEach(c =>
                    {
                        int indexOfColumn = columnNames.IndexOf(c);
                        if (values.Count <= indexOfColumn)
                        {
                            throw new Exception($"Value with for column {c} is not found in {rawIndex} raw.");
                        }

                        if (!double.TryParse(values[indexOfColumn], out double value))
                        {
                            throw new Exception(
                                $"Failed to parse value '{values[indexOfColumn]}' of column {c} in {rawIndex} raw.");
                        }

                        inputData.Inputs.Add(value);
                    });

                    if (!double.TryParse(values[indexOfResultColumn], out double resValue))
                    {
                        throw new Exception(
                            $"Failed to parse value '{values[indexOfResultColumn]}' of column {resultColumnName} in {rawIndex} raw.");
                    }

                    inputData.Result = resValue;
                    result.Add(inputData);

                    ++rawIndex;
                }
            }

            return result;
        }

        public static List<string> ReadColumnNames(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            using (var sr = new StreamReader(filePath))
            {
                List<string> columnNames = sr.ReadLine()?.Split(DEFAULT_CSV_SEPARATOR).ToList();
                if (columnNames == null)
                {
                    throw new Exception("Can't read column names.");
                }

                return columnNames;
            }
        }
    }
}