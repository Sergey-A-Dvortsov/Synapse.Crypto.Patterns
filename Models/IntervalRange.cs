using Synapse.Crypto.Bybit;
using Synapse.Crypto.Trading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Synapse.Crypto.Patterns
{
    // Copyright(c) [2026], [Sergey Dvortsov]
    /// <summary>
    /// Набор характеристик ценового интервала: максимумы, минимумы и т.п.
    /// </summary>
    /// <remarks>
    /// Возможно будут добавлены дополнительные характеристики интервала, например, волатильность, ATR, средний размах свечей.
    /// </remarks>
    public class IntervalRange
    {

        public IntervalRange(RangeIntervals interval) 
        { 
            Interval = interval;
            Span = interval.ToTimeSpan(); 
        }

        public IntervalRange(TimeSpan span)
        {
            Span = span;
        }

        /// <summary>
        /// Тип/размер диапазона
        /// </summary>
        public RangeIntervals Interval {  get; }

        /// <summary>
        /// Тип/размер диапазона
        /// </summary>
        public TimeSpan Span { get; }

        /// <summary>
        /// 
        /// </summary>
        public int Test {  get; } 

        /// <summary>
        /// Максимум диапазона
        /// </summary>
        public double Max { get; set; }

        /// <summary>
        /// Минимум диапазона
        /// </summary>
        public double Min { get; set; }

        /// <summary>
        /// Время максисума
        /// </summary>
        public DateTime MaxTime { get; set; }

        /// <summary>
        /// Время минимума
        /// </summary>
        public DateTime MinTime { get; set; }

        /// <summary>
        /// Начало диапазона
        /// </summary>
        public DateTime Start { get; set; }

        /// <summary>
        /// Конец диапазона
        /// </summary>
        public DateTime End { get; set; }

        /// <summary>
        /// Создает экземпляр IntervalRange.
        /// </summary>
        /// <param name="interval">интервал</param>
        /// <param name="exact">если true, то при недостаточном количестве свечей для определенного интервала в candles, возвращается null, 
        /// в противном случае возвращаются IntervalRange, расчитанный на имеющемся наборе свечей</param>
        /// <param name="time">Момент от которого ведется расчет, если null, то используется текущее время</param>
        /// <returns></returns>
        public static IntervalRange? Create(List<Candle> candles, RangeIntervals interval, bool exact = true, DateTime? time = null)
        {
            if (candles == null || candles.Count < 2) return null;

            var ntime = time == null ? DateTime.UtcNow : time.GetValueOrDefault();
            DateTime prevdate = ntime.Date.AddDays(-1);
            IEnumerable<Candle>? prevCandles = null;

            if (exact)
            {
                switch (interval)
                {
                    case RangeIntervals.Day:
                        {
                            if (candles.First().OpenTime > prevdate) return null; // если нет всех свечей за предыдущий день
                            prevCandles = candles.ToArray().Where(c => c.OpenTime.Date == prevdate.Date);
                            break;
                        }
                    case RangeIntervals.Three:
                        {
                            DateTime prev3date = ntime.Date.AddDays(-3);
                            if (candles.First().OpenTime > prev3date) return null; // если нет всех свечей за предыдущие три дня 
                            prevCandles = candles.ToArray().Where(c => c.OpenTime.Date >= prev3date.Date && c.OpenTime.Date <= prevdate.Date);
                            break;
                        }
                    case RangeIntervals.Week:
                        {
                            DateTime prev7date = ntime.Date.AddDays(-7);
                            if (candles.First().OpenTime > prev7date) return null; // если нет всех свечей за предыдущие семь дней
                            prevCandles = candles.ToArray().Where(c => c.OpenTime.Date >= prev7date.Date && c.OpenTime.Date <= prevdate.Date);
                            break;
                        }
                    case RangeIntervals.Month:
                        {
                            DateTime prev30date = ntime.Date.AddDays(-30);
                            if (candles.First().OpenTime > prev30date) return null; // если нет всех свечей за предыдущие 30 дней
                            prevCandles = candles.ToArray().Where(c => c.OpenTime.Date >= prev30date.Date && c.OpenTime.Date <= prevdate.Date);
                            break;
                        }
                    default:
                        break;
                }
            }
            else
            {
                switch (interval)
                {
                    case RangeIntervals.Day:
                        {
                            prevCandles = candles.ToArray().Where(c => c.OpenTime.Date >= prevdate.Date && c.OpenTime.Date < ntime.Date);
                            break;
                        }
                    case RangeIntervals.Three:
                        {
                            DateTime prev3date = ntime.Date.AddDays(-3);
                            prevCandles = candles.ToArray().Where(c => c.OpenTime.Date >= prev3date.Date && c.OpenTime.Date <= prevdate.Date);
                            break;
                        }
                    case RangeIntervals.Week:
                        {
                            DateTime prev7date = ntime.Date.AddDays(-7);
                            prevCandles = candles.ToArray().Where(c => c.OpenTime.Date >= prev7date.Date && c.OpenTime.Date <= prevdate.Date);
                            break;
                        }
                    case RangeIntervals.Month:
                        {
                            DateTime prev30date = ntime.Date.AddDays(-30);
                            prevCandles = candles.ToArray().Where(c => c.OpenTime.Date >= prev30date.Date && c.OpenTime.Date <= prevdate.Date);
                            break;
                        }
                    default:
                        break;
                }

            }

            if (prevCandles == null) return null;

            var max = prevCandles.Max(c => c.High);
            var maxtime = prevCandles.FirstOrDefault(c => c.High == max).OpenTime;
            var min = prevCandles.Min(c => c.Low);
            var mintime = prevCandles.FirstOrDefault(c => c.Low == min).OpenTime;

            var range = new IntervalRange(interval)
            {
                Start = prevCandles.First().OpenTime,
                End = prevCandles.Last().OpenTime,
                Max = max,
                Min = min,
                MaxTime = maxtime,
                MinTime = mintime,
            };

            return range;
        }

        /// <summary>
        /// Создает экземпляр IntervalRange.
        /// </summary>
        /// <param name="span">интервал</param>
        /// <param name="time">Момент от которого ведется расчет, если null, то используется текущее время</param>
        /// <returns></returns>
        public static IntervalRange? Create(List<Candle> candles, TimeSpan span, DateTime? time = null)
        {


            if (candles == null || candles.Count < 2) return null;
            TimeSpan frame = candles[1].OpenTime - candles[0].OpenTime;

            if (candles.Count < (int)(span/frame)) return null;

            var ntime = time == null ? DateTime.UtcNow : time.GetValueOrDefault();

            DateTime prevDaydate = ntime.Date.AddDays(-1);
            DateTime prevdate = ntime.Date - span;

            IEnumerable<Candle>? prevCandles = null;

            if (candles.First().OpenTime > prevdate) return null; // недостаточно свечей в предыдущей истории

            prevCandles = candles.ToArray().Where(c => c.OpenTime.Date >= prevdate.Date && c.OpenTime.Date <= prevDaydate);

            if (prevCandles == null) return null;

            var max = prevCandles.Max(c => c.High);
            var maxtime = prevCandles.FirstOrDefault(c => c.High == max).OpenTime;
            var min = prevCandles.Min(c => c.Low);
            var mintime = prevCandles.FirstOrDefault(c => c.Low == min).OpenTime;

            var range = new IntervalRange(span)
            {
                Start = prevCandles.First().OpenTime,
                End = prevCandles.Last().OpenTime,
                Max = max,
                Min = min,
                MaxTime = maxtime,
                MinTime = mintime,
            };

            return range;
        }

        /// <summary>
        /// Создает экземпляр IntervalRange.
        /// </summary>
        /// <returns></returns>
        public static IntervalRange? Create(IEnumerable<Candle> cndls)
        {
            var candles = cndls.ToArray();  

            if (candles == null || candles.Length < 2) return null;

            TimeSpan frame = candles[1].OpenTime - candles[0].OpenTime;

            TimeSpan span = (candles.Last().OpenTime - candles.First().OpenTime) + frame;

            var max = candles.Max(c => c.High);
            var maxtime = candles.FirstOrDefault(c => c.High == max).OpenTime;
            var min = candles.Min(c => c.Low);
            var mintime = candles.FirstOrDefault(c => c.Low == min).OpenTime;

            var range = new IntervalRange(span)
            {
                Start = candles.First().OpenTime,
                End = candles.Last().OpenTime,
                Max = max,
                Min = min,
                MaxTime = maxtime,
                MinTime = mintime,
            };

            return range;
        }



    }
}
