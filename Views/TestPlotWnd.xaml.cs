using ScottPlot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Synapse.Crypto.Patterns
{
    /// <summary>
    /// Логика взаимодействия для TestPlotWnd.xaml
    /// </summary>
    public partial class TestPlotWnd : Window
    {
        public TestPlotWnd()
        {
            InitializeComponent();
        }

        private void Plotchart()
        {

            var myPlot = Plot.Plot;


            // 1. Создание графика и свечных данных
            var prices = Generate.RandomOHLCs(50); // Ваши данные
            var cndlPlot = myPlot.Add.Candlestick(prices);

            cndlPlot.Axes.YAxis = myPlot.Axes.Right;
            myPlot.Grid.YAxis = myPlot.Axes.Right;
            myPlot.Axes.Left.IsVisible = false;


            myPlot.Axes.DateTimeTicksBottom();


            // 2. Подготовка данных для маркеров (например, на High и Low)
            double[] markerX = new double[prices.Count * 2];
            double[] markerY = new double[prices.Count * 2];

            for (int i = 0; i < prices.Count; i++)
            {
                // Маркеры для максимумов
                markerX[i * 2] = prices[i].DateTime.ToOADate();
                markerY[i * 2] = prices[i].High;
                // Маркеры для минимумов
                markerX[i * 2 + 1] = prices[i].DateTime.ToOADate();
                markerY[i * 2 + 1] = prices[i].Low;
            }

            // 3. Добавление маркеров на график
            //var markers = myPlot.Add.Markers(markerX, markerY);
            //markers.MarkerStyle.Shape = MarkerShape.FilledDiamond; // Например, крестик
            //markers.MarkerStyle.Size = 8;
            //markers.MarkerStyle.LineColor = ScottPlot.Colors.Red;
            //markers.MarkerStyle.LineWidth = 2;

            var marker = myPlot.Add.Marker(markerX[0], markerY[0]);

            marker.Axes.YAxis = myPlot.Axes.Right;

            marker.MarkerStyle.Shape = MarkerShape.FilledDiamond; // Например, крестик
            marker.MarkerStyle.Size = 8;
            marker.MarkerStyle.LineColor = ScottPlot.Colors.Red;
            marker.MarkerStyle.LineWidth = 2;


            myPlot.SavePng("candles_with_markers.png", 800, 600);


            // configure the grid to display ticks from the right Y axis
            // plt.Grid.YAxis = plt.Axes.Right;
            // plt.Axes.Left.IsVisible = false;

            //CreateHighlowline(HighLowLine);

            //var max = candles.MaxPrice();
            //var min = candles.MinPrice();
            //maxDayliLine = plt.Add.HorizontalLine(max);
            //minDayliLine = plt.Add.HorizontalLine(min);

            //maxDayliLine.Axes.YAxis = plt.Axes.Right;
            //minDayliLine.Axes.YAxis = plt.Axes.Right;

            //hilowSpan = plt.Add.HorizontalSpan(min, max, ScottPlot.Colors.LightGreen);
            //hilowSpan.Axes.YAxis = plt.Axes.Right;

            // style the right axis as desired
            //WpfPlot1.Plot.Axes.Right.Label.Text = "Hello, Right Axis";
            //WpfPlot1.Plot.Axes.Right.Label.FontSize = 18;

            // it is recommended to remove tick generators from unused axes
            //plt.Axes.Left.RemoveTickGenerator();

            // pass in the custom axis when calling SetLimits()
            //WpfPlot1.Plot.Axes.SetLimitsY(bottom: -2, top: 2, yAxis: WpfPlot1.Plot.Axes.Right);

            // plt.Axes.DateTimeTicksBottom();


            Plot.Refresh();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Plotchart();
        }
    }
}
