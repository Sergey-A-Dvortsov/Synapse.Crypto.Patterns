using ScottPlot;
using ScottPlot.Colormaps;
using ScottPlot.Interactivity.UserActions;
using ScottPlot.Palettes;
using ScottPlot.Plottables;
using ScottPlot.WPF;
using Synapse.General;
using Synapse.Crypto.Bybit;
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
using Synapse.Crypto.Trading;

namespace Synapse.Crypto.Patterns
{
    public class MarkupViewModel: BaseViewModel
    {
        private readonly AppRoot root = AppRoot.GetInstance();

        private List<Candle> candles;
        private Candle curCandle;
        private Candle selectedCandle;
        private DateTime lastTime;
        private DateTime firstTime;
        private Plot plt;
        private WpfPlotMenu menu;
        private Crosshair ch;
        private ChartTypes chartType;

        public MarkupViewModel(WpfPlot plot, MasterTableItem item, ChartTypes chartType = ChartTypes.Сandlesticks) 
        {
            Plot = plot;
            plt = Plot.Plot;
            ScreenItem = item;
            this.chartType = chartType;
            NotifyPropertyChanged(nameof(Plot));

            if (root.TimeFrame == TimeFrames.Hour)
                candles = root.Candles[item.Symbol].ToHourInterval().TakeLast(root.LoadedBars).ToList();
            else
                candles = root.Candles[item.Symbol].TakeLast(root.LoadedBars).ToList();

            firstTime = candles.First().OpenTime;
            lastTime = candles.Last().OpenTime;

            menu = (WpfPlotMenu)Plot.Menu;
            menu.Clear();

            if(chartType == ChartTypes.Сandlesticks)
            {
                menu.Add("Молот > вниз", AddHummerDown);
                menu.Add("Волчок > вниз", AddSpinningTopDown);
                menu.Add("Додж > вниз", AddDojiDown);
                menu.AddSeparator();
                menu.Add("Молот > вверх", AddHummerUp);
                menu.Add("Волчок > вверх", AddSpinningTopUp);
                menu.Add("Додж > вверх", AddDojiUp);
            }
            else if(chartType == ChartTypes.HeikenAchi)
            {
                menu.Add("Trend", AddTrend);
                menu.Add("Small", AddSmall);
                menu.Add("Doji", AddDoji);
                menu.Add("SpinningTop", AddSpinningTop);
                menu.Add("Hummer", AddHummer);
                menu.Add("Indefinably", AddIndefinably);
            }

            SaveMarkupCommand = new DelegateCommand(OnSaveMarkup, CanSaveMarkup);

        }

        WpfPlot Plot { get; }

        public ICollectionView ItemsView => CollectionViewSource.GetDefaultView(ScreenItem.CandleMarkups);

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

        private void PlotChart()
        {
            

            var cndls = plt.Add.Candlestick(candles.ToOHLC());

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

        public void AddHummerDown(Plot plot)
        {
            AddMarkup(CandlePatterns.Hummer, MarkupDirection.Down);
        }

        private void AddSpinningTopDown(Plot plot)
        {
            AddMarkup(CandlePatterns.SpinningTop, MarkupDirection.Down);
        }

        public void AddDojiDown(Plot plot)
        {
            AddMarkup(CandlePatterns.Doji, MarkupDirection.Down);
        }

        public void AddHummerUp(Plot plot)
        {
            AddMarkup(CandlePatterns.Hummer, MarkupDirection.Up);
        }

        private void AddSpinningTopUp(Plot plot)
        {
            AddMarkup(CandlePatterns.SpinningTop, MarkupDirection.Up);
        }

        public void AddDojiUp(Plot plot)
        {
            AddMarkup(CandlePatterns.Doji, MarkupDirection.Up);
        }

        private void AddMarkup(CandlePatterns pattern, MarkupDirection direction)
        {
            var tm = selectedCandle.OpenTime;

            if (tm == DateTime.MinValue || tm < firstTime || tm > lastTime)
            {
                MessageBox.Show($" Некорректное время свечи {tm}. ", "", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }
            var cm = ScreenItem.CandleMarkups.FirstOrDefault(si => si.Time == tm);

            var newcm = new CandleMarkup()
            {
                Time = tm,
                Pattern = pattern,
                Direction = direction
            };

            if (cm.Time == DateTime.MinValue)
                ScreenItem.CandleMarkups.Add(newcm);
            else
            {
                var ind = ScreenItem.CandleMarkups.IndexOf(cm);
                if (ind > -1)
                {
                    ScreenItem.CandleMarkups[ind] = newcm;
                }
                //cm = newcm;
            }

            LastMarkup = newcm;

            ItemsView.Refresh();

        }

        #endregion

        #region HeikenAshi menu

        public void AddTrend(Plot plot)
        {
            AddMarkup(CandlePatterns.TrendCandle);
        }

        public void AddSmall(Plot plot)
        {
            AddMarkup(CandlePatterns.SmallCandle);
        }

        public void AddDoji(Plot plot)
        {
            AddMarkup(CandlePatterns.Doji);
        }

        private void AddSpinningTop(Plot plot)
        {
            AddMarkup(CandlePatterns.SpinningTop);
        }

        public void AddHummer(Plot plot)
        {
            AddMarkup(CandlePatterns.Hummer);
        }

        public void AddIndefinably(Plot plot)
        {
            AddMarkup(CandlePatterns.Indefinably);
        }

        private void AddMarkup(CandlePatterns pattern)
        {
            var tm = selectedCandle.OpenTime;

            if (tm == DateTime.MinValue || tm < firstTime || tm > lastTime)
            {
                MessageBox.Show($" Некорректное время свечи {tm}. ", "", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }
            var cm = ScreenItem.CandleMarkups.FirstOrDefault(si => si.Time == tm);

            var newcm = new CandleMarkup()
            {
                Time = tm,
                Pattern = pattern
            };

            if (cm.Time == DateTime.MinValue)
                ScreenItem.CandleMarkups.Add(newcm);
            else
            {
                var ind = ScreenItem.CandleMarkups.IndexOf(cm);
                if (ind > -1)
                {
                    ScreenItem.CandleMarkups[ind] = newcm;
                }
                //cm = newcm;
            }

            LastMarkup = newcm;

            ItemsView.Refresh();

        }


        #endregion

        #region commands

        public DelegateCommand SaveMarkupCommand { private set; get; }

        private void OnSaveMarkup(object obj)
        {
           root.SaveMarkupForScreenItem(ScreenItem);
        }

        private bool CanSaveMarkup(object obj)
        {
            return ScreenItem.CandleMarkups.Any();
        }

        #endregion

        public void OnLoaded(object sender, RoutedEventArgs e)
        {
            PlotChart();
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
