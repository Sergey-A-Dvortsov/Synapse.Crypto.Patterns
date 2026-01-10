using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// Copyright(c) [2026], [Sergey Dvortsov]
namespace Synapse.Crypto.Patterns
{
    public enum CandleUpdateModes
    {
        temporary,
        pause,
        constant
    }

    public enum Periods
    {
        small,
        mid,
        large,
        h24
    }

    /// <summary>
    /// Instrument types
    /// </summary>
    public enum InstrTypes
    {
        SPOT,
        FUTURE,
        SWAP,
        PATTERN
    }

    /// <summary>
    /// The style in which the level was broken
    /// </summary>
    public enum BreakStyles
    {
        /// <summary>
        /// The level only breaks through the shadow
        /// </summary>
        shadow,
        /// <summary>
        /// The level breaks through the close price
        /// </summary>
        close,
        /// <summary>
        /// The level breaks through the entire body (i.e. the closing and opening prices must go beyond the level)
        /// </summary>
        body
    }

    public enum BreakDownSide
    {
        /// <summary>
        /// Пробит уровень снизу вверх
        /// </summary>
        upside,
        /// <summary>
        /// Пробит уровень сверху вниз
        /// </summary>
        downside
    }

    /// <summary>
    /// Chart types
    /// </summary>
    public enum ChartTypes
    {
        Сandlesticks,
        HeikenAchi
    }

    /// <summary>
    /// Single candlestick formation
    /// </summary>
    public enum CandlePatterns
    {
        /// <summary>
        /// Молот
        /// </summary>
        Hummer = 1,
        /// <summary>
        /// Волчок
        /// </summary>
        SpinningTop = 2,
        /// <summary>
        /// Додж
        /// </summary>
        Doji = 3,
        /// <summary>
        /// Трендовая свеча (HA)
        /// </summary>
        TrendCandle = 4,
        /// <summary>
        /// Маленькая трендовая свеча (HA)
        /// </summary>
        SmallCandle = 5,
        /// <summary>
        /// Неопределенная свеча (HA)
        /// </summary>
        Indefinably = 6
    }

    /// <summary>
    /// Направление движения инструмента, после маркированного паттерна
    /// </summary>
    public enum MarkupDirection
    {
        Up,
        Down
    }

    public enum RangeIntervals
    {
        Day = 1,
        Three = 3,
        Week = 7,
        Month = 30,
        None = 0
    }

    /// <summary>
    /// Trading formations
    /// </summary>
    public enum TradeFormations
    {
        /// <summary>
        /// Ретест уровня
        /// </summary>
        Retest,
        /// <summary>
        /// Ложный пробой
        /// </summary>
        FalseBreak,
        /// <summary>
        /// Пробой наклонной
        /// </summary>
        SlopeBreak,
        /// <summary>
        /// Неопределена
        /// </summary>
        Undefined
    }

    /// <summary>
    /// Reasons for closing a position
    /// </summary>
    public enum CloseReasons
    {
        TakeProfit,
        StopLoss,
        Force
    }


}
//Indefinably