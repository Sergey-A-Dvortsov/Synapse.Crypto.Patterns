using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    /// Тип инструмента
    /// </summary>
    public enum InstrTypes
    {
        SPOT,
        FUTURE,
        SWAP,
        PATTERN
    }

    public enum BreakStyles
    {
        /// <summary>
        /// Уровень пробивает только тень
        /// </summary>
        shadow,
        /// <summary>
        /// Уровень пробивает цена закрытия
        /// </summary>
        close,
        /// <summary>
        /// Уровень пробивает все тело (т.е. цена закрытия и открытия должна уйти за уровень)
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

    public enum ChartTypes
    {
        Сandlesticks,
        HeikenAchi
    }

    /// <summary>
    /// Одиночная свечная формация
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

    //public enum HeikinAshiPatterns : CandlePatterns
    //{
    //    /// <summary>
    //    /// Молот
    //    /// </summary>
    //    Hummer,
    //    /// <summary>
    //    /// Волчок
    //    /// </summary>
    //    SpinningTop,
    //    /// <summary>
    //    /// Додж
    //    /// </summary>
    //    Doji
    //}

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
    /// Торговые формации
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

}
//Indefinably