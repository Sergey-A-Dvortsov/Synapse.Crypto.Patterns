using MathNet.Numerics;
using NUnit.Framework.Internal.Execution;
using ScottPlot;
using ScottPlot.Interactivity;
using ScottPlot.Plottables;
using ScottPlot.Plottables.Interactive;
using ScottPlot.TickGenerators.Financial;
using ScottPlot.WPF;
using Synapse.Crypto.Bybit;
using Synapse.Crypto.Trading;
using Synapse.General;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace Synapse.Crypto.Patterns
{

    /// <summary>
    /// Выбранная свеча. Выбирается при помощи левого щелчка, выделяется маркерами. Отмена выбора - повторный левый щелчкок. 
    /// </summary>
    public class SelectedCandle
    {
        /// <summary>
        /// Индекс в списке отображаемых свечей на графике
        /// </summary>
        public int Index { set; get; }
        /// <summary>
        /// Свеча
        /// </summary>
        public Candle Candle { set; get; }
        /// <summary>
        /// Маркеры выделения
        /// </summary>
        public Markers? Markers { set; get; }
    }

    public class SimulatorViewModel : BaseViewModel
    {
       

        public const string NEXTBAR = "Следующий бар";
        public const string TS = "Тейк/стоп";
        public const string RANGEBREAK = "Пробой диапазона";
        public const string SLOPEBREAK = "Пробой наклонной";

        private const double SNAP_Y_THRESHOLD = 0.005; 

        private AppRoot root = AppRoot.GetInstance();
        private readonly BybitSecurity security;
        private List<Candle> candles;
        private List<Candle> displaycandles;
        //private List<Candle> selectedCandles = [];

       
        private readonly Plot plt;
        private CandlestickPlot? cndlPlot;
        private List<OHLC>? oHLCs;
        private List<SelectedCandle> selectedCandles = [];

        //private Scatter? selectedScatter;
        //private Marker? selectedCandleMarker;
        //private List<Markers?> candleMarkers = []; // хранит маркеры выбранных свечей

        private Crosshair ch; // перекрестье
        private WpfPlotMenu menu;
        private HorizontalSpan? rangeSpan; // выделяет интервал, по экстремумам которого строится диапазон
        private HorizontalLine? maxRangeLine; // верхняя линия диапазона
        private HorizontalLine? minRangeLine; // нижняя линия диапазона

        private HorizontalLine? takeLine; // верхняя линия диапазона
        private HorizontalLine? stopLine; // нижняя линия диапазона

        private HorizontalLine draggedLine; // вспомогательные объект для отслеживания перетаскивания
        private double grabVrange = 0.01;   // зона захвата по вертикальной координате.
                                            // Захват возникает, если разность между вертикальными координатами курсора и объекта меньше 0.01%

        private InteractiveLineSegment interactiveSegment;

        private InteractiveLine interactiveLine;

        private InteractiveHorizontalLine interactiveHLine;

        private TrendLine trendLine;

        //WpfPlot1.Plot.Add.InteractiveHorizontalLineSegment(x1, x2, y);

        //  private InteractiveHorizontalLineSegment takeLine

        //CoordinateLine line = Generate.RandomLine();
        //WpfPlot1.Plot.Add.InteractiveLineSegment(line);

        private SlopeLine? slope;

        private IYAxis yaxis;

        private Edge yedge = Edge.Left;

        private readonly List<IPlottable> elementsForDelete = [];

        public SimulatorViewModel(WpfPlot plot, MasterTableItem item)
        {
            Plot = plot;
            plt = plot.Plot;
            Symbol = item.Symbol;
            security = item.Security;

            SetStartTimeCommand = new DelegateCommand(OnSetStartTime, CanSetStartTime);
            NextCommand = new DelegateCommand(OnNext, CanNext);
            RangeCommand = new DelegateCommand(OnRange, CanRange);
            LinesCommand = new DelegateCommand(OnLines, CanLines);
            PlotSlopeCommand = new DelegateCommand(OnPlotSlope, CanPlotSlope);
            DeleteElementCommand = new DelegateCommand(OnDeleteElement, CanDeleteElement);

            candles = root.Candles[item.Symbol];
            GoToItems = [NEXTBAR, TS, RANGEBREAK, SLOPEBREAK];
            SetStartTime();
            Trading = new([.. candles], security);
            menu = (WpfPlotMenu)Plot.Menu;

            yaxis = yedge == Edge.Left ? plt.Axes.Left : plt.Axes.Right;

            Instance = this;
            // plt.Add.InteractiveHorizontalLine(4);

        }

        public static SimulatorViewModel Instance { get; private set; }

         public WpfPlot Plot { get; }

        public CandleViewModel CandleViewModel { private set; get; } = new CandleViewModel();

        public SimulateTradingViewModel Trading { private set; get; }

        /// <summary>
        /// Свеча под перекрестьем курсора
        /// </summary>
        public Candle CrosshairCandle { private set; get; }

        /// <summary>
        /// Текущая выбранная свеча. Выбирается щелчком мыши. Текущей всегда является свеча, выбранная последней.  
        /// </summary>
        public SelectedCandle SelectedCandle { private set; get; }

        /// <summary>
        /// Текущая свеча (крайняя правая свеча на графике)
        /// </summary>
        public Candle CurrentCandle { private set; get; }

        private bool _crosshairOn;
        /// <summary>
        /// Включить/выключить перекрестье
        /// </summary>
        public bool CrosshairOn
        {
            get => _crosshairOn;
            set
            {
                _crosshairOn = value;
                NotifyPropertyChanged();
                if (_crosshairOn)
                    CreateCrosshair();
                else
                    DeleteCrosshair();
            }
        }

        private bool _isSlope;
        /// <summary>
        /// Включить/выключить перекрестье
        /// </summary>
        public bool IsSlope
        {
            get => _isSlope;
            set
            {
                _isSlope = value;
                NotifyPropertyChanged();
            }
        }

        #region properties

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

        private TimeFrames _timeFrame = TimeFrames.Min15;
        /// <summary>
        /// Интервал графика
        /// </summary>
        public TimeFrames TimeFrame
        {
            get => _timeFrame;
            set
            {
                if (Simulating) return; //Запрещено менять TimeFrame во время симуляции
                _timeFrame = value;
                NotifyPropertyChanged();
            }
        }

        private ChartTypes _chartType = ChartTypes.Сandlesticks;
        /// <summary>
        /// Интервал графика
        /// </summary>
        public ChartTypes ChartType
        {
            get => _chartType;
            set
            {
                _chartType = value;
                NotifyPropertyChanged();
            }
        }

        private DateTime _time;
        /// <summary>
        /// "Текущее" время симулятора (время текущей свечи) 
        /// </summary>
        public DateTime Time
        {
            get => _time;
            set
            {
                _time = value;
                NotifyPropertyChanged();
            }
        }

        private DateTime _startTime;
        /// <summary>
        /// Начало симуляции
        /// </summary>
        public DateTime StartTime
        {
            get => _startTime;
            set
            {
                _startTime = value;
                NotifyPropertyChanged();
            }
        }

        private DateTime _endTime;
        /// <summary>
        /// Последняя потенциальная свеча в симуляции
        /// </summary>
        public DateTime EndTime
        {
            get => _endTime;
            set
            {
                _endTime = value;
                NotifyPropertyChanged();
            }
        }

        public DateTime FirstTime { private set; get; }

        private int _startBackground = 14;
        /// <summary>
        /// Предыстория (число дней)
        /// </summary>
        public int StartBackground
        {
            get => _startBackground;
            set
            {
                _startBackground = value;
                NotifyPropertyChanged();
            }
        }

        private int _workBackground = 30;
        /// <summary>
        /// Предыстория (число дней)
        /// </summary>
        public int WorkBackground
        {
            get => _workBackground;
            set
            {
                _startBackground = value;
                NotifyPropertyChanged();
            }
        }

        private bool _simulating;
        /// <summary>
        /// Истина - идет симуляция
        /// </summary>
        public bool Simulating
        {
            get => _simulating;
            set
            {
                _simulating = value;
                NotifyPropertyChanged();
            }
        }

        private RangeIntervals _rangeInterval = RangeIntervals.None;
        /// <summary>
        /// Интервал, который импользовался для определения уровней
        /// </summary>
        public RangeIntervals RangeInterval
        {
            get => _rangeInterval;
            set
            {
                _rangeInterval = value;
                NotifyPropertyChanged();
                CreateRangeLines(RangeInterval);
                CreateRangeSpan(RangeInterval);
            }
        }

        private BreakStyles _rangeBreakStyle;
        /// <summary>
        /// Как должен быть пробит уровень
        /// </summary>
        public BreakStyles RangeBreakStyle
        {
            get => _rangeBreakStyle;
            set
            {
                _rangeBreakStyle = value;
                NotifyPropertyChanged();
            }
        }

        private BreakStyles _slopeBreakStyle;
        /// <summary>
        /// Как должен быть пробита наклонная линия
        /// </summary>
        public BreakStyles SlopeBreakStyle
        {
            get => _slopeBreakStyle;
            set
            {
                _slopeBreakStyle = value;
                NotifyPropertyChanged();
            }
        }

        public List<string> GoToItems { private set; get; }

        private string _goToItem = NEXTBAR;
        /// <summary>
        /// Место, куда нужно перейти после нажатия кнопки "Next".
        /// В обычном варианте 
        /// 1. "Следующий бар" вы перейдете на следующую свечу или через несколько свечей, если установите шаг > 1.
        /// Но также возможны следующие варианты
        /// 2. "Тейк/стоп", если была открыта позиция, можно перейти к свече, на которой сработал тейк или стоп
        /// 3. "Пробой диапазона", если установлен "диапзон", то к месту пробоя диапазона
        /// 4. "Пробой наклонной", если установлена наклонная, то к месту пробоя наклонной
        /// </summary>
        public string GoToItem
        {
            get => _goToItem;
            set
            {
                _goToItem = value;
                NotifyPropertyChanged();
            }
        }

        private int _steps = 1;
        /// <summary>
        /// На сколько баров (свечей) произойдет перемещение после нажатия кнопки "Next".
        /// </summary>
        public int Steps
        {
            get => _steps;
            set
            {
                _steps = value;
                NotifyPropertyChanged();
            }
        }

        #endregion

        #region commands

        public DelegateCommand SetStartTimeCommand { get; set; }

        private void OnSetStartTime(object arg)
        {
            if (arg.ToString() == "Begin")
            {
                //TODO
            }
            else if (arg.ToString() == "Random")
            {
                //TODO
            }
        }

        private bool CanSetStartTime(object arg)
        {
            return true;
        }

        //public DelegateCommand ChangeSideCommand { get; set; }

        //private void OnChangeSide(object arg)
        //{
        //    Side = Side == Sides.Buy ? Sides.Sell : Sides.Buy;
        //    if (Side == Sides.Sell)
        //       SideColor = new SolidColorBrush(System.Windows.Media.Colors.LightPink);
        //    else SideColor = new SolidColorBrush(System.Windows.Media.Colors.LightGreen);
        //}

        //private bool CanChangeSide(object arg)
        //{
        //    return true;
        //}

        //public DelegateCommand MakeTradeCommand { get; set; }

        //private void OnMakeTrade(object arg)
        //{
        //    //TODO
        //}

        //private bool CanMakeTrade(object arg)
        //{
        //    return true;
        //}

        public DelegateCommand NextCommand { get; set; }

        private void OnNext(object arg)
        {
            int idx = 0;

            switch (GoToItem)
            {
                case NEXTBAR: //Перемещение на n баров, заданных в Steps

                    if (Trading?.Position?.State == PositionStates.Open)
                    {
                        idx = Trading.CheckForClose(CurrentCandle, Steps);
                        CurrentCandle = candles[idx];
                        Time = CurrentCandle.OpenTime;
                    }
                    else
                    {
                        Time += TimeSpan.FromMinutes((int)TimeFrame * Steps);
                        CurrentCandle = candles.FirstOrDefault(c => c.OpenTime == Time);
                    }

                    break;
                case TS: // Перемещение к месту тейка/стопа

                    if (Trading?.Position?.State != PositionStates.Open) return;

                    idx = Trading.CheckForClose(CurrentCandle, -1);
                    CurrentCandle = candles[idx];
                    Time = CurrentCandle.OpenTime;

                    break;
                case RANGEBREAK: // Перемещение к месту пробоя диапазона
                    if (Trading?.Position?.State == PositionStates.Open) return;

                    //TODO выполняется логика перемещения, функция перемещени должна вернуть индекс текущей свечи
                    //idx = Trading.CheckForClose(CurrentCandle, -1);
                    //CurrentCandle = candles[idx];
                    //Time = CurrentCandle.OpenTime;

                    break;
                case SLOPEBREAK: // Перемещение к месту пробоя наклонной
                    if (Trading?.Position?.State == PositionStates.Open) return;

                    //TODO выполняется логика перемещения, функция перемещени должна вернуть индекс текущей свечи
                    //idx = Trading.CheckForClose(CurrentCandle, -1);
                    //CurrentCandle = candles[idx];
                    //Time = CurrentCandle.OpenTime;

                    break;
                default:
                    break;
            }

            if (Trading?.Position?.State == PositionStates.Close)
                Trading.ClearPosition();

            Trading.CurrentCandle = CurrentCandle;
            SetDisplayCandles();
            Plotchart();
            RangeInterval = RangeInterval;

        }

        private bool CanNext(object arg)
        {
            var t = Time + TimeSpan.FromMinutes((int)TimeFrame * Steps);
            var e = EndTime;
            var r = (Time + TimeSpan.FromMinutes((int)TimeFrame * Steps)) <= EndTime;

            return (Time + TimeSpan.FromMinutes((int)TimeFrame * Steps)) <= EndTime;
        }

        public DelegateCommand RangeCommand { get; set; }

        private void OnRange(object arg)
        {

            switch (arg.ToString())
            {
                case "Day":
                    {
                        RangeInterval = RangeIntervals.Day;
                        break;
                    }
                case "Three":
                    {
                        RangeInterval = RangeIntervals.Three;
                        break;
                    }
                case "Week":
                    {
                        RangeInterval = RangeIntervals.Week;
                        break;
                    }
                case "Month":
                    {
                        RangeInterval = RangeIntervals.Month;
                        break;
                    }
                case "Clear":
                    {
                        RangeInterval = RangeIntervals.None;
                        break;
                    }
                default:
                    break;
            }



        }

        private bool CanRange(object arg)
        {
            switch (arg.ToString())
            {
                case "Day":
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

        public DelegateCommand LinesCommand { get; set; }

        private void OnLines(object arg)
        {
            switch (arg.ToString())
            {
                case "slope":
                    {
                        IsSlope = true;
                        break;
                    }
                case "slope_by_candles":
                    {
                        if (slope == null)
                        {
                            slope = new(selectedCandles[^2].Candle, selectedCandles[^1].Candle);
                            slope.SetMinMax(displaycandles);
                            slope.Line = plt.Add.Line(slope.GetCoordinateLine().GetValueOrDefault());
                            slope.Line.Axes.YAxis = yaxis;
                        }
                        else
                        {
                            plt.Remove(slope.Line);
                            slope = null;
                        }

                        break;
                    }
                default:
                    break;
            }

            Plot.Refresh();

        }

        private bool CanLines(object arg)
        {
            switch (arg.ToString())
            {
                case "slope":
                    {
                        break;
                    }
                case "slope_by_candles":
                    {   var isSelected = selectedCandles?.Count > 1 || slope != null;
                        return isSelected;
                    }
                default:
                    break;
            }
            return true;
        }

        public DelegateCommand PlotSlopeCommand { get; set; }

        private void OnPlotSlope(object arg)
        {
            if (slope == null)
            {
                slope = new(selectedCandles[^2].Candle, selectedCandles[^1].Candle);
                slope.SetMinMax(displaycandles);
                slope.Line = plt.Add.Line(slope.GetCoordinateLine().GetValueOrDefault());
                slope.Line.Axes.YAxis = yaxis;
            }
            else
            {
                plt.Remove(slope.Line);
                slope = null;
            }

            Plot.Refresh();

        }

        private bool CanPlotSlope(object arg)
        {
            return selectedCandles?.Count > 1 || slope != null;
        }

        public DelegateCommand DeleteElementCommand { get; set; }

        private void OnDeleteElement(object arg)
        {
            foreach(var element in elementsForDelete)
            {
                plt.Remove(element);
            }
            Plot.Refresh();
        }

        private bool CanDeleteElement(object arg)
        {
            return elementsForDelete.Any();
        }

        #endregion

        #region plot

        private void Plotchart()
        {

            plt.Clear();

            if (displaycandles == null) return;

            oHLCs = displaycandles.ToOHLC();

            cndlPlot = plt.Add.Candlestick(oHLCs);

            // configure the plottable to use the right Y axis
            cndlPlot.Axes.YAxis = yaxis;
            //cndlPlot.Sequential = true;
            plt.Axes.DateTimeTicksBottom();

            Plot.Refresh();
        }

        // создает перекрестье
        private void CreateCrosshair()
        {
            ch = plt.Add.Crosshair(0, 0);
            ch.Axes.XAxis = plt.Axes.Bottom;
            ch.Axes.YAxis = yaxis;

            //ch.VerticalLine.LabelOffsetY = 3;
            ch.VerticalLine.LabelBorderWidth = 1;
            ch.VerticalLine.LabelBorderColor = ScottPlot.Color.FromColor(System.Drawing.Color.Gray);
            ch.VerticalLine.LabelBackgroundColor = ScottPlot.Color.FromColor(System.Drawing.Color.LightGray);
            ch.VerticalLine.LabelFontSize = 11;
            ch.VerticalLine.LabelPadding = 2;
            ch.VerticalLine.LabelOffsetY = 3;
            ch.VerticalLine.LinePattern = LinePattern.DenselyDashed;
            ch.VerticalLine.LineColor = ScottPlot.Colors.CadetBlue;
            ch.VerticalLine.LineWidth = 0.5F;
            

            ch.HorizontalLine.LabelOppositeAxis = yaxis.Edge == Edge.Right ? true : false;

            ch.HorizontalLine.LabelBorderColor = ScottPlot.Colors.CadetBlue; 
            ch.HorizontalLine.LabelFontSize = 11;
            ch.HorizontalLine.LabelPadding = 1;
            ch.HorizontalLine.LabelBackgroundColor = ScottPlot.Colors.LightBlue.WithAlpha(200);

            //ch.HorizontalLine.Label.BackgroundColor. = ScottPlot.Colors.LightBlue;

            ch.HorizontalLine.LabelAlignment = Alignment.MiddleRight;

            ch.HorizontalLine.LabelOffsetX = -22;
            ch.HorizontalLine.LabelOffsetY = 5;

            ch.HorizontalLine.LabelRotation = 0;

            ch.HorizontalLine.LinePattern = LinePattern.DenselyDashed;
            ch.HorizontalLine.LineColor = ScottPlot.Colors.CadetBlue;

            ch.HorizontalLine.IsVisible = true;

            //ch.VerticalLine.Text = $"{mouseCoordinates.X:N3}";
            //ch.HorizontalLine.Text = $"{mouseCoordinates.Y:N3}";

        }

        // удаляет перекрестье
        private void DeleteCrosshair()
        {
            if (ch == null) return;
            plt.Remove(ch);
            ch = null;
            Plot.Refresh();
        }

        // рисует/удаляет линии ценового диапазона
        private void CreateRangeLines(RangeIntervals interval)
        {

            if (maxRangeLine != null)
            {
                plt.Remove(maxRangeLine);
                maxRangeLine = null;
            }

            if (minRangeLine != null)
            {
                plt.Remove(minRangeLine);
                minRangeLine = null;
            }

            if (interval != RangeIntervals.None)
            {
                var range = displaycandles.GetIntervalRange(interval, true, Time);

                if (range != null)
                {
                    maxRangeLine = plt.Add.HorizontalLine(range.Max, 1, ScottPlot.Colors.Green);
                    minRangeLine = plt.Add.HorizontalLine(range.Min, 1, ScottPlot.Colors.Red);
                    maxRangeLine.Axes.YAxis = yaxis;
                    minRangeLine.Axes.YAxis = yaxis;
                }

            }

            Plot.Refresh();
        }

        // рисует/удаляет тейк и стоп линии
        private void CreateTakeStopLines(double price, string arg)
        {
            if (arg == "take")
            {
                if (takeLine != null)
                {
                    plt.Remove(takeLine);
                    takeLine = null;
                }

                if (price > 0)
                {
                    takeLine = plt.Add.HorizontalLine(price, 1, ScottPlot.Colors.Blue);
                    takeLine.IsDraggable = true;
                    takeLine.Text = "Take";
                    takeLine.Axes.YAxis = yaxis;
                }

            }
            else if (arg == "stop")
            {
                if (stopLine != null)
                {
                    plt.Remove(stopLine);
                    stopLine = null;
                }

                if (price > 0)
                {
                    stopLine = plt.Add.HorizontalLine(price, 1, ScottPlot.Colors.Orange);
                    stopLine.IsDraggable = true;
                    stopLine.Text = "Stop";
                    stopLine.Axes.YAxis = yaxis;
                }

            }

            Plot.Refresh();
        }

        // рисует/удаляет выделение временного диапазона
        private void CreateRangeSpan(RangeIntervals interval)
        {

            if (rangeSpan != null)
            {
                plt.Remove(rangeSpan);
                rangeSpan = null;
            }

            if (interval != RangeIntervals.None)
            {
                var range = displaycandles.GetIntervalRange(interval, true, Time);

                if (range != null)
                {
                    double X1 = range.Start.ToOADate();
                    double X2 = range.End.ToOADate();
                    rangeSpan = plt.Add.HorizontalSpan(X1, X2, ScottPlot.Colors.LightGreen.WithAlpha(0.1));
                }

            }

            Plot.Refresh();

        }

        private void OnChangeTakeStop(double price, string arg)
        {
            CreateTakeStopLines(price, arg);
        }

        #endregion

        #region mouse

        private void Plot_MouseMove(object s, MouseEventArgs e)
        {
            Coordinates mouseCoords = GetCoordinates(e);

            //MouseEventProcessing("MouseMove", mouseCoords);

            if (trendLine != null)
               trendLine.MouseMove(mouseCoords);

            if (CrosshairOn)
                CrosshairMove(mouseCoords);

        }

        private void Plot_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            // selectedCandle = GetCandleFromMousePosition(e);
            //Coordinates mouseCoords = GetCoordinates(e);
        }

        private void Plot_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            //Coordinates mouseCoords = GetCoordinates(e);
            // SelectedCandle = GetCandleFromMousePosition(e); // выбирает свечу при помощи щелчка правой кнопкой
        }

        private void Plot_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Coordinates mouseCoords = GetCoordinates(e);
            //e.Handled = SelectCandle(mouseCoords);
        }

        private void Plot_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Coordinates mouseCoords = GetCoordinates(e);

            if (trendLine != null)
                trendLine.MouseLeftButtonDown(mouseCoords);

            //MouseEventProcessing("MouseLeftButtonDown", mouseCoords);
          

            //if (draggedLine is null)
            //    Plot.Cursor = Cursors.Arrow;
            //else if (draggedLine is HorizontalLine)
            //{
            //    Plot.UserInputProcessor.IsEnabled = true;
            //    Plot.Cursor = Cursors.SizeNS;
            //    e.Handled = true;
            //}
        }

        private void Plot_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Coordinates mouseCoords = GetCoordinates(e);

            if (IsSlope)
            {
                var offset = TimeSpan.FromMinutes(30).ToOADate();
                CoordinateLine line = new(mouseCoords, new Coordinates(mouseCoords.X + offset, mouseCoords.Y));
                //interactiveLine = new(line);
                trendLine = new(line);
                IsSlope = false;
                Plot.Refresh();
            }

            //SelectCandle(mouseCoords);
        }

        private void Plot_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            Coordinates mouseCoords = GetCoordinates(e);

            //ResetPreviousSelection();

            // Важно: убедитесь, что на графике используется DateTimeTicksBottom для отображения времени
            // WpfPlot1.Plot.Axes.DateTimeTicksBottom(); // Используйте, если не делали этого ранее

        }

        private void Plot_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Coordinates mouseCoords = GetCoordinates(e);

            //var lines = plt.GetPlottables().OfType<HorizontalLine>().Where(l => l.IsDraggable);

            //foreach (var line in lines)
            //{
            //    // Если курсор достаточно близко к линии по оси Y, "захватываем" её

            //    if (line.Grab(mouseCoords.Y) < grabVrange)
            //    {
            //        draggedLine = line;
            //        break;
            //    }

            //}

            //if (draggedLine is null)
            //    Plot.Cursor = Cursors.Arrow;
            //else if (draggedLine is HorizontalLine)
            //{
            //    Plot.UserInputProcessor.IsEnabled = true;
            //    Plot.Cursor = Cursors.SizeNS;
            //    e.Handled = true;
            //}


        }

        private void Plot_MouseUp(object sender, MouseButtonEventArgs e)
        {
            //Coordinates mouseCoords = GetCoordinates(e);

            //if (draggedLine != null)
            //{
            //    if (draggedLine.Text == "Take")
            //    {
            //        //TODO Изменить тейк
            //    }

            //    if (draggedLine.Text == "Stop")
            //    {
            //        //TODO Изменить стоп
            //    }

            //    draggedLine = null;
            //    Plot.Cursor = Cursors.Arrow;
            //    Plot.UserInputProcessor.Reset();
            //    Plot.Refresh();
            //}
        }

        private void MouseEventProcessing(string evnt, Coordinates mouseCoords)
        {
            var plottables = plt.GetPlottables();

            foreach (var item in plottables)
            {
                if (item is IHasInteractiveHandles)
                {
                    if (item is InteractiveLineSegment)
                    {
                        // Check if the cursor is over the line

                        var segment = item as InteractiveLineSegment;

                        if (interactiveLine.Segment.Equals(segment))
                        {

                            if (evnt == "MouseMove")
                            {
                                interactiveLine.MouseMove(mouseCoords);
                            }
                            else if (evnt == "MouseLeftButtonDown")
                            {
                                interactiveLine.MouseLeftButtonDown(mouseCoords);
                            }

                        }

                        //bool mouseover = segment.Line.IsMouseOver(mouseCoords);    

                        //if (mouseover)
                        //{
                        //    if(!segment.StartMarkerStyle.IsVisible)
                        //        segment.StartMarkerStyle.IsVisible = true;

                        //    if (!segment.EndMarkerStyle.IsVisible)
                        //        segment.EndMarkerStyle.IsVisible = true;

                        //}
                        //else
                        //{
                        //    segment.StartMarkerStyle.IsVisible = false;
                        //    segment.EndMarkerStyle.IsVisible = false;
                        //}

                    }
                    else if (item is InteractiveHorizontalLine)
                    {
                        //TODO
                    }
                }

                //    // Если курсор достаточно близко к линии по оси Y, "захватываем" её

                //    if (line.Grab(mouseCoords.Y) < grabVrange)
                //    {
                //        draggedLine = line;
                //        break;
            }

            Plot.Refresh();
        }



        private Coordinates GetCoordinates(MouseButtonEventArgs e)
        {
            System.Windows.Point p = e.GetPosition(Plot);
            Pixel pixel = new(p.X * Plot.DisplayScale, p.Y * Plot.DisplayScale);

            return plt.GetCoordinates(pixel, plt.Axes.Bottom, yaxis);
        }

        private Coordinates GetCoordinates(MouseEventArgs e)
        {
            System.Windows.Point p = e.GetPosition(Plot);
            Pixel pixel = new(p.X * Plot.DisplayScale, p.Y * Plot.DisplayScale);
            return plt.GetCoordinates(pixel, plt.Axes.Bottom, yaxis);
        }

        public Coordinates GetSnappedPoint(Coordinates mouseCoord)
        {
            if (oHLCs == null || oHLCs.Count == 0) return mouseCoord;

            double mouseX = mouseCoord.X;
            double mouseY = mouseCoord.Y;

            // Бинарный поиск ближайшей свечи по X (всегда ищем ближайшую, без порога по X)
            int left = 0;
            int right = oHLCs.Count - 1;
            while (left <= right)
            {
                int mid = left + (right - left) / 2;
                double midX = oHLCs[mid].DateTime.ToOADate();
                if (midX == mouseX) return TrySnapToExtremum(oHLCs[mid], midX, mouseY);
                if (midX < mouseX) left = mid + 1;
                else right = mid - 1;
            }

            // Выбираем ближайшую из двух кандидатов (left и right)
            OHLC? closest = null;
            double minDiffX = double.MaxValue;

            if (left < oHLCs.Count)
            {
                double diff = Math.Abs(oHLCs[left].DateTime.ToOADate() - mouseX);
                if (diff < minDiffX) { minDiffX = diff; closest = oHLCs[left]; }
            }

            if (right >= 0)
            {
                double diff = Math.Abs(oHLCs[right].DateTime.ToOADate() - mouseX);
                if (diff < minDiffX) { minDiffX = diff; closest = oHLCs[right]; }
            }

            if (closest == null)
                return mouseCoord;

            double candleX = closest.Value.DateTime.ToOADate();

            // Пытаемся прилипнуть к high или low только если Y в пределах порога
            return TrySnapToExtremum(closest.Value, candleX, mouseY);
        }

        private Coordinates TrySnapToExtremum(OHLC candle, double candleX, double mouseY)
        {
            double high = candle.High;
            double low = candle.Low;

            double distToHigh = Math.Abs(high / mouseY - 1);
            double distToLow = Math.Abs(low / mouseY - 1);

           // Фиксированный порог в единицах цены
            if (distToHigh <= SNAP_Y_THRESHOLD)
            {
                return new Coordinates(candleX, high);
            }

            if (distToLow <= SNAP_Y_THRESHOLD)
            {
                return new Coordinates(candleX, low);
            }

            //Если ни high, ни low не попали в порог → возвращаем точные координаты мыши
            //(X остаётся привязанным к свече, Y — где курсор)
            return new Coordinates(candleX, mouseY);
        }
          
       // возвращает свечу, на которую указывает курсор мыши
        private Candle GetCandleFromMousePosition(MouseEventArgs e)
        {
            Coordinates mouseCoords = GetCoordinates(e);
           
            var tm = DateTime.FromOADate(mouseCoords.X).Round(TimeSpan.FromMinutes((int)TimeFrame));

            Candle candle;

            if (tm < displaycandles.First().OpenTime)
                candle = displaycandles.First();
            else if (tm > displaycandles.Last().OpenTime)
                candle = displaycandles.Last();
            else
                candle = displaycandles.FirstOrDefault(c => c.OpenTime == tm);

            return candle;
        }

        // выбор свечи. Ищем свечу по которой кликнули мышью
        private bool SelectCandle(Coordinates mouseCoords)
        {
            foreach (var plottable in plt.GetPlottables())
            {
                if (plottable is CandlestickPlot candles && candles.Sequential == false)
                {
                    // Получаем массив временных меток в формате OADate (double)
                    double[] candlePositions = [.. oHLCs.Select(ohlc => ohlc.DateTime.ToOADate())];

                    for (int i = 0; i < candlePositions.Length; i++)
                    {
                        var ohlc = oHLCs[i];

                        double candleCenterX = candlePositions[i];

                        // Расчёт ширины свечи в единицах данных.
                        // TimeSpan свечи взят из её данных, если не задан - используется TimeSpan.FromDays(1)
                        double candleWidth = (ohlc.TimeSpan != default ? ohlc.TimeSpan : TimeSpan.FromDays(1)).TotalDays * 0.8; // 80% от интервала для визуального зазора
                        double halfWidth = candleWidth / 2;

                        // Проверяем, попадает ли курсор в границы свечи по X и Y
                        bool isXInRange = mouseCoords.X >= (candleCenterX - halfWidth) && mouseCoords.X <= (candleCenterX + halfWidth);
                        bool isYInRange = mouseCoords.Y >= ohlc.Low && mouseCoords.Y <= ohlc.High;

                        if (isXInRange && isYInRange)
                        {

                            HighlightSelectedCandle(i);

                            Plot.Refresh();
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        // выбор/отмена выбора одной свечи
        private void HighlightSelectedCandle(int index)
        {

            var sOHLC = oHLCs[index];

            var oldSelection = selectedCandles.FirstOrDefault(c => c.Candle.OpenTime == sOHLC.DateTime);

            if (oldSelection != null) // отменяем выбор
            {
                plt.Remove(oldSelection.Markers);
                selectedCandles.Remove(oldSelection);
                SelectedCandle = selectedCandles.Count == 0 ? null : selectedCandles.Last();
                if (SelectedCandle == null)
                    SetTakeStopMenu(false);
            }
            else
            {
                // 3. Создаем координаты двух маркеров для верха и низа свечи
                double[] X = [sOHLC.DateTime.ToOADate(), sOHLC.DateTime.ToOADate()];
                double[] Y = [sOHLC.High, sOHLC.Low];

                var m = plt.Add.Markers(X, Y);
                m.Axes.YAxis = yaxis;
                m.MarkerSize = 10;
                m.MarkerShape = MarkerShape.FilledDiamond;
                m.Color = ScottPlot.Colors.Blue;
                m.IsVisible = true;

                selectedCandles.Add(new SelectedCandle { Index = index, Candle = displaycandles[index], Markers = m });
                SelectedCandle = selectedCandles.Last();
                SetTakeStopMenu(true);

            }

            Plot.Refresh();


        }

        // удаляет все выбранные свечи
        private void DeleteAllSelectedCandles()
        {
            foreach (var candle in selectedCandles)
            {
                plt.Remove(candle.Markers);
            }
            selectedCandles.Clear();
            SelectedCandle = null;
        }

        // выделение одной свечи
        private void HighlightSlopeCandle(int index)
        {
            // 1. Сбрасываем предыдущее выделение
            //if (candleMarker != null)
            //{
            //    plt.Remove(candleMarker);
            //    candleMarker = null;
            //}

            // 2. Получаем данные выбранной свечи для позиционирования маркера
            var sOHLC = oHLCs[index];

            if (slope.FirstCandle == null)
            {
                slope.FirstCandle = displaycandles[index];
                SetSlopeMarkers(sOHLC, 0);
            }
            else if (slope.SecondCandle == null)
            {
                slope.FirstCandle = displaycandles[index];
                SetSlopeMarkers(sOHLC, 1);
                //TODO строим линию
                slope.Line = plt.Add.Line(slope.GetCoordinateLine().GetValueOrDefault());

                //  var sl =  plt.Add.Coordinate.Markers(X, Y);

                //TODO сбрасываем выделение свечей
                //for (var i = 0; i < candleMarkers.Length; i++)
                //{
                //    if (candleMarkers[i] != null)
                //    {
                //        plt.Remove(candleMarkers[i]);
                //        candleMarkers[i] = null;
                //    }
                //}

            }
            else
            {

            }


            Plot.Refresh();


        }

        private void SetSlopeMarkers(OHLC sOHLC, int marIdx)
        {
            double[] X = [sOHLC.DateTime.ToOADate(), sOHLC.DateTime.ToOADate()];
            double[] Y = [sOHLC.High, sOHLC.Low];

            //candleMarkers[marIdx] = plt.Add.Markers(X, Y);
            //candleMarkers[marIdx].Axes.YAxis = yaxis;
            //candleMarkers[marIdx].MarkerSize = 10;
            //candleMarkers[marIdx].MarkerShape = MarkerShape.FilledDiamond;
            //candleMarkers[marIdx].Color = ScottPlot.Colors.Blue;
            //candleMarkers[marIdx].IsVisible = true;

        }

        // выделение одной свечи
        private void ShowMarker(OHLC oHLC)
        {

            // 3. Создаем точечный график с одной точкой-маркером для выделения
            //double[] markerX = { selectedOHLC.DateTime.ToOADate() }; // Позиция по X
            //double[] markerY = { (selectedOHLC.High + selectedOHLC.Low) / 2.0 }; // Центр свечи по Y

            double X = oHLC.DateTime.ToOADate(); // Позиция по X
            double Y = (oHLC.High + oHLC.Low) / 2.0; // Центр свечи по Y

            //double Y = oHLC.High + 1; // Центр свечи по Y

            //selectedScatter = plt.Add.Scatter(markerX, markerY);

            //selectedCandleMarker = plt.Add.Marker(X, Y);

            //selectedCandleMarker.Axes.YAxis = yaxis;
            //selectedCandleMarker.MarkerSize = 10; // Большой размер
            //selectedCandleMarker.MarkerShape = MarkerShape.FilledDiamond; // Чёткая форма
            //selectedCandleMarker.Color = ScottPlot.Colors.Yellow; // Яркий цвет

            //selectedCandleMarker.IsVisible = true;

            ////selectedCandleMarker.Size = 20;
            //selectedCandleMarker.Shape = MarkerShape.FilledDiamond;

            //selectedScatter.MarkerSize = 20; // Большой размер
            //selectedScatter.MarkerShape = MarkerShape.FilledDiamond; // Чёткая форма
            //selectedScatter.Color = ScottPlot.Colors.Yellow; // Яркий цвет
            //selectedScatter.LineWidth = 0; // Без линии


        }

        // перемещение перекрестья
        private void CrosshairMove(Pixel mousePixel)
        {
            Coordinates coordinates = plt.GetCoordinates(mousePixel, plt.Axes.Bottom, yaxis);
            ch.Position = coordinates;
            var tm = DateTime.FromOADate(ch.Position.X).Round(TimeSpan.FromMinutes((int)TimeFrame));
            ch.VerticalLine.LabelText = $"{tm:HH:mm}";
            var fmt = "{0:F" + security.PriceScale + "}";
            ch.HorizontalLine.LabelText = string.Format(fmt, ch.Position.Y);
            
            CrosshairCandle = candles.FirstOrDefault(c => c.OpenTime == tm);
            CandleViewModel.Candle = CrosshairCandle;
            Plot.Refresh();
        }

        private void CrosshairMove(Coordinates coordinates)
        {
            //Coordinates coordinates = plt.GetCoordinates(mousePixel, plt.Axes.Bottom, yaxis);
            ch.Position = coordinates;
            var tm = DateTime.FromOADate(ch.Position.X).Round(TimeSpan.FromMinutes((int)TimeFrame));
            ch.VerticalLine.LabelText = $"{tm:HH:mm}";
            var fmt = "{0:F" + security.PriceScale + "}";
            ch.HorizontalLine.LabelText = string.Format(fmt, ch.Position.Y);

            CrosshairCandle = candles.FirstOrDefault(c => c.OpenTime == tm);
            CandleViewModel.Candle = CrosshairCandle;
            Plot.Refresh();
        }

        #endregion

        #region menu

        private void DoTakeStopMenu(Plot plot)
        {
            if (SelectedCandle == null) return;

            if (Trading.Side == Sides.Buy)
            {
                if (Trading.Price <= SelectedCandle.Candle.Low - security.PriceFilter.TickSize * Trading.SLOffset)
                {
                    if (MessageBox.Show(" Выбрана некорректная свеча. Цена заявки меньше Low свечи. ", "",
                        MessageBoxButton.OK, MessageBoxImage.Exclamation) == MessageBoxResult.OK)
                    {
                        return;
                    }
                }
                else
                    Trading.StopLoss = SelectedCandle.Candle.Low - security.PriceFilter.TickSize * Trading.SLOffset;
            }
            else if (Trading.Side == Sides.Sell)
            {
                if (Trading.Price >= SelectedCandle.Candle.High + security.PriceFilter.TickSize * Trading.SLOffset)
                {
                    if (MessageBox.Show(" Выбрана некорректная свеча. Цена заявки выше High свечи. ", "", MessageBoxButton.OK, MessageBoxImage.Exclamation) == MessageBoxResult.OK)
                    {
                        return;
                    }
                }
                else
                    Trading.StopLoss = SelectedCandle.Candle.High + security.PriceFilter.TickSize * Trading.SLOffset;
            }
        }

        private void SetTakeStopMenu(bool set)
        {
            menu.Clear();
            if (set)
                menu.Add("Установить тейк/стоп", DoTakeStopMenu);
        }

        #endregion

        private void SetStartTime()
        {
            StartTime = candles.First().OpenTime.AddDays(StartBackground);
            FirstTime = StartTime;
            Time = StartTime;
            EndTime = candles.Last().OpenTime;
            CurrentCandle = candles.FirstOrDefault(c => c.OpenTime == Time);
        }

        private void SetDisplayCandles()
        {
            displaycandles = [.. candles.Where(c => c.OpenTime >= Time.AddDays(-WorkBackground) && c.OpenTime <= Time)];
        }

        public void OnLoaded(object sender, RoutedEventArgs e)
        {
            SetDisplayCandles();
            CurrentCandle = candles.FirstOrDefault(c => c.OpenTime == Time);
            Trading.CurrentCandle = CurrentCandle;
            Trading.ChangeTakeStop += OnChangeTakeStop;

            Plot.MouseRightButtonDown += Plot_MouseRightButtonDown;
            Plot.MouseRightButtonUp += Plot_MouseRightButtonUp;

            Plot.PreviewMouseLeftButtonDown += Plot_PreviewMouseLeftButtonDown;
            Plot.MouseLeftButtonDown += Plot_MouseLeftButtonDown;
            Plot.MouseLeftButtonUp += Plot_MouseLeftButtonUp;
            
            Plot.MouseDown += Plot_MouseDown;
            Plot.MouseUp += Plot_MouseUp;

            Plot.MouseMove += Plot_MouseMove;
            Plot.MouseDoubleClick += Plot_MouseDoubleClick;

            plt.Grid.YAxis = yaxis;

            if(yaxis.Edge == Edge.Right)
                plt.Axes.Left.IsVisible = false;

            CreateCrosshair();
            Plotchart();

            //SetTakeStopMenu();

        }

        public void OnUnloaded(object sender, RoutedEventArgs e)
        {
            Plot.MouseRightButtonDown -= Plot_MouseRightButtonDown;
            Plot.MouseRightButtonUp -= Plot_MouseRightButtonUp;
            Plot.MouseMove -= Plot_MouseMove;
            Plot.MouseDoubleClick -= Plot_MouseDoubleClick;
            Plot.MouseLeftButtonDown -= Plot_MouseLeftButtonDown;
            Plot.MouseLeftButtonUp -= Plot_MouseLeftButtonUp;
            Plot.PreviewMouseLeftButtonDown -= Plot_PreviewMouseLeftButtonDown;
            Plot.MouseDown -= Plot_MouseDown;
            Plot.MouseUp -= Plot_MouseUp;

            Trading.ChangeTakeStop -= OnChangeTakeStop;
        }

        public void OnClosing(object sender, CancelEventArgs e)
        {

            //TODO
        }

    }
}


//using System.Windows.Input;
//using ScottPlot.WPF;

//public partial class MainWindow : Window
//{
//    private ScottPlot.Plottables.HorizontalLine? _draggedLine = null;
//    private readonly WpfPlot _wpfPlot;

//    public MainWindow()
//    {
//        InitializeComponent();
//        _wpfPlot = wpfPlot1; // Предполагается, что это имя вашего элемента WpfPlot

//        // 1. Добавляем горизонтальную линию
//        var horizontalLine = _wpfPlot.Plot.Add.HorizontalLine(2.5);
//        horizontalLine.Color = Colors.Blue;
//        horizontalLine.LineWidth = 2;

//        // 2. Отключаем стандартные взаимодействия (панорамирование), чтобы они не мешали
//        _wpfPlot.Interactions.Disable();

//        // 3. Подписываемся на события мыши для ручного управления
//        _wpfPlot.MouseDown += WpfPlot_MouseDown;
//        _wpfPlot.MouseMove += WpfPlot_MouseMove;
//        _wpfPlot.MouseUp += WpfPlot_MouseUp;

//        _wpfPlot.Refresh();
//    }

//    private void WpfPlot_MouseDown(object sender, MouseButtonEventArgs e)
//    {
//        var mousePosition = e.GetPosition(_wpfPlot);
//        var coordinates = _wpfPlot.GetCoordinates(mousePosition.X, mousePosition.Y);

//        // Ищем все горизонтальные линии на графике
//        var allHorizontalLines = _wpfPlot.Plot.GetPlottables()
//                                             .OfType<ScottPlot.Plottables.HorizontalLine>();

//        double snapDistance = 0.15; // "Чувствительность" захвата в единицах графика

//        foreach (var line in allHorizontalLines)
//        {
//            // Если курсор достаточно близко к линии по оси Y, "захватываем" её
//            if (Math.Abs(coordinates.Y - line.Y) < snapDistance)
//            {
//                _draggedLine = line;
//                break;
//            }
//        }
//    }

//    private void WpfPlot_MouseMove(object sender, MouseEventArgs e)
//    {
//        if (_draggedLine == null) return;

//        var mousePosition = e.GetPosition(_wpfPlot);
//        var coordinates = _wpfPlot.GetCoordinates(mousePosition.X, mousePosition.Y);

//        // Обновляем положение захваченной линии по оси Y
//        _draggedLine.Y = coordinates.Y;
//        _wpfPlot.Refresh(); // Перерисовываем график
//    }

//    private void WpfPlot_MouseUp(object sender, MouseButtonEventArgs e)
//    {
//        // Отпускаем линию при отпускании кнопки мыши
//        _draggedLine = null;
//    }
//}


//myPlot.UserInputProcessor.IsEnabled = true;

//myPlot.UserInputProcessor.UserActionResponses.Clear();
//var menuButton = StandardMouseButtons.Right;
//var menuResponse = new ScottPlot.Interactivity.UserActionResponses.SingleClickContextMenu(menuButton);
//myPlot.UserInputProcessor.UserActionResponses.Add(menuResponse);


//public partial class Form1 : Form
//{
//    private readonly Plot plot = new Plot();
//    private readonly FormsPlot formsPlot;
//    private List<OHLC> candles; // предполагаем отсортированы по DateStart ascending
//    private LinePlot trendLine;
//    private Coordinates? startPoint;
//    private bool isDrawing = false;

//    // === Настраиваемый порог snapping только по Y ===
//    private const double SNAP_Y_THRESHOLD_PRICE = 5.0;      // макс. расстояние по Y в единицах цены
//    // private const double SNAP_Y_THRESHOLD_PERCENT = 2.0; // альтернативно — в % от диапазона свечи

//    public Form1()
//    {
//        InitializeComponent();

//        formsPlot = new FormsPlot { Dock = DockStyle.Fill };
//        Controls.Add(formsPlot);
//        formsPlot.Plot = plot;

//        candles = GenerateRandomCandles(30);
//        plot.Add.Candlesticks(candles);
//        plot.Axes.DateTimeTicksBottom();

//        trendLine = plot.Add.Line(0, 0, 0, 0);
//        trendLine.Color = Colors.Red;
//        trendLine.Width = 2;
//        trendLine.IsVisible = false;

//        formsPlot.MouseDown += OnMouseDown;
//        formsPlot.MouseMove += OnMouseMove;
//        formsPlot.Refresh();
//    }

//    private List<OHLC> GenerateRandomCandles(int count)
//    {
//        var rnd = new Random();
//        var list = new List<OHLC>(count);
//        DateTime startDate = DateTime.Today;
//        for (int i = 0; i < count; i++)
//        {
//            double open = 100 + rnd.NextDouble() * 10;
//            double close = 100 + rnd.NextDouble() * 10;
//            double high = Math.Max(open, close) + rnd.NextDouble() * 5;
//            double low = Math.Min(open, close) - rnd.NextDouble() * 5;
//            list.Add(new OHLC(open, high, low, close, startDate.AddDays(i), TimeSpan.FromDays(1)));
//        }
//        return list;
//    }

//    private Coordinates GetSnappedPoint(Coordinates mouseCoord)
//    {
//        if (candles == null || candles.Count == 0)
//            return mouseCoord;

//        double mouseX = mouseCoord.X;
//        double mouseY = mouseCoord.Y;

//        // Бинарный поиск ближайшей свечи по X (всегда ищем ближайшую, без порога по X)
//        int left = 0;
//        int right = candles.Count - 1;
//        while (left <= right)
//        {
//            int mid = left + (right - left) / 2;
//            double midX = candles[mid].DateStart.ToOADate();
//            if (midX == mouseX) return TrySnapToExtremum(candles[mid], midX, mouseY);
//            if (midX < mouseX) left = mid + 1;
//            else right = mid - 1;
//        }

//        // Выбираем ближайшую из двух кандидатов (left и right)
//        OHLC? closest = null;
//        double minDiffX = double.MaxValue;

//        if (left < candles.Count)
//        {
//            double diff = Math.Abs(candles[left].DateStart.ToOADate() - mouseX);
//            if (diff < minDiffX) { minDiffX = diff; closest = candles[left]; }
//        }

//        if (right >= 0)
//        {
//            double diff = Math.Abs(candles[right].DateStart.ToOADate() - mouseX);
//            if (diff < minDiffX) { minDiffX = diff; closest = candles[right]; }
//        }

//        if (closest == null)
//            return mouseCoord;

//        double candleX = closest.Value.DateStart.ToOADate();

//        // Пытаемся прилипнуть к high или low только если Y в пределах порога
//        return TrySnapToExtremum(closest.Value, candleX, mouseY);
//    }

//    private Coordinates TrySnapToExtremum(OHLC candle, double candleX, double mouseY)
//    {
//        double high = candle.High;
//        double low = candle.Low;

//        double distToHigh = Math.Abs(high - mouseY);
//        double distToLow = Math.Abs(low - mouseY);

//        // Фиксированный порог в единицах цены
//        if (distToHigh <= SNAP_Y_THRESHOLD_PRICE)
//        {
//            return new Coordinates(candleX, high);
//        }

//        if (distToLow <= SNAP_Y_THRESHOLD_PRICE)
//        {
//            return new Coordinates(candleX, low);
//        }

//        // Альтернатива: порог в процентах от диапазона свечи (раскомментировать при необходимости)
//        // double candleRange = high - low;
//        // if (candleRange > 0)
//        // {
//        //     double relThreshold = candleRange * (SNAP_Y_THRESHOLD_PERCENT / 100.0);
//        //     if (distToHigh <= relThreshold) return new Coordinates(candleX, high);
//        //     if (distToLow  <= relThreshold) return new Coordinates(candleX, low);
//        // }

//        // Если ни high, ни low не попали в порог → возвращаем точные координаты мыши
//        // (X остаётся привязанным к свече, Y — где курсор)
//        return new Coordinates(candleX, mouseY);
//    }

//    private void OnMouseDown(object? sender, MouseEventArgs e)
//    {
//        if (e.Button != MouseButtons.Left) return;

//        Coordinates mouseCoord = formsPlot.Plot.GetCoordinate(e.Location);

//        if (!isDrawing)
//        {
//            startPoint = GetSnappedPoint(mouseCoord);
//            isDrawing = true;
//            trendLine.IsVisible = true;
//        }
//        else
//        {
//            Coordinates endPoint = GetSnappedPoint(mouseCoord);
//            UpdateTrendLine(startPoint.Value, endPoint);
//            isDrawing = false;
//        }

//        formsPlot.Refresh();
//    }

//    private void OnMouseMove(object? sender, MouseEventArgs e)
//    {
//        if (!isDrawing || startPoint == null) return;

//        Coordinates mouseCoord = formsPlot.Plot.GetCoordinate(e.Location);
//        Coordinates endPoint = GetSnappedPoint(mouseCoord);
//        UpdateTrendLine(startPoint.Value, endPoint);
//        formsPlot.Refresh();
//    }

//    private void UpdateTrendLine(Coordinates start, Coordinates end)
//    {
//        trendLine.Start = start;
//        trendLine.End = end;
//    }
//}