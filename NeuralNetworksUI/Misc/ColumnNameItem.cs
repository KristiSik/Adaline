using Prism.Mvvm;

namespace NeuralNetworksUI.Misc
{
    public class ColumnNameItem : BindableBase
    {
        private bool _isChecked;
        private bool _isResult;
        public string Text { get; set; }

        public bool IsChecked
        {
            get => _isChecked;
            set
            {
                if (value && IsResult) return;

                SetProperty(ref _isChecked, value);
            }
        }

        public bool IsResult
        {
            get => _isResult;
            set
            {
                if (value) IsChecked = false;

                _isResult = value;
            }
        }
    }
}