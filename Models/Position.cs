using Synapse.General;
using Synapse.Crypto.Bybit;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Synapse.Crypto.Trading;

namespace Synapse.Crypto.Patterns
{
    /// <summary>
    /// Направление позиции
    /// </summary>
    public enum PositionSides
    {
        LONG,
        SHORT
    }

    /// <summary>
    /// Состояние позиции
    /// </summary>
    public enum PositionStates
    {
        Open,
        Close
    }

    /// <summary>
    /// Позиция
    /// </summary>
    public class Position : INotifyPropertyChanged
    {

        public Position() { }

        public Position(Candle candel, Sides side, double quoteSize, double openfee) 
        { 
            OpenTime = candel.OpenTime;
            OpenPrice = candel.Close;
            Time = candel.OpenTime;
            OpenFee = openfee;
            QuoteSize = quoteSize;

            if(side == Sides.Buy)
            {
                Side = PositionSides.LONG;
            }
            else
            {
                Side = PositionSides.SHORT;
            }

        }

        public Position(DateTime openTime, double openPrice, Sides side, double quoteSize, double openfee)
        {
            OpenTime = openTime;
            OpenPrice = openPrice;
            Time = openTime;
            OpenFee = openfee;
            QuoteSize = quoteSize;

            if (side == Sides.Buy)
                Side = PositionSides.LONG;
            else
                Side = PositionSides.SHORT;

        }

        #region properties

        private PositionSides _side;
        /// <summary>
        /// Направление позиции
        /// </summary>
        public PositionSides Side
        {
            get => _side;
            set
            {
                _side = value;
                NotifyPropertyChanged();
            }
        }

        private PositionStates _state;
        /// <summary>
        /// Состояние позиции
        /// </summary>
        public PositionStates State
        {
            get => _state;
            set
            {
                _state = value;
                NotifyPropertyChanged();
            }
        }

        private DateTime _openTime;
        /// <summary>
        /// Время открытия позиции
        /// </summary>
        public DateTime OpenTime
        {
            get => _openTime;
            set
            {
                _openTime = value;
                NotifyPropertyChanged();
            }
        }

        private double _openPrice;
        /// <summary>
        /// Цена открытия позиции
        /// </summary>
        public double OpenPrice
        {
            get => _openPrice;
            set
            {
                _openPrice = value;
                NotifyPropertyChanged();
            }
        }

        private double _openFee;
        /// <summary>
        /// %
        /// </summary>
        public double OpenFee
        {
            get => _openFee;
            set
            {
                _openFee = value;
                NotifyPropertyChanged();
            }
        }

        private DateTime _closeTime;
        /// <summary>
        /// Время закрытия позиции
        /// </summary>
        public DateTime CloseTime
        {
            get => _closeTime;
            set
            {
                _closeTime = value;
                NotifyPropertyChanged();
            }
        }

        private DateTime _time;
        /// <summary>
        /// Время закрытия позиции
        /// </summary>
        public DateTime Time
        {
            get => _time;
            set
            {
                _time = value;
                NotifyPropertyChanged();
                NotifyPropertyChanged(nameof(Duration));
            }
        }

        private double _closePrice;
        /// <summary>
        /// Цена закрытия позиции
        /// </summary>
        public double ClosePrice
        {
            get => _closePrice;
            set
            {
                _closePrice = value;
                NotifyPropertyChanged();
            }
        }

        private double _closeFee;
        /// <summary>
        /// %
        /// </summary>
        public double CloseFee
        {
            get => _closeFee;
            set
            {
                _closeFee = value;
                NotifyPropertyChanged();
            }
        }

        /// <summary>
        /// Размер позиции, номинированный в базовом активе (монетах)
        /// </summary>
        public double QuoteSize { private get; set; }

        /// <summary>
        /// Размер позиции, номинированный в базовом активе (монетах)
        /// </summary>
        public double Size 
        { 
            get => QuoteSize / OpenPrice; 
        }

        /// <summary>
        /// Длительность позиции
        /// </summary>
        public TimeSpan Duration
        {
            get => CloseTime == DateTime.MinValue ? Time - OpenTime : CloseTime - OpenTime;
        }

        /// <summary>
        /// Прибыль/убыток закрытой позиции с учетом комиссии
        /// </summary>
        public double PNL
        {
            get
            {
                double openfee = OpenFee * (OpenPrice * Size) / 100;
                double closefee = CloseFee * (ClosePrice * Size) / 100;
                int k = Side == PositionSides.LONG ? 1 : -1;
                return k * (ClosePrice - OpenPrice) * Size - openfee - closefee;
            }
        }

        /// <summary>
        /// Относительная прибыль/убыток позиции (%)
        /// </summary>
        public double PNLPer
        {
            get
            {
                return Math.Round(100 * PNL / QuoteSize, 2);
            }
        }

        #endregion

        /// <summary>
        /// Проверяет условия и закрывает позицию, если достигнуты уровни стопа или тейка
        /// </summary>
        /// <param name="candle"></param>
        /// <param name="sl"></param>
        /// <param name="tp"></param>
        /// <returns></returns>
        public bool IsClose(Candle candle, double sl, double tp, double fee)
        {
            if (State == PositionStates.Close) return true;

            CloseFee = fee;

            Time = candle.OpenTime;

            if (Side == PositionSides.LONG)
            {
                if (candle.High >= tp) // сработал тейк
                {
                    State = PositionStates.Close;
                    CloseTime = candle.OpenTime;
                    ClosePrice = tp;
                }
                else if (candle.Low <= sl) // сработал стоп
                {
                    State = PositionStates.Close;
                    CloseTime = candle.OpenTime;
                    ClosePrice = sl;
                }
            }
            else
            {
                if (candle.Low <= tp) // сработал тейк
                {
                    State = PositionStates.Close;
                    CloseTime = candle.OpenTime;
                    ClosePrice = tp;
                }
                else if (candle.High >= sl) // сработал стоп
                {
                    State = PositionStates.Close;
                    CloseTime = candle.OpenTime;
                    ClosePrice = sl;
                }
            }

            return State == PositionStates.Close;
        }

        /// <summary>
        /// Закрытие позиции по текущей свече
        /// </summary>
        /// <param name="candle">текущая свеча</param>
        /// <returns></returns>
        public bool ForseClose(Candle candle, double fee)
        {
            Time = candle.OpenTime;
            CloseFee = fee;
            State = PositionStates.Close;
            CloseTime = candle.OpenTime;
            ClosePrice = candle.Close;
            return State == PositionStates.Close;
        }

        /// <summary>
        /// Клонирует экземпляр позиции
        /// </summary>
        /// <returns></returns>
        public Position Clone()
        {
            return new()
            {
                Side = this.Side,
                State = this.State,
                OpenTime = this.OpenTime,
                OpenPrice = this.OpenPrice,
                OpenFee = this.OpenFee,
                CloseTime = this.CloseTime,
                ClosePrice = this.ClosePrice,
                CloseFee = this.CloseFee
            };
        }

        #region NotifyPropertyChanged

        public event PropertyChangedEventHandler? PropertyChanged;

        public void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

    }


}
