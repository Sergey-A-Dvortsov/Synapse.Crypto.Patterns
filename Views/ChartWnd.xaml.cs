using ScottPlot;
using ScottPlot.Interactivity.UserActionResponses;
using ScottPlot.Plottables;
using ScottPlot.WPF;
using Synapse.Crypto.Bybit;
using Synapse.Crypto.Trading;
using Synapse.General;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
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
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Synapse.Crypto.Patterns
{
   

    /// <summary>
    /// Логика взаимодействия для ChartWnd.xaml
    /// </summary>
    public partial class ChartWnd : Window, INotifyPropertyChanged
    {
        private static AppRoot root = AppRoot.GetInstance();

        // базовый набор японских свечей, на основе, которого создаются другие наборы, которые непосредственно отображаются на графике
        private List<Candle> candles;

        // набор свечей, который непосредственно отображается на графике
        private List<Candle> chartcandles;

        private Plot plt;
        private WpfPlotMenu menu;
        private Crosshair ch;

        private HorizontalSpan? rangeSpan;   
        //private HorizontalSpan prevDaySpan;

        private HorizontalLine? maxRangeLine;
        private HorizontalLine? minRangeLine;

        private CoordinateLine line;
        

        public ChartWnd()
        {
            InitializeComponent();
            RangeCommand = new DelegateCommand(OnRange, CanRange);
        }

        public ChartWnd(ScreenItem item, bool highLowLine) : this()
        {
            plt = Plot.Plot;
            ScreenItem = item;
            Symbol = item.Symbol;
            //SetCandles();
            HighLowLine = highLowLine;
            menu = (WpfPlotMenu)Plot.Menu;
            menu.Clear();
            menu.Add("Item1", DoMenuItem1);
            
        }

        public CandleViewModel CandleViewModel { private set; get; } = new CandleViewModel();

        #region properties

        private ScreenItem _screenItem;
        public ScreenItem ScreenItem
        {
            get => _screenItem;
            set
            {
                _screenItem = value;
                NotifyPropertyChanged();
            }
        }

        private string _symbol;
        public string Symbol 
        { 
            get => _symbol; 
            set 
            { 
                _symbol = value; 
                NotifyPropertyChanged();
            } 
        }

        private int _loadedBars = root.LoadedBars;
        public int LoadedBars
        {
            get { return _loadedBars; }
            set
            {
                _loadedBars = value;
                NotifyPropertyChanged();
            }
        }

        private TimeFrames _timeFrame = TimeFrames.Hour;
        public TimeFrames TimeFrame
        {
            get { return _timeFrame; }
            set
            {
                _timeFrame = value;
                NotifyPropertyChanged();
                PlotCoinChart();
            }
        }

        private ChartTypes _chartType = ChartTypes.Сandlesticks;
        public ChartTypes ChartType
        {
            get { return _chartType; }
            set
            {
                _chartType = value;
                NotifyPropertyChanged();
                PlotCoinChart();
            }
        }

        private bool _highLowLine;
        public bool HighLowLine
        {
            get => _highLowLine;
            set
            {
                _highLowLine = value;
                CreateHighlowline(value);
                NotifyPropertyChanged();
            }
        }

        private Candle _currentCandle;
        public Candle CurrentCandle
        {
            get => _currentCandle;
            set
            {
                if(value.OpenTime == DateTime.MinValue) return;
                _currentCandle = value;
                NotifyPropertyChanged();
            }
        }

        #endregion

        #region commands

        public DelegateCommand RangeCommand { get; private set; }

        private void OnRange(object arg)
        {
            RangeIntervals? interval = null; 

            switch (arg.ToString())
            {
                case "One" :
                    {
                        interval = RangeIntervals.Day;
                        break;
                    }
                case "Three":
                    {
                        interval = RangeIntervals.Three;
                        break;
                    }
                case "Week":
                    {
                        interval = RangeIntervals.Week;
                        break;
                    }
                case "Month":
                    {
                        interval = RangeIntervals.Month;
                        break;
                    }
                case "Clear":
                    {
                        interval = null;
                        break;
                    }
                default:
                    break;
            }

            CreateRangeLines(interval);
            CreateRangeSpan(interval);

        }



        private bool CanRange(object arg)
        {
            switch (arg.ToString())
            {
                case "One":
                    {
                        break;
                    }
                case "Three":
                    {
                        break;
                    }
                case "Week":
                    {
                        break;
                    }
                case "Month":
                    {
                        break;
                    }
                case "Clear":
                    {
                        return maxRangeLine != null && minRangeLine != null;
                    }
                default:
                    break;
            }

            return true;

        }

        #endregion

        private void PlotMarketBreth()
        {

            var plt = Plot.Plot;

            double[] xs = null;
            double[] ys = null;

            // xs = avrrates.Select(x => x.Time.ToOADate()).ToArray();
            //ys = avrrates.Select(r => r.Value).ToArray();

            plt.Add.HorizontalLine(0, 2, ScottPlot.Colors.Red);

            plt.Axes.DateTimeTicksBottom();

            //var yz = zz.Select(r => r.Value).ToArray();
            //var xz = zz.Select(x => x.Time.ToOADate()).ToArray();

            plt.Add.Scatter(xs, ys);

            Plot.Refresh();
        }

        private void PlotCryptoIndexes()
        {                                                                          

            var plt = Plot.Plot;

            double[] xs = null;
            double[] ys = null;

            //xs = root.Indexes.Select(x => x.Time.ToOADate()).ToArray();
            //ys = root.Indexes.Select(r => r.Value).ToArray();

            ////plt.Add.HorizontalLine(0, 2, ScottPlot.Colors.Red);

            //plt.Axes.DateTimeTicksBottom();

            ////var yz = zz.Select(r => r.Value).ToArray();
            ////var xz = zz.Select(x => x.Time.ToOADate()).ToArray();

            //plt.Add.Scatter(xs, ys);

            Plot.Refresh();
        }

        private void PlotCoinChart()
        {
            plt.Clear();

           SetCandles();

            if (ChartType == ChartTypes.Сandlesticks)
                chartcandles = candles;
            else if(ChartType == ChartTypes.HeikenAchi)
                chartcandles = candles.ToHeikinAshi();

            var ohlcPlot =  plt.Add.Candlestick(chartcandles.ToOHLC());

            // configure the plottable to use the right Y axis
             ohlcPlot.Axes.YAxis = plt.Axes.Right;

            // configure the grid to display ticks from the right Y axis
            plt.Grid.YAxis = plt.Axes.Right;
            plt.Axes.Left.IsVisible = false;

            //CreateHighlowline(HighLowLine);

           // CreatePrevDaySpan();


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

            plt.Axes.DateTimeTicksBottom();


            ch = plt.Add.Crosshair(0, 0);
            ch.Axes.XAxis = plt.Axes.Bottom;
            ch.Axes.YAxis = plt.Axes.Right;

            //ch.VerticalLine.LabelOffsetY = 3;
            ch.VerticalLine.LabelBorderWidth = 1;
            ch.VerticalLine.LabelBorderColor = ScottPlot.Color.FromColor(System.Drawing.Color.Gray);
            ch.VerticalLine.LabelBackgroundColor = ScottPlot.Color.FromColor(System.Drawing.Color.LightGray);

            ch.HorizontalLine.LabelOppositeAxis = true;
            ch.HorizontalLine.LabelRotation = 0;
            ch.HorizontalLine.IsVisible = true;
            ch.HorizontalLine.LabelAlignment = Alignment.MiddleRight;
            ch.HorizontalLine.LabelOffsetY = 80;

            //ch.VerticalLine.Text = $"{mouseCoordinates.X:N3}";
            //ch.HorizontalLine.Text = $"{mouseCoordinates.Y:N3}";

            // move the crosshair to track the cursor
            MouseMove += (s, e) => {

                Point p = e.GetPosition(Plot);
                Pixel mousePixel = new(p.X * Plot.DisplayScale, p.Y * Plot.DisplayScale);
                Coordinates coordinates = plt.GetCoordinates(mousePixel, plt.Axes.Bottom, plt.Axes.Right);

                ch.Position = coordinates;
                var tm = DateTime.FromOADate(ch.Position.X).Round(TimeSpan.FromMinutes((int)TimeFrame));

                ch.VerticalLine.LabelText = $"{tm:HH:mm}";

                var fmt = "{0:F"+ScreenItem.Security.PriceScale+"}";

                ch.HorizontalLine.LabelText = string.Format(fmt, ch.Position.Y);

                CurrentCandle = chartcandles.FirstOrDefault(c => c.OpenTime == tm);

                CandleViewModel.Candle = CurrentCandle;

                Plot.Refresh();
            };

            Plot.Refresh();
        }

        // создает базовый набор свечей, с заданными таймфреймом и числом баров
        private void SetCandles()
        {
            if (TimeFrame == TimeFrames.Hour)
                candles = root.Candles[Symbol].ToHourInterval().TakeLast(LoadedBars).ToList();
            else
                candles = root.Candles[Symbol].TakeLast(LoadedBars).ToList();
        }

        private void CreateHighlowline(bool create)
        {
            if (create)
            {
                var max = candles.MaxPrice(); 
                var min = candles.MinPrice();
                maxRangeLine = plt.Add.HorizontalLine(max, 1, ScottPlot.Colors.Green);
                minRangeLine = plt.Add.HorizontalLine(min, 1, ScottPlot.Colors.Red);
                maxRangeLine.Axes.YAxis = plt.Axes.Right;
                minRangeLine.Axes.YAxis = plt.Axes.Right;
            }
            else 
            {
                plt.Remove(maxRangeLine);
                plt.Remove(minRangeLine);
            }

            Plot.Refresh();
        }

        private void CreateRangeLines(RangeIntervals? interval)
        {

            if(maxRangeLine != null)
            {
                plt.Remove(maxRangeLine);
                maxRangeLine = null;
            }

            if (minRangeLine != null)
            {
                plt.Remove(minRangeLine);
                minRangeLine = null;
            }

            if (interval != null)
            {
                var range = candles.GetIntervalRange(interval.GetValueOrDefault());

                if(range != null)
                {
                    maxRangeLine = plt.Add.HorizontalLine(range.Max, 1, ScottPlot.Colors.Green);
                    minRangeLine = plt.Add.HorizontalLine(range.Min, 1, ScottPlot.Colors.Red);
                    maxRangeLine.Axes.YAxis = plt.Axes.Right;
                    minRangeLine.Axes.YAxis = plt.Axes.Right;
                }

            }

            Plot.Refresh();
        }

        private void CreateRangeSpan(RangeIntervals? interval)
        {

            if (rangeSpan != null)
            {
                plt.Remove(rangeSpan);
                rangeSpan = null;
            }

            if (interval != null)
            {
                var range = candles.GetIntervalRange(interval.GetValueOrDefault());

                if (range != null)
                {
                    double X1 = range.Start.ToOADate();
                    double X2 = range.End.ToOADate();
                    rangeSpan = plt.Add.HorizontalSpan(X1, X2, ScottPlot.Colors.LightGreen.WithAlpha(0.1));
                }

            }

            Plot.Refresh();

        }


        //private void CreatePrevDaySpan()
        //{
        //    double X1 = DateTime.UtcNow.PvevAODayStart();
        //    double X2 = DateTime.UtcNow.PvevAODayEnd();
        //    prevDaySpan = plt.Add.HorizontalSpan(X1, X2, ScottPlot.Colors.LightGreen.WithAlpha(0.1));
        //}

        //CoordinateLine line = Generate.RandomLine();
        //WpfPlot1.Plot.Add.InteractiveLineSegment(line);


        public void DoMenuItem1(Plot plot)
        {
            //TODO
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            PlotCoinChart();
        }

        private void Window_Unloaded(object sender, RoutedEventArgs e)
        {
            //var f = 5;
        }



        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

    }
}




////Mouse Tracker
//crosshair = _plots[0].Add.Crosshair(0, 0);
//crosshair.TextColor = Colors.White;
//crosshair.TextBackgroundColor = crosshair.HorizontalLine.Color;
//crosshair.Axes.XAxis = _plots[0].Axes.Bottom;
//crosshair.Axes.YAxis = _plots[0].Axes.Left;
//crosshair.HorizontalLine.LabelOppositeAxis = false;
////crosshair.HorizontalLine.LabelAlignment = Alignment.MiddleRight;
//crosshair.HorizontalLine.LabelRotation = 0;
//crosshair.HorizontalLine.LabelBorderRadius = 10;
//crosshair.HorizontalLine.IsVisible = true;
//crosshair.HorizontalLine.LineWidth = 1;

//// Configurar a linha vertical do crosshair
//crosshair.VerticalLine.IsVisible = true;
//crosshair.VerticalLine.LabelBorderRadius = 10;
//crosshair.VerticalLine.LineWidth = 1;