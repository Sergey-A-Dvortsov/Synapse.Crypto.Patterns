using Synapse.Crypto.Trading;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Synapse.Crypto.Patterns
{
    public class SimulatorNaviViewModel : BaseViewModel
    {
        //public const string NEXTBAR = "Следующий бар";
        //public const string TS = "Тейк/стоп";
        //public const string RANGEBREAK = "Пробой диапазона";
        //public const string SLOPEBREAK = "Пробой наклонной";

        private SimulatorViewModel? parent;

        public SimulatorNaviViewModel()
        {
            parent = SimulatorViewModel.Instance;
            if (parent == null) throw new InvalidOperationException("Parent SimulatorViewModel is not set.");
            NextCommand = new DelegateCommand(OnNext, CanNext);
            GoToReasonCommand = new DelegateCommand(OnGoToReason, CanGoToReason);
            GoToReasons = parent.GoToReasons;
            GoToReason = GoToReasons.First();
            Instance = this;
        }

        public static SimulatorNaviViewModel? Instance { get; private set; }

        public event Action<string, int> NaviAction = delegate { };

        private void OnNaviAction(string reason, int bars = 0)
        {
            NaviAction.Invoke(reason, bars);
        }

        public List<string> GoToReasons { get; private set; }

        private string _goToReason;
        /// <summary>
        /// Место, куда нужно перейти после нажатия кнопки "Next".
        /// В обычном варианте 
        /// 1. "Следующий бар" вы перейдете на следующую свечу или через несколько свечей, если установите шаг > 1.
        /// Но также возможны следующие варианты
        /// 2. "Тейк/стоп", если была открыта позиция, можно перейти к свече, на которой сработал тейк или стоп
        /// 3. "Пробой диапазона", если установлен "диапзон", то к месту пробоя диапазона
        /// 4. "Пробой наклонной", если установлена наклонная, то к месту пробоя наклонной
        /// </summary>
        public string GoToReason
        {
            get => _goToReason;
            set
            {
                _goToReason = value;
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

        /// <summary>
        /// Перемещение на n баров вперед.
        /// </summary>
        public DelegateCommand NextCommand { get; set; }

        private void OnNext(object arg)
        {

            OnNaviAction(SimulatorViewModel.NEXTBAR, Steps);

            //switch (GoToItem)
            //{
            //    case NEXTBAR: //Перемещение на n баров, заданных в Steps

            //        if (trading?.Position?.State == PositionStates.Open)
            //        {
            //            idx = trading.CheckForClose(trading.CurrentCandle, Steps);
            //            trading.CurrentCandle = candles[idx];
            //            parent.Time = trading.CurrentCandle.OpenTime;
            //        }
            //        else
            //        {
            //            parent.Time += TimeSpan.FromMinutes((int)parent.TimeFrame * Steps);
            //            trading.CurrentCandle = candles.FirstOrDefault(c => c.OpenTime == parent.Time);
            //        }

            //        break;
            //    case TS: // Перемещение к месту тейка/стопа

            //        if (trading?.Position?.State != PositionStates.Open) return;

            //        idx = trading.CheckForClose(CurrentCandle, -1);
            //        trading.CurrentCandle = candles[idx];
            //        parent.Time = trading.CurrentCandle.OpenTime;

            //        break;
            //    case RANGEBREAK: // Перемещение к месту пробоя диапазона
            //        if (trading?.Position?.State == PositionStates.Open) return;

            //        //TODO выполняется логика перемещения, функция перемещени должна вернуть индекс текущей свечи
            //        //idx = Trading.CheckForClose(CurrentCandle, -1);
            //        //CurrentCandle = candles[idx];
            //        //Time = CurrentCandle.OpenTime;

            //        break;
            //    case SLOPEBREAK: // Перемещение к месту пробоя наклонной
            //        if (trading?.Position?.State == PositionStates.Open) return;

            //        //TODO выполняется логика перемещения, функция перемещени должна вернуть индекс текущей свечи
            //        //idx = Trading.CheckForClose(CurrentCandle, -1);
            //        //CurrentCandle = candles[idx];
            //        //Time = CurrentCandle.OpenTime;

            //        break;
            //    default:
            //        break;
            //}

            //if (trading?.Position?.State == PositionStates.Close)
            //    trading.ClearPosition();

            //trading.CurrentCandle = CurrentCandle;
            //SetDisplayCandles();
            //Plotchart();
            //RangeInterval = RangeInterval;

        }

        private bool CanNext(object arg)
        {
            if(arg?.ToString() != SimulatorViewModel.NEXTBAR) return false;
            return (parent.Time + TimeSpan.FromMinutes((int)parent.TimeFrame * Steps)) <= parent.EndTime;
        }

        /// <summary>
        /// Перемещение по шкале времени к месту, где выполняется заданное условие.
        /// </summary>
        public DelegateCommand GoToReasonCommand { get; set; }

        private void OnGoToReason(object arg)
        {
            //int idx = 0;

            //switch (GoToItem)
            //{
            //    case TS: // Перемещение к месту исполнения тейка/стопа

            //        if (trading?.Position?.State != PositionStates.Open) return;

            //        idx = trading.CheckForClose(CurrentCandle, -1);
            //        trading.CurrentCandle = candles[idx];
            //        parent.Time = trading.CurrentCandle.OpenTime;

            //        break;
            //    case RANGEBREAK: // Перемещение к месту пробоя диапазона
            //        if (trading?.Position?.State == PositionStates.Open) return;

            //        //TODO выполняется логика перемещения, функция перемещени должна вернуть индекс текущей свечи
            //        //idx = Trading.CheckForClose(CurrentCandle, -1);
            //        //CurrentCandle = candles[idx];
            //        //Time = CurrentCandle.OpenTime;

            //        break;
            //    case SLOPEBREAK: // Перемещение к месту пробоя наклонной
            //        if (trading?.Position?.State == PositionStates.Open) return;

            //        //TODO выполняется логика перемещения, функция перемещени должна вернуть индекс текущей свечи
            //        //idx = Trading.CheckForClose(CurrentCandle, -1);
            //        //CurrentCandle = candles[idx];
            //        //Time = CurrentCandle.OpenTime;

            //        break;
            //    default:
            //        break;
            //}

            //if (trading?.Position?.State == PositionStates.Close)
            //    trading.ClearPosition();

            //trading.CurrentCandle = CurrentCandle;
            //SetDisplayCandles();
            //Plotchart();
            //RangeInterval = RangeInterval;

        }

        private bool CanGoToReason(object arg)
        {
            if (arg?.ToString() == SimulatorViewModel.NEXTBAR) return false;
            return parent.Time < parent.EndTime;
        }

    }
}
