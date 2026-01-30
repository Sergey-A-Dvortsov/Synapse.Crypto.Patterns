using bybit.net.api.Models.Trade;
using NLog;
using Synapse.Crypto.Bybit;
using Synapse.Crypto.Trading;
using Synapse.General;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace Synapse.Crypto.Patterns
{

    public class TakeStopBlock : BaseViewModel
    {
        private double _stopLoss;
        /// <summary>
        /// Уровень стоп-лосса
        /// </summary>
        //public double StopLoss
        //{
        //    get
        //    {
        //        OnChangeTakeStop(_stopLoss, "stop");
        //        return Math.Round(_stopLoss, security.PriceScale);
        //    }
        //    set
        //    {
        //        _stopLoss = value;
        //        _takeProfit = Side == Sides.Buy ? Price + (LossAbs * TakeStopRatio) : Price - (LossAbs * TakeStopRatio);
        //        NotifyProfitLoss();
        //    }
        //}

        //WpfPlot1.Plot.Add.InteractiveHorizontalLineSegment(x1, x2, y);


    }


    public class SimulateTradingViewModel : BaseViewModel
    {

        public const string SL = "Stop-loss";
        public const string TP = "Take-profit";
        public const string USER = "User";
        
        private Candle[] candles;
        private BybitSecurity security;
        private SimulatorNaviViewModel? navigation;

        private Logger logger = LogManager.GetCurrentClassLogger();

        public SimulateTradingViewModel(Candle[] candles, BybitSecurity security)
        {
            this.candles = candles;
            this.security = security;
            ChangeSideCommand = new DelegateCommand(OnChangeSide, CanChangeSide);
            MakePositionCommand = new DelegateCommand(OnMakePosition, CanMakePosition);
            navigation = SimulatorNaviViewModel.Instance;
        }

        public event Action<double, string> ChangeTakeStop = delegate { };

        private void OnChangeTakeStop(double price, string arg)
        {
            ChangeTakeStop?.Invoke(price, arg);
        }

        #region properties

        private Candle _currentCandle;
        /// <summary>
        /// Текущая свеча
        /// </summary>
        public Candle CurrentCandle
        {
            get => _currentCandle;
            set
            {
                _currentCandle = value;
                NotifyPropertyChanged(nameof(Price));
            }
        }

        /// <summary>
        /// Цена потенциальной заявки
        /// </summary>
        public double Price
        {
            get => CurrentCandle.Close;
        }

        private double _size = 10000.0;
        /// <summary>
        /// Объем заявки (USD)
        /// </summary>
        public double Size
        {
            get => _size;
            set
            {
                _size = value;
                NotifyPropertyChanged();
            }
        }

        private Sides _side = Sides.Buy;
        /// <summary>
        /// Направление заявки (позиции)
        /// </summary>
        public Sides Side
        {
            get => _side;
            set
            {
                _side = value;
                NotifyPropertyChanged();
            }
        }

        private SolidColorBrush _sideColor = new SolidColorBrush(Colors.LightGreen);
        /// <summary>
        /// Цвет кнопки Side
        /// </summary>
        public SolidColorBrush SideColor
        {
            get => _sideColor;
            set
            {
                _sideColor = value;
                NotifyPropertyChanged();
            }
        }

        private double _stopLoss;
        /// <summary>
        /// Уровень стоп-лосса
        /// </summary>
        public double StopLoss
        {
            get 
            {
                OnChangeTakeStop(_stopLoss, "stop");
                return Math.Round(_stopLoss, security.PriceScale); 
            }
            set
            {
                _stopLoss = value;
                _takeProfit = Side == Sides.Buy ? Price + (LossAbs * TakeStopRatio) : Price - (LossAbs * TakeStopRatio);
                NotifyProfitLoss();
            }
        }

        /// <summary>
        /// Потенциальный убыток при срабатывании стоп-лосса 
        /// </summary>
        public double LossAbs
        {
            get => Math.Abs(StopLoss - Price);
        }

        /// <summary>
        /// Потенциальный убыток при срабатывании стоп-лосса (%)
        /// </summary>
        public double LossPer
        {
            get => Math.Round(100 * (LossAbs / Price),2);
        }

        private int _slOffset = 2;
        /// <summary>
        /// Число тиков, которые нужно отступить от тени свечи при автовычисления стоп-лосса
        /// </summary>
        public int SLOffset
        {
            get => _slOffset;
            set
            {
                _slOffset = value;
                NotifyPropertyChanged();
            }
        }

        private double _takeProfit;
        /// <summary>
        /// Уровень тейк-прифита
        /// </summary>
        public double TakeProfit
        {
            get 
            {
                OnChangeTakeStop(_takeProfit, "take");
                return Math.Round(_takeProfit, security.PriceScale); 
            }
            set
            {
                _takeProfit = value;
                var tpOffset = Math.Abs(Price - _takeProfit);
                var slOffset = Math.Abs(Price - StopLoss);
                 _takeStopRatio = tpOffset / slOffset;
                NotifyProfitLoss();
            }
        }

        /// <summary>
        /// Потенциальная прибыль при срабатываниии тейк-профита
        /// </summary>
        public double ProfitAbs
        {
            get => Math.Abs(TakeProfit - Price);
        }

        /// <summary>
        /// Потенциальная прибыль при срабатываниии тейк-профита (%)
        /// </summary>
        public double ProfitPer
        {
            get => Math.Round(100 * (ProfitAbs / Price), 2);
        }

        private double _takeStopRatio = 3;
        /// <summary>
        /// Соотношение тейк/профит
        /// </summary>
        public double TakeStopRatio
        {
            get => Math.Round(_takeStopRatio, 2);
            set
            {
                _takeStopRatio = value;
                _takeProfit = Side == Sides.Buy ? Price + (LossAbs * _takeStopRatio) : Price - (LossAbs * _takeStopRatio);
                NotifyProfitLoss();
            }
        }

        private TradeFormations _formation = TradeFormations.Retest;
        /// <summary>
        /// Торговая формация (причина открытия позиции)
        /// </summary>
        public TradeFormations Formation
        {
            get => _formation;
            set
            {
                _formation = value;
                NotifyPropertyChanged();
            }
        }

        private double _takerFee = 0.1;
        /// <summary>
        /// Комиссия тейкера (%)
        /// </summary>
        public double TakerFee
        {
            get => _takerFee;
            set
            {
                _takerFee = value;
                NotifyPropertyChanged();
            }
        }

        private double _makerFee = 0.03;
        /// <summary>
        /// Комиссия мейкера (%)
        /// </summary>
        public double MakerFee
        {
            get => _makerFee;
            set
            {
                _makerFee = value;
                NotifyPropertyChanged();
            }
        }

        private string _closeReason;
        /// <summary>
        /// Причина закрытия позиции
        /// </summary>
        public string CloseReason
        {
            get => _closeReason;
            set
            {
                _closeReason = value;
                NotifyPropertyChanged();
            }
        }

        private void NotifyProfitLoss()
        {
            NotifyPropertyChanged(nameof(TakeProfit));
            NotifyPropertyChanged(nameof(ProfitAbs));
            NotifyPropertyChanged(nameof(ProfitPer));
            NotifyPropertyChanged(nameof(StopLoss));
            NotifyPropertyChanged(nameof(LossAbs));
            NotifyPropertyChanged(nameof(LossPer));
        }

        #endregion

        #region position

        /// <summary>
        /// Причина закрытия позиции
        /// </summary>
        public string MakePositionContent
        {
            get => Position?.State == PositionStates.Open ? "Закрыть позицию" : "Открыть позицию";
        }

        public List<Position> Positions { private set; get; } = new();


        private Position _position;
        /// <summary>
        /// Позиция
        /// </summary>
        public Position Position 
        {
            get => _position;
            set
            {
                _position = value;
                NotifyPropertyChanged();
            }
        }

        /// <summary>
        /// Заголовок блока позиция
        /// </summary>
        public string PositionState
        {
            get 
            {
                string state = Position == null ? "OFF" : Position.State.ToString();
                return $"Position: {state}"; 
            }
        }

        //public TimeSpan Duration
        //{
        //    get => Close ;
        //}



        #endregion

        #region commands 

        public DelegateCommand ChangeSideCommand { get; set; }

        private void OnChangeSide(object arg)
        {
            Side = Side == Sides.Buy ? Sides.Sell : Sides.Buy;
            if (Side == Sides.Sell)
                SideColor = new SolidColorBrush(Colors.LightPink);
            else SideColor = new SolidColorBrush(Colors.LightGreen);
        }

        private bool CanChangeSide(object arg)
        {
            return true;
        }

        public DelegateCommand MakePositionCommand { get; set; }

        private void OnMakePosition(object arg)
        {

            if(Position?.State == PositionStates.Open)
            {
                Position.ForseClose(CurrentCandle, TakerFee);
            }
            else
            {
                if (Position?.State == PositionStates.Close)
                    ClearPosition();
                Position = new Position(CurrentCandle, Side, Size, TakerFee);
            }

            NotifyPropertyChanged(nameof(PositionState));
        }

        private bool CanMakePosition(object arg)
        {
            return true;
        }

        #endregion

        /// <summary>
        /// Проверяет последовательность свечей на условия закрытия позиции. Метод запускают, если есть открытая позиция.
        /// Если позиция не закрыта, то возвращается индекс последнй свечи, если закрыта, то индекс свечи, на которой выполнено закрытие.
        /// </summary>
        /// <param name="curcandle">текущая свеча</param>
        /// <param name="steps">число шагов, если =-1, то проверка проводится по всем свечам</param>
        /// <returns></returns>
        public int CheckForClose(Candle curcandle, int steps)
        {

            try
            {

                if (Position?.State != PositionStates.Open)
                    throw new Exception("Нет открытой позиции.");

                var idx = Array.IndexOf(candles, curcandle);

                if (idx < 0)
                    throw new InvalidOperationException("Не найден индекс текущей свечи.");

                int cnt = steps > 0 ? idx + steps : candles.Length - 1;

                int i = 0;

                for (i = idx + 1; i <= cnt; i++)
                {
                    if (Position.IsClose(candles[i], StopLoss, TakeProfit, MakerFee))
                    {
                        Positions.Add(Position.Clone());
                        NotifyPropertyChanged(nameof(PositionState));
                        break;
                    }

                    // если достигнут конец списка, а условия закрытия позиции не выполнены,
                    // выполняется принудительное закрытие по последней свече
                    if(steps == -1 && i == cnt)
                    {
                        Position.ForseClose(candles[i], TakerFee);
                        Positions.Add(Position.Clone());
                    }

                }

                return i;
            }
            catch (Exception ex)
            {
                logger.ToError(ex);
            }

            return -1;


        }

        public void ClearPosition()
        {
            Position = null;
        }

        private void Navigation_NaviAction(string action, int bars)
        {
            switch (action)
            {
                case  SimulatorViewModel.NEXTBAR :
                    break;
                case SimulatorViewModel.TS :
                    break;
                case SimulatorViewModel.RANGEBREAK :
                    break;
                case SimulatorViewModel.SLOPEBREAK:
                    break;
                default:
                    break;
            }

        }   

        public void OnLoaded(object sender, RoutedEventArgs e)
        {
            navigation?.NaviAction += Navigation_NaviAction;
        }

        public void OnUnloaded(object sender, RoutedEventArgs e)
        {
            navigation?.NaviAction -= Navigation_NaviAction;
        }

    }
}
