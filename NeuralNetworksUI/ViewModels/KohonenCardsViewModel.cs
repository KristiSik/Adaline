using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Common.Models;
using Common.Utility;
using KohonenCards;
using KohonenCards.Models;
using Microsoft.Win32;
using NeuralNetworksUI.Misc;
using NeuralNetworksUI.Views;
using OxyPlot;
using OxyPlot.Series;
using Prism.Commands;
using Prism.Mvvm;
using Serilog;

namespace NeuralNetworksUI.ViewModels
{
    public class KohonenCardsViewModel : BindableBase
    {
        private const double MAX_JITTER = 0.1;

        private KohonenCardNeuralNetwork _neuralNetwork;
        private string _numberOfClusters = "5";
        private string _initialNeighborhoodParameter = "0,5";
        private string _cardWidth = "5";
        private string _cardHeight = "5";
        private string _learningRateConstA = "1";
        private string _learningRateConstB = "10";
        private ObservableCollection<ColumnNameItem> _columnNames;
        private int _epoch;
        private string _fullSourceFilePath;
        private List<InputData> _inputData;
        private bool _isCalculateAllowed;
        private bool _isLearnAllowed;
        private string _learningRate = "0,1";
        private double _learningRateDouble;
        private double _meanSquare;
        private int _progressBarMaximum;
        private int _progressBarValue;
        private string _sourceFilePath;
        private string _spentTime;
        private Stopwatch _stopwatch;
        private ObservableCollection<DataInfo> _weights;
        private int _cardHeightInt;
        private int _cardWidthInt;
        private double _learningRateConstADouble;
        private double _learningRateConstBDouble;
        private double _initialNeighborhoodParameterDouble;
        private int _numberOfClustersInt;
        private LineSeries _heatMapLineSeries;
        private LineSeries _clusteredDataLineSeries;
        private PlotModel _model;

        public KohonenCardsViewModel()
        {
            SelectFileCommand = new DelegateCommand(ExecuteSelectFileCommand);
            LearnCommand = new DelegateCommand(ExecuteLearnCommand);
            CalculateCommand = new DelegateCommand(ExecuteCalculateCommand);
            ColumnNames = new ObservableCollection<ColumnNameItem>();

            // Create the plot model
            var tmp = new PlotModel();

            // Create two line series (markers are hidden by default)
            var series1 = new LineSeries
            {
                Title = "Series 1", MarkerType = MarkerType.Circle, MarkerFill = OxyColor.Parse("#020305"),
                Color = OxyColor.FromArgb(0, 0, 0, 0)
            };
            series1.Points.Add(new DataPoint(0, 0));
            series1.Points.Add(new DataPoint(10, 18));
            series1.Points.Add(new DataPoint(20, 12));
            series1.Points.Add(new DataPoint(30, 8));
            series1.Points.Add(new DataPoint(40, 15));

            var series2 = new LineSeries
            {
                Title = "Series 2", MarkerType = MarkerType.Square, MarkerFill = OxyColor.Parse("#582939"),
                Color = OxyColor.FromArgb(0, 0, 0, 0)
            };
            series2.Points.Add(new DataPoint(0, 4));
            series2.Points.Add(new DataPoint(10, 12));
            series2.Points.Add(new DataPoint(20, 16));
            series2.Points.Add(new DataPoint(30, 25));
            series2.Points.Add(new DataPoint(40, 5));


            // Add the series to the plot model
            tmp.Series.Add(series1);
            tmp.Series.Add(series2);

            // Axes are created automatically if they are not defined

            // Set the Model property, the INotifyPropertyChanged event will make the WPF Plot control update its content
            Model = new PlotModel();
        }

        private void ExecuteCalculateCommand()
        {
        }

        private void ExecuteLearnCommand()
        {
            IsCalculateAllowed = false;

            if (!double.TryParse(_learningRate, out _learningRateDouble))
            {
                MessageBox.Show("Learning rate is appropriate. Use ',' to separate decimals.");
                return;
            }

            if (!int.TryParse(_cardHeight, out _cardHeightInt))
            {
                MessageBox.Show("Card height is appropriate.");
                return;
            }

            if (!int.TryParse(_cardWidth, out _cardWidthInt))
            {
                MessageBox.Show("Card width is appropriate.");
                return;
            }

            if (!int.TryParse(_numberOfClusters, out _numberOfClustersInt))
            {
                MessageBox.Show("Number of cluster is appropriate.");
                return;
            }

            if (!double.TryParse(_learningRateConstA, out _learningRateConstADouble))
            {
                MessageBox.Show("Learning rate const A is appropriate. Use ',' to separate decimals.");
                return;
            }

            if (!double.TryParse(_learningRateConstB, out _learningRateConstBDouble))
            {
                MessageBox.Show("Learning rate const B is appropriate. Use ',' to separate decimals.");
                return;
            }

            if (!double.TryParse(_initialNeighborhoodParameter, out _initialNeighborhoodParameterDouble))
            {
                MessageBox.Show("Initial neighborhood parameter is appropriate. Use ',' to separate decimals.");
                return;
            }

            try
            {
                _inputData = DataManager.ReadInputData(_fullSourceFilePath, null,
                    _columnNames.Where(c => c.IsChecked).Select(c => c.Text).ToArray());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to read values from CSV file. Reason: {ex.Message}");
                return;
            }

            _neuralNetwork = new KohonenCardNeuralNetwork(
                _cardWidthInt,
                _cardHeightInt,
                _learningRateConstADouble,
                _learningRateConstBDouble,
                _initialNeighborhoodParameterDouble,
                Log.Logger);

            List<KohonenLayerNeuron> kohonenLayerNeurons = _neuralNetwork.InitializeLayers(ColumnNames.Count(c => c.IsChecked));
            InitializeHeatMapLineSeries(kohonenLayerNeurons);

            Task.Run(async () =>
            {
                _stopwatch = new Stopwatch();
                _stopwatch.Start();
                try
                {
                    // taking 70% of input data for learning (learn data)
                    List<InputDataResult> inputDataResults =
                        await _neuralNetwork.Learn(_inputData.Take((int) (_inputData.Count * 0.7)).ToList());

                    DisplayInputDataResultsOnPlot(inputDataResults);

                    IsCalculateAllowed = true;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to learn. Reason: {ex.Message}");
                    IsCalculateAllowed = false;
                }

                _stopwatch.Stop();
            });
        }

        private void DisplayInputDataResultsOnPlot(List<InputDataResult> inputDataResults)
        {
            if (Model.Series.Contains(_clusteredDataLineSeries))
            {
                Model.Series.Remove(_clusteredDataLineSeries);
            }

            _clusteredDataLineSeries = new LineSeries
            {
                MarkerFill = OxyColor.Parse("#040168"),
                MarkerType = MarkerType.Circle,
                Color = OxyColor.FromArgb(0, 0, 0, 0),
            };

            var rnd = new Random();
            inputDataResults.ForEach(r => 
                _clusteredDataLineSeries.Points.Add(
                    new DataPoint(
                        r.Neuron.Position.X + (-MAX_JITTER + 2 * MAX_JITTER * rnd.NextDouble()),
                        r.Neuron.Position.Y + (-MAX_JITTER + 2 * MAX_JITTER * rnd.NextDouble()))));

            Model.Series.Add(_clusteredDataLineSeries);

            Model.InvalidatePlot(true);
        }

        private void InitializeHeatMapLineSeries(List<KohonenLayerNeuron> kohonenLayerNeurons)
        {
            _heatMapLineSeries = new LineSeries
            {
                MarkerStroke = OxyColor.Parse("#0101FF"),
                MarkerStrokeThickness = 1,
                MarkerType = MarkerType.Circle,
                MarkerSize = 3,
                Color = OxyColor.FromArgb(0, 0, 0, 0),
            };

            kohonenLayerNeurons.ForEach(n => _heatMapLineSeries.Points.Add(new DataPoint(n.Position.X, n.Position.Y)));

            Model.Series.Clear();
            Model.Series.Add(_heatMapLineSeries);
            Model.InvalidatePlot(true);
        }

        private void ExecuteSelectFileCommand()
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "CSV|*.csv",
                Title = "Selecting source file"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                IsLearnAllowed = false;
                SourceFilePath = openFileDialog.SafeFileName;
                _fullSourceFilePath = openFileDialog.FileName;
                try
                {
                    var columnNames = DataManager.ReadColumnNames(_fullSourceFilePath);
                    ColumnNames.Clear();
                    ColumnNames.AddRange(columnNames.Select(c => new ColumnNameItem { Text = c }));
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to read column names from CSV file. Reason: {ex.Message}");
                }
            }

            IsLearnAllowed = true;
        }

        public string SourceFilePath
        {
            get => _sourceFilePath;
            set => SetProperty(ref _sourceFilePath, value);
        }

        public ObservableCollection<ColumnNameItem> ColumnNames
        {
            get => _columnNames;
            set => SetProperty(ref _columnNames, value);
        }

        public bool IsLearnAllowed
        {
            get => _isLearnAllowed;
            set => SetProperty(ref _isLearnAllowed, value);
        }

        public bool IsCalculateAllowed
        {
            get => _isCalculateAllowed;
            set => SetProperty(ref _isCalculateAllowed, value);
        }

        public string InitialNeighborhoodParameter
        {
            get => _initialNeighborhoodParameter;
            set => SetProperty(ref _initialNeighborhoodParameter, value);
        }

        public string CardWidth
        {
            get => _cardWidth;
            set => SetProperty(ref _cardWidth, value);
        }

        public string CardHeight
        {
            get => _cardHeight;
            set => SetProperty(ref _cardHeight, value);
        }

        public string LearningRateConstA
        {
            get => _learningRateConstA;
            set => SetProperty(ref _learningRateConstA, value);
        }

        public string LearningRateConstB
        {
            get => _learningRateConstB;
            set => SetProperty(ref _learningRateConstB, value);
        }

        public string NumberOfClusters
        {
            get => _numberOfClusters;
            set => SetProperty(ref _numberOfClusters, value);
        }

        public PlotModel Model
        {
            get => _model;
            set => SetProperty(ref _model, value);
        }


        public DelegateCommand SelectFileCommand { get; set; }

        public DelegateCommand LearnCommand { get; set; }

        public DelegateCommand CalculateCommand { get; set; }

    }
}