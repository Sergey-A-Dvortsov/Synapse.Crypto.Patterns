using ScottPlot;
using ScottPlot.Colormaps;
using ScottPlot.Interactivity.UserActions;
using ScottPlot.Palettes;
using ScottPlot.Plottables;
using ScottPlot.WPF;
using Synapse.General;
using Synapse.Crypto.Bybit;
using Synapse.Crypto.Trading;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace Synapse.Crypto.Patterns
{
    public class StepViewModel: BaseViewModel
    {
        private AppRoot root = AppRoot.GetInstance();

        private List<Candle> candles;
        private Candle curCandle;
        private Candle selectedCandle;
        private Plot plt;
        private WpfPlotMenu menu;
        private Crosshair ch;
        private List<Candle> chartcandles;

        private ChartTypes chartType;

        public StepViewModel(WpfPlot plot, MasterTableItem item, ChartTypes chartType = ChartTypes.Сandlesticks) 
        {
            Plot = plot;
            plt = Plot.Plot;
            ScreenItem = item;
            this.chartType = chartType;
            NotifyPropertyChanged(nameof(Plot));

            //if (root.TimeFrame == Bybit.TimeFrames.Hour)
            //    candles = [.. root.Candles[item.Symbol].ToHourInterval()];
            //else
            //    candles = [.. root.Candles[item.Symbol]];
            Symbol = item.Symbol;

            SetCandles();

            FirstTime = candles.First().OpenTime.AddDays(LoadDays);
            LastTime = candles.Last().OpenTime;
            Time = FirstTime;

            SetChartCandles();

        }

        public DateSpinViewModel DateSpinViewModel { set; get;}

        WpfPlot Plot { get; }

      //  public ICollectionView ItemsView => CollectionViewSource.GetDefaultView(ScreenItem.CandleMarkups);

        private MasterTableItem _screenItem;
        public MasterTableItem ScreenItem
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

        private Bybit.TimeFrames _timeFrame = Bybit.TimeFrames.Hour;
        public Bybit.TimeFrames TimeFrame
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

        private DateTime _lastTime;
        public DateTime  LastTime
        {
            get => _lastTime;
            set 
            { 
                _lastTime = value;
                NotifyPropertyChanged();    
            }
        }

        private DateTime _firstTime;
        public DateTime FirstTime
        {
            get => _firstTime;
            set
            {
                _firstTime = value;
                NotifyPropertyChanged();
            }
        }

        private DateTime _time;
        public DateTime Time
        {
            get => _time;
            set
            {
                _time = value;
                NotifyPropertyChanged();
                SetChartCandles();
                PlotCoinChart();
            }
        }

        private int _loadDays = 3;
        public int LoadDays
        {
            get => _loadDays;
            set
            {
                _loadDays = value;
                NotifyPropertyChanged();
            }
        }

        private CandleMarkup _lastMarkup;
        public CandleMarkup LastMarkup
        {
            get => _lastMarkup;
            set
            {
                _lastMarkup = value;
                NotifyPropertyChanged();
            }
        }

        public CandleViewModel CandleViewModel { private set; get; } = new CandleViewModel();

        private void PlotCoinChart()
        {
            plt.Clear();

            var cndls = plt.Add.Candlestick(chartcandles.ToOHLC());

            // configure the plottable to use the right Y axis
            cndls.Axes.YAxis = plt.Axes.Right;

            // configure the grid to display ticks from the right Y axis
            plt.Grid.YAxis = plt.Axes.Right;
            plt.Axes.Left.IsVisible = false;

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

            //ch.VerticalLine.Text = $"{mouseCoordinates.X:N3}";
            //ch.HorizontalLine.Text = $"{mouseCoordinates.Y:N3}";

            Plot.MouseRightButtonDown += Plot_MouseRightButtonDown;
            Plot.MouseRightButtonUp += Plot_MouseRightButtonUp;
            Plot.MouseMove += Plot_MouseMove;

            // move the crosshair to track the cursor
           //Plot.MouseMove += (s, e) => {

           //     System.Windows.Point p = e.GetPosition(Plot);
           //     Pixel mousePixel = new(p.X * Plot.DisplayScale, p.Y * Plot.DisplayScale);
           //     Coordinates coordinates = plt.GetCoordinates(mousePixel, plt.Axes.Bottom, plt.Axes.Right);

           //     ch.Position = coordinates;
           //     var tm = DateTime.FromOADate(ch.Position.X).Round(TimeSpan.FromMinutes((int)root.TimeFrame));

           //     ch.VerticalLine.LabelText = $"{tm:HH:mm}";

           //     var fmt = "{0:F" + ScreenItem.Security.PriceScale + "}";

           //     ch.HorizontalLine.LabelText = string.Format(fmt, ch.Position.Y);

           //     curCandle = candles.FirstOrDefault(c => c.OpenTime == tm);

           //     CandleViewModel.Candle = curCandle;

           //     Plot.Refresh();
           // };
            Plot.Refresh();
        }

        // создает базовый набор свечей, с заданными таймфреймом и числом баров
        private void SetCandles()
        {
            if (TimeFrame == Bybit.TimeFrames.Hour)
                candles = [.. root.Candles[Symbol].ToHourInterval()];
            else
                candles = [.. root.Candles[Symbol]];
        }

        #region mouse

        private Candle GetCandleFromMousePosition(System.Windows.Input.MouseEventArgs e)
        {
            System.Windows.Point p = e.GetPosition(Plot);
            Pixel mousePixel = new(p.X * Plot.DisplayScale, p.Y * Plot.DisplayScale);
            Coordinates coordinates = plt.GetCoordinates(mousePixel, plt.Axes.Bottom, plt.Axes.Right);
            var tm = DateTime.FromOADate(coordinates.X).Round(TimeSpan.FromMinutes((int)root.TimeFrame));
            var candle = candles.FirstOrDefault(c => c.OpenTime == tm);
            return candle;
        }

        private void Plot_MouseMove(object s, System.Windows.Input.MouseEventArgs e)
        {
            System.Windows.Point p = e.GetPosition(Plot);
            Pixel mousePixel = new(p.X * Plot.DisplayScale, p.Y * Plot.DisplayScale);
            Coordinates coordinates = plt.GetCoordinates(mousePixel, plt.Axes.Bottom, plt.Axes.Right);

            ch.Position = coordinates;
            var tm = DateTime.FromOADate(ch.Position.X).Round(TimeSpan.FromMinutes((int)root.TimeFrame));

            ch.VerticalLine.LabelText = $"{tm:HH:mm}";

            var fmt = "{0:F" + ScreenItem.Security.PriceScale + "}";

            ch.HorizontalLine.LabelText = string.Format(fmt, ch.Position.Y);

            curCandle = candles.FirstOrDefault(c => c.OpenTime == tm);

            CandleViewModel.Candle = curCandle;

            Plot.Refresh();
        }

        private void Plot_MouseRightButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
           // selectedCandle = GetCandleFromMousePosition(e);
        }
      
        private void Plot_MouseRightButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
           selectedCandle = GetCandleFromMousePosition(e);
        }

        #endregion

        #region menu

 

        #endregion

        #region HeikenAshi menu

 

        #endregion

        #region commands

        //public DelegateCommand OpenStepWndCommand { private set; get; }

        //private void OnOpenStepWnd(object obj)
        //{
        //   root.SaveMarkupForScreenItem(ScreenItem);
        //}

        //private bool CanSaveMarkup(object obj)
        //{
        //    return ScreenItem.CandleMarkups.Any();
        //}

        #endregion

        private void SetChartCandles()
        {
            chartcandles = [.. candles.Where(c => c.OpenTime.Date >= Time.AddDays(-LoadDays).Date && c.OpenTime.Date <= Time.Date)];
        }

        public void OnLoaded(object sender, RoutedEventArgs e)
        {
            PlotCoinChart();
        }

        public void OnUnloaded(object sender, RoutedEventArgs e)
        {
            Plot.MouseRightButtonDown -= Plot_MouseRightButtonDown;
            Plot.MouseRightButtonUp -= Plot_MouseRightButtonUp;
            Plot.MouseMove -= Plot_MouseMove;
        }

        public void OnClosing(object sender, CancelEventArgs e)
        {
            if (!ScreenItem.CandleMarkups.Any()) return;

            if (MessageBox.Show(" Сохранить маркировку? ","", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                root.SaveMarkupForScreenItem(ScreenItem);
            }
        }

    }
}
