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


    // Copyright(c) [2026], [Sergey Dvortsov]
    /// <summary>
    /// A special class of position specific to the tasks of a given application
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
        /// The position direction
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
        /// The position state
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
        /// The position opening time
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
        /// The position opening price
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
        /// The exchange fee when opening a position (%).
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
        /// The position closing time
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
        /// The current time (open time of current candle)
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
        /// The position closing price
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
        /// The exchange fee when closing a position (%).
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
        /// Position size denominated in the quote asset (USD)
        /// </summary>
        public double QuoteSize { private get; set; }

        /// <summary>
        /// Position size denominated in the base asset (coins)
        /// </summary>
        public double Size 
        { 
            get => QuoteSize / OpenPrice; 
        }

        /// <summary>
        /// The position duration
        /// </summary>
        public TimeSpan Duration
        {
            get => CloseTime == DateTime.MinValue ? Time - OpenTime : CloseTime - OpenTime;
        }

        /// <summary>
        /// Profit/loss of a closed position, taking into account commission
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
        /// Relative profit/loss of position (%)
        /// </summary>
        public double PNLPer
        {
            get { return Math.Round(100 * PNL / QuoteSize, 2); }
        }

        private CloseReasons _closeReason;
        /// <summary>
        /// Reason for closing the position
        /// </summary>
        public CloseReasons CloseReason
        {
            get => _closeReason;
            set
            {
                _closeReason = value;
                NotifyPropertyChanged();
            }
        }

        #endregion

        /// <summary>
        /// Checks conditions and closes the position if stop or take profit levels are reached
        /// </summary>
        /// <param name="candle">current candle</param>
        /// <param name="sl">stop-loss trigger price</param>
        /// <param name="tp">take-profit limit price</param>
        /// <returns>true if stop or take profit levels are reached</returns>
        public bool IsClose(Candle candle, double sl, double tp, double fee)
        {
            if (State == PositionStates.Close) return true;

            CloseFee = fee;

            Time = candle.OpenTime;

            if (Side == PositionSides.LONG)
            {
                if (candle.High >= tp) // take profit level are reached
                {
                    State = PositionStates.Close;
                    CloseTime = candle.OpenTime;
                    ClosePrice = tp;
                    CloseReason = CloseReasons.TakeProfit;
                }
                else if (candle.Low <= sl) // stop loss level are reached
                {
                    State = PositionStates.Close;
                    CloseTime = candle.OpenTime;
                    ClosePrice = sl;
                    CloseReason = CloseReasons.StopLoss;
                }
            }
            else
            {
                if (candle.Low <= tp) // take profit level are reached
                {
                    State = PositionStates.Close;
                    CloseTime = candle.OpenTime;
                    ClosePrice = tp;
                    CloseReason = CloseReasons.TakeProfit;
                }
                else if (candle.High >= sl) // stop loss level are reached
                {
                    State = PositionStates.Close;
                    CloseTime = candle.OpenTime;
                    ClosePrice = sl;
                    CloseReason = CloseReasons.StopLoss;
                }
            }

            return State == PositionStates.Close;
        }

        /// <summary>
        /// Forced closing of a position at the current candle
        /// </summary>
        /// <param name="candle">текущая свеча</param>
        /// <returns>true if position state = Close</returns>
        ///<remarks>A forced close occurs when the end of the training data is reached or
        ///if the user closes the position independently for their own reasons.</remarks>
        public bool ForseClose(Candle candle, double fee)
        {
            Time = candle.OpenTime;
            CloseFee = fee;
            State = PositionStates.Close;
            CloseTime = candle.OpenTime;
            ClosePrice = candle.Close;
            CloseReason = CloseReasons.Force;
            return State == PositionStates.Close;
        }

        /// <summary>
        /// Clones an instance of a position
        /// </summary>
        /// <returns>Position</returns>
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
