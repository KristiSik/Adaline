using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Common.Models;
using Common.Utility;
using KohonenCards;
using KohonenCards.Models;
using Microsoft.Win32;
using NeuralNetworksUI.Misc;
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
        private static readonly string[] CLUSTER_COLORS =
            { "#FFD700", "#32CD32", "#00CED1", "#6495ED", "#8A2BE2", "#FF00FF", "#FFC0CB", "#D2691E", "#E6E6FA" };

        private KohonenCardNeuralNetwork _neuralNetwork;
        private string _numberOfClusters = "5";
        private string _initialNeighborhoodParameter = "0,5";
        private string _cardWidth = "5";
        private string _cardHeight = "5";
        private string _learningRateConstA = "1";
        private string _learningRateConstB = "10";
        private ObservableCollection<ColumnNameItem> _columnNames;
        private string _fullSourceFilePath;
        private List<InputData> _inputData;
        private bool _isCalculateAllowed;
        private bool _isLearnAllowed;
        private bool _isExportResultAllowed;
        private string _sourceFilePath;
        private Stopwatch _stopwatch;
        private int _cardHeightInt;
        private int _cardWidthInt;
        private double _learningRateConstADouble;
        private double _learningRateConstBDouble;
        private double _initialNeighborhoodParameterDouble;
        private int _numberOfClustersInt;
        private LineSeries _heatMapLineSeries;
        private LineSeries _clusteredDataLineSeries;
        private PlotModel _model;
        private List<Cluster> _clusters;

        public KohonenCardsViewModel()
        {
            SelectFileCommand = new DelegateCommand(ExecuteSelectFileCommand);
            LearnCommand = new DelegateCommand(ExecuteLearnCommand);
            CalculateCommand = new DelegateCommand(ExecuteCalculateCommand);
            ColumnNames = new ObservableCollection<ColumnNameItem>();
            ExportResultCommand = new DelegateCommand(ExecuteExportResultCommand);
            Model = new PlotModel();
        }

        private void ExecuteExportResultCommand()
        {
            if (_clusters == null)
            {
                MessageBox.Show("Clustering should be done before export.");
                return;
            }

            var saveFileDialog = new SaveFileDialog
            {
                Filter = "CSV|*.csv",
                Title = "Selecting file to store result"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    DataManager.WriteData(
                        saveFileDialog.FileName,
                        ColumnNames.Where(c => c.IsChecked).Select(c => c.Text).Union(new []{ "Cluster" }).ToArray(),
                        _clusters.SelectMany((c, index) => c.InputDataResults.Select(i =>
                        {
                            i.InputData.Inputs.Add(index);
                            return i.InputData;
                        }).ToList()).ToList());
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to write data to CSV file. Reason: {ex.Message}");
                }
            }
        }

        private void ExecuteCalculateCommand()
        {
            IsExportResultAllowed = false;

            if (!int.TryParse(_numberOfClusters, out _numberOfClustersInt))
            {
                MessageBox.Show("Number of cluster is inappropriate.");
                return;
            }

            Task.Run(() =>
            {
                _stopwatch = new Stopwatch();
                _stopwatch.Start();
                try
                {
                    _clusters =
                        _neuralNetwork.Cluster(_inputData.Skip((int)(_inputData.Count * 0.7)).ToList(), _numberOfClustersInt);

                    Model.Series.Clear();
                    Model.Series.Add(_heatMapLineSeries);
                    Model.InvalidatePlot(true);

                    for (int i = 0; i < _clusters.Count; i++)
                    {
                        DisplayInputDataResultsOnPlot(_clusters[i].InputDataResults, CLUSTER_COLORS[i]);
                    }

                    IsExportResultAllowed = true;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to learn. Reason: {ex.Message}");
                }

                _stopwatch.Stop();
            });
        }

        private void ExecuteLearnCommand()
        {
            IsCalculateAllowed = false;
            IsExportResultAllowed = false;

            if (!int.TryParse(_cardHeight, out _cardHeightInt))
            {
                MessageBox.Show("Card height is inappropriate.");
                return;
            }

            if (!int.TryParse(_cardWidth, out _cardWidthInt))
            {
                MessageBox.Show("Card width is inappropriate.");
                return;
            }

            if (!double.TryParse(_learningRateConstA, out _learningRateConstADouble))
            {
                MessageBox.Show("Learning rate const A is inappropriate. Use ',' to separate decimals.");
                return;
            }

            if (!double.TryParse(_learningRateConstB, out _learningRateConstBDouble))
            {
                MessageBox.Show("Learning rate const B is inappropriate. Use ',' to separate decimals.");
                return;
            }

            if (!double.TryParse(_initialNeighborhoodParameter, out _initialNeighborhoodParameterDouble))
            {
                MessageBox.Show("Initial neighborhood parameter is inappropriate. Use ',' to separate decimals.");
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


            Model.Series.Clear();
            InitializeHeatMapLineSeries(kohonenLayerNeurons);

            Task.Run(() =>
            {
                _stopwatch = new Stopwatch();
                _stopwatch.Start();
                try
                {
                    // taking 70% of input data for learning (learn data)
                    List<InputDataResult> inputDataResults =
                        _neuralNetwork.Learn(_inputData.Take((int) (_inputData.Count * 0.7)).ToList());

                    DisplayInputDataResultsOnPlot(inputDataResults, "#FF4500");

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

        private void DisplayInputDataResultsOnPlot(List<InputDataResult> inputDataResults, string color)
        {
            _clusteredDataLineSeries = new LineSeries
            {
                MarkerFill = OxyColor.Parse(color),
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

        public bool IsExportResultAllowed
        {
            get => _isExportResultAllowed;
            set => SetProperty(ref _isExportResultAllowed, value);
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

        public DelegateCommand ExportResultCommand { get; set; }
    }
}