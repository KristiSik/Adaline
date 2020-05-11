using OxyPlot;
using OxyPlot.Series;
using Prism.Mvvm;

namespace NeuralNetworksUI.ViewModels
{
    public class KohonenCardsViewModel : BindableBase
    {
        public KohonenCardsViewModel()
        {
            // Create the plot model
            var tmp = new PlotModel {Title = "Simple example", Subtitle = "using OxyPlot"};

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
            Model = tmp;
        }

        public PlotModel Model { get; set; }
    }
}