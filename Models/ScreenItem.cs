using bybit.net.api.Models.Trade;
using Synapse.Crypto.Bybit;
using Synapse.Crypto.Trading;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace Synapse.Crypto.Patterns
{

    [CategoryOrder("High/Low levels", 0)]
    [CategoryOrder("Break/Retest Time", 1)]
    [CategoryOrder("Дополнительно", 3)]
    [DisplayName("Retests")]
    [Description("Иннформация о ретестах")]
    public class ScreenItem : INotifyPropertyChanged
    {
        public ScreenItem(BybitSecurity security, CandleStorageInfo info)
        {
            Security = security;
            Start = info.StorageStart;
            NotifyPropertyChanged(nameof(Symbol));
        }

        public List<CandleMarkup> CandleMarkups { get; private set; } = new List<CandleMarkup>();

        [Browsable(false)]
        public readonly BybitSecurity Security;

        [Browsable(false)]
        public string Symbol
        {
            get => Security.Symbol;
        }

        private DateTime _start;
        [Browsable(false)]
        public DateTime Start
        {
            get => _start;
            private set
            {
                _start = value;
                NotifyPropertyChanged();
            }
        }

        private int _rank;
        [Browsable(false)]
        public int Rank
        {
            get => _rank;
            set
            {
                _rank = value;
                NotifyPropertyChanged();
            }
        }

        #region FH (first hour marks)

        private double _fhLenth;
        /// <summary>
        /// Длинна свечи в процентах первого часа торгов. = 100 * (Close/Open - 1) 
        /// </summary>
        [Browsable(false)]
        public double FHLenth
        {
            get => _fhLenth;
            set
            {
                _fhLenth = value;
                NotifyPropertyChanged();
            }
        }

        private double _fhRange;
        /// <summary>
        /// Диапазон свечи первого часа торгов в процентах. = 100 * (High/Low - 1)
        /// </summary>
        [Browsable(false)]
        public double FHRange
        {
            get => _fhRange;
            set
            {
                _fhRange = value;
                NotifyPropertyChanged();
            }
        }

        [Browsable(false)]
        public Side FHSide
        {
            get => FHLenth >= 0 ? Side.BUY : Side.SELL;
        }

        public void SetFHMarks(Candle[] candles)
        {
            FHLenth = 100 * (candles.Last().Close / candles.First().Open - 1);
            FHRange = 100 * (candles.Max(c => c.High) / candles.Min(c => c.Low) - 1);
            NotifyPropertyChanged(nameof(FHSide));
        }

        #endregion FH (first hour marks)


        private double _prevDayHigh;
        /// <summary>
        /// Максимальная цена предыдущего дня
        /// </summary>
        [Category("High/Low levels")]
        [PropertyOrder(0)]
        [DisplayName("Максимум")]
        [Description("Максимум заданного уровня.")]
        [ReadOnly(true)]
        public double PrevDayHigh
        {
            get => _prevDayHigh;
            set 
            { 
                _prevDayHigh = value;
                NotifyPropertyChanged();
            }
        }

        private double _prevDayLow;
        /// <summary>
        /// Ммнимальная цена предыдущего дня
        /// </summary>
        [Category("High/Low levels")]
        [PropertyOrder(1)]
        [DisplayName("Минимум")]
        [Description("Максимум заданного уровня.")]
        [ReadOnly(true)]
        public double PrevDayLow
        {
            get => _prevDayLow;
            set
            {
                _prevDayLow = value;
                NotifyPropertyChanged();
            }
        }

        private bool _isHighBreakdown;
        /// <summary>
        /// Был ли пробой максимальной цены предыдущего дня
        /// </summary>
        [Browsable(false)]
        public bool IsHighBreakdown
        {
            get => _isHighBreakdown;
            set
            {
                _isHighBreakdown = value;
                NotifyPropertyChanged();
            }
        }

        private DateTime? _highBreakTime;
        /// <summary>
        /// Время пробоя или null
        /// </summary>
        [Category("Break/Retest Time")]
        [PropertyOrder(0)]
        [DisplayName("Пробитие максимума")]
        [Description("Время пробития максимума.")]
        [ReadOnly(true)]
        public DateTime? HighBreakTime
        {
            get => _highBreakTime;
            set
            {
                _highBreakTime = value;
                NotifyPropertyChanged();
            }
        }

        private bool _isHighRetest;
        /// <summary>
        /// Был ли  ретест пробоя максимальной цены предыдущего дня
        /// </summary>
        [Browsable(false)]
        public bool IsHighRetest
        {
            get => _isHighRetest;
            set
            {
                _isHighRetest = value;
                NotifyPropertyChanged();
            }
        }

        private DateTime? _highRetestTime;
        /// <summary>
        /// Время ретеста пробоя или null
        /// </summary>
        [Category("Break/Retest Time")]
        [PropertyOrder(1)]
        [DisplayName("Ретест максимума")]
        [Description("Время ретеста максимума.")]
        [ReadOnly(true)]
        public DateTime? HighRetestTime
        {
            get => _highRetestTime;
            set
            {
                _highRetestTime = value;
                NotifyPropertyChanged();
            }
        }

        private bool _isLowBreakdown;
        /// <summary>
        /// Был ли пробой минимальной цены предыдущего дня
        /// </summary>
        [Browsable(false)]
        public bool IsLowBreakdown
        {
            get => _isLowBreakdown;
            set
            {
                _isLowBreakdown = value;
                NotifyPropertyChanged();
            }
        }


        private DateTime? _lowBreakTime;
        /// <summary>
        /// Время пробоя или null
        /// </summary>
        [Category("Break/Retest Time")]
        [PropertyOrder(2)]
        [DisplayName("Пробитие минимума")]
        [Description("Время пробития минимума.")]
        [ReadOnly(true)]
        public DateTime? LowBreakTime
        {
            get => _lowBreakTime;
            set
            {
                _lowBreakTime = value;
                NotifyPropertyChanged();
            }
        }

        private bool _isLowRetest;
        /// <summary>
        /// Был ли ретест пробоя мминимальной цены предыдущего дня
        /// </summary>
        [Browsable(false)]
        public bool IsLowRetest
        {
            get => _isLowRetest;
            set
            {
                _isLowRetest = value;
                NotifyPropertyChanged();
            }
        }

        private DateTime? _lowRetestTime;
        /// <summary>
        /// Время ретеста пробоя или null
        /// </summary>
        [Category("Break/Retest Time")]
        [PropertyOrder(3)]
        [DisplayName("Ретест минимума")]
        [Description("Время ретеста минимума.")]
        [ReadOnly(true)]
        public DateTime? LowRetestTime
        {
            get => _lowRetestTime;
            set
            {
                _lowRetestTime = value;
                NotifyPropertyChanged();
            }
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
