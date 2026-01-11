using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Synapse.Crypto.Patterns
{
    // Copyright(c) [2026], [Sergey Dvortsov]
    /// <summary>
    /// Structure for storing information about the marked candle.
    /// </summary>
    public struct CandleMarkup : INotifyPropertyChanged
    {

        public CandleMarkup() { }

        private ChartTypes _chartType = ChartTypes.Сandlesticks;
        /// <summary>
        /// Chart type
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
        /// Markup candle's open time
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

        private CandlePatterns _pattern;
        /// <summary>
        /// Pattern
        /// </summary>
        public CandlePatterns Pattern 
        {
            get => _pattern;
            set 
            { 
                _pattern = value;
                NotifyPropertyChanged();
            }
        }

        private MarkupDirection _direction;
        /// <summary>
        /// Направление движения рынка после размеченной свечи
        /// </summary>
        public MarkupDirection Direction 
        {
            get => _direction;
            set 
            { 
                _direction = value;
                NotifyPropertyChanged();
            } 
        }

        public override string ToString()
        {
            return ChartType == ChartTypes.Сandlesticks ? $"{Time};{Pattern.ToString()[0]};{Direction.ToString()[0]}" : $"{Time};{Pattern}";
        }

        /// <summary>
        /// Parse from string. 
        /// </summary>
        /// <param name="line">data string</param>
        /// <returns>CandleMarkup</returns>
        public static CandleMarkup Parse(string line, ChartTypes chartType = ChartTypes.Сandlesticks)
        {
            var arr = line.Split(";");

            CandleMarkup markup = new()
            {
                Time = DateTime.Parse(arr[0])
            };

            if (chartType == ChartTypes.Сandlesticks)
            {
                markup.Pattern = arr[1] == "H" ? CandlePatterns.Hummer : arr[1] == "S" ? CandlePatterns.SpinningTop : CandlePatterns.Doji;
                markup.Direction = arr[2] == "U" ? MarkupDirection.Up : MarkupDirection.Down;
            }
            else if(chartType == ChartTypes.HeikenAchi)
            {
                markup.Pattern = (CandlePatterns)Enum.Parse(typeof(CandlePatterns), arr[1]);
            }

            return markup;
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
