using Adaline.Utility;
using Microsoft.Win32;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Adaline.Models;
using AdalineUI.Misc;

namespace AdalineUI.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
        private string _sourceFilePath;
        private string _fullSourceFilePath;
        private ObservableCollection<ColumnNameItem> _columnNames;
        private ObservableCollection<DataInfo> _weights;
        private bool _isLearnAllowed;
        private bool _isCalculateAllowed;
        private global::Adaline.Models.Adaline _adaline;
        private string _learningRate = "0,1";
        private double _learningRateDouble;
        private string _desiredLms = "1";
        private double _desiredLmsDouble;
        private List<InputData> _inputData;
        private int _progressBarValue;
        private double _calculatedResult;
        private ObservableCollection<DataInfo> _customInputs;
        private int _progressBarMaximum;
        private double _meanSquare;

        public string LearningRate
        {
            get => _learningRate;
            set => SetProperty(ref _learningRate, value);
        }

        public string DesiredLms
        {
            get => _desiredLms;
            set =>  SetProperty(ref _desiredLms, value);
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

        public ObservableCollection<DataInfo> Weights
        {
            get => _weights;
            set => SetProperty(ref _weights, value);
        }

        public ObservableCollection<DataInfo> CustomInputs
        {
            get => _customInputs;
            set => SetProperty(ref _customInputs, value);
        }

        public int ProgressBarMaximum
        {
            get => _progressBarMaximum;
            set => SetProperty(ref _progressBarMaximum, value);
        }

        public int ProgressBarValue
        {
            get => _progressBarValue;
            set => SetProperty(ref _progressBarValue, value);
        }

        public double CalculatedResult
        {
            get => _calculatedResult;
            set => SetProperty(ref _calculatedResult, value);
        }

        public double MeanSquare
        {
            get => _meanSquare;
            set => SetProperty(ref _meanSquare, value);
        }

        public DelegateCommand<string> SelectResultColumnCommand { get; set; }

        public DelegateCommand SelectFileCommand { get; set; }

        public DelegateCommand LearnCommand { get; set; }

        public DelegateCommand CalculateCommand { get; set; }

        public MainWindowViewModel()
        {
            SelectFileCommand = new DelegateCommand(ExecuteSelectFileCommand);
            SelectResultColumnCommand = new DelegateCommand<string>(ExecuteSelectResultColumnCommand);
            LearnCommand = new DelegateCommand(ExecuteLearnCommand);
            CalculateCommand = new DelegateCommand(ExecuteCalculateCommand);
            ColumnNames = new ObservableCollection<ColumnNameItem>();
            Weights = new ObservableCollection<DataInfo>();
            CustomInputs = new ObservableCollection<DataInfo>();
            ColumnNames.CollectionChanged += ColumnNamesOnCollectionChanged;
        }

        private void ColumnNamesOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (var eNewItem in e.NewItems)
                {
                    ((ColumnNameItem) eNewItem).PropertyChanged += OnPropertyChanged;
                }
            }

            if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (var eOldItem in e.OldItems)
                {
                    ((ColumnNameItem)eOldItem).PropertyChanged -= OnPropertyChanged;
                }
            }
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ColumnNameItem.IsChecked))
            {
                Weights.Clear();
                Weights.AddRange(ColumnNames.Where(c => c.IsChecked).Select((c, index) => new DataInfo
                {
                    Name = $"w{index}",
                }));
                CustomInputs.Clear();
                CustomInputs.AddRange(ColumnNames.Where(c => c.IsChecked).Select((c, index) => new DataInfo
                {
                    Name = $"x{index}",
                }));
            }
        }

        private void ExecuteCalculateCommand()
        {
            if (_adaline == null)
            {
                MessageBox.Show("Adaline is not initialized.");
                return;
            }

            try
            {
                CalculatedResult = _adaline.CalculateForInput(CustomInputs.Select(c => c.Value).ToList());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to calculate. Reason: {ex.Message}");
            }
        }

        private void ExecuteLearnCommand()
        {
            if (_adaline != null)
            {
                _adaline.EpochFinished -= AdalineOnEpochFinished;
                _adaline = null;
            }

            IsCalculateAllowed = false;

            if (!double.TryParse(_learningRate, out _learningRateDouble))
            {
                MessageBox.Show("Learning rate is appropriate. Use ',' to separate decimals.");
                return;
            }
            if (!double.TryParse(_desiredLms, out _desiredLmsDouble))
            {
                MessageBox.Show("Desired LMS is appropriate.");
                return;
            }

            try
            {
                _inputData = DataManager.ReadInputData(_fullSourceFilePath, GetResultColumnName(), _columnNames.Where(c => c.IsChecked).Select(c => c.Text).ToArray());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to read values from CSV file. Reason: {ex.Message}");
                return;
            }

            _adaline = new Adaline.Models.Adaline(_inputData, _learningRateDouble, _desiredLmsDouble);
            MeanSquare = _adaline.GetMeanSquare();

            _adaline.EpochFinished += AdalineOnEpochFinished;
            Task.Run(() =>
            {
                _adaline.Start();
                IsCalculateAllowed = true;
            });
        }

        private void AdalineOnEpochFinished(object sender, EpochFinishedEventArgs args)
        {
            if (Weights.Count != args.Weights.Count)
            {
                Application.Current.Dispatcher.Invoke(delegate
                {
                    Weights.Clear();
                    Weights.AddRange(args.Weights.Select((w, index) => new DataInfo
                    {
                        Name = $"w{index}",
                        Value = w,
                    }));
                });
            }
            else
            {
                for (var i = 0; i < args.Weights.Count; i++)
                {
                    Application.Current.Dispatcher.Invoke(delegate
                    {
                        Weights[i].Value = args.Weights[i];
                    });
                }
            }

            Application.Current.Dispatcher.Invoke(delegate
            {
                MeanSquare = args.MeanSquare;
            });
        }

        string GetResultColumnName()
        {
            return _columnNames.First(c => c.IsResult).Text;
        }

        void ExecuteSelectFileCommand()
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
                    ColumnNames.AddRange(columnNames.Select(c => new ColumnNameItem {Text = c}));
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to read column names from CSV file. Reason: {ex.Message}");
                }
            }
        }

        void ExecuteSelectResultColumnCommand(string columnName)
        {
            ColumnNameItem item = ColumnNames.FirstOrDefault(c => c.Text == columnName);
            foreach (var columnNameItem in ColumnNames)
            {
                columnNameItem.IsResult = false;
            }

            item.IsResult = true;
            IsLearnAllowed = true;
        }
    }
}