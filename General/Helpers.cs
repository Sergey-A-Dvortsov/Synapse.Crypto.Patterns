using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using System.Runtime.CompilerServices;
using Synapse.Crypto.Bybit;
using System.Windows;
using ScottPlot;
using MathNet.Numerics.Statistics;
using ScottPlot.Plottables;
using Synapse.General;
using Synapse.Crypto.Trading;

namespace Synapse.Crypto.Patterns
{

    public static class Helpers
    {

        #region Candles

        /// <summary>
        /// Патерн "Волчок". Большие приблизительно одинаковые тени (разница не более 20%) и маленькое тело.
        /// Лимиты задаются в %.
        /// </summary>
        /// <param name="candle"></param>
        /// <param name="rangeLimit">Миниманый диапазон свечи. Зависит от тайм-фрейма свечи</param>
        /// <param name="bodyLimit">Максимальный размер тела относительно диапазона</param>
        /// <returns></returns>
        public static bool IsVolchok(this Candle candle, double rangeLimit, double bodyLimit = 15.0)
        {
            // если маленький диапазон
            if (candle.PerRange() < rangeLimit) return false;

            // если размеры теней различаются более чем на 20%
            if (100 * Math.Abs(candle.TopShadow() / candle.BottomShadow() - 1) > 20) return false;

            return 100 * (candle.Body() / candle.Range()) < bodyLimit;
        }

        /// <summary>
        /// Возвращает размер верхней тени свечи
        /// </summary>
        /// <param name="candle"></param>
        /// <returns></returns>
        public static double TopShadow(this Candle candle)
        {
            return candle.High - (candle.IsGreen() ? candle.Close : candle.Open);
        }

        /// <summary>
        /// Возвращает размер нижней тени свечи
        /// </summary>
        /// <param name="candle"></param>
        /// <returns></returns>
        public static double BottomShadow(this Candle candle)
        {
            return (candle.IsGreen() ? candle.Open : candle.Close) - candle.Low;
        }

        /// <summary>
        /// Возвращает истину, если свеча красная
        /// </summary>
        /// <param name="candle"></param>
        /// <returns></returns>
        public static bool IsRed(this Candle candle)
        {
            return candle.Open > candle.Close;
        }

        /// <summary>
        /// Возвращает истину, если свеча зеленая
        /// </summary>
        /// <returns></returns>
        public static bool IsGreen(this Candle candle)
        {
            return candle.Open < candle.Close;
        }

        /// <summary>
        /// Возвращает относительный размер (%) размаха (High - Low) свечи
        /// </summary>
        /// <returns></returns>
        public static double PerRange(this Candle candle)
        {
            return 100 * (candle.High / candle.Low - 1);
        }

        /// <summary>
        /// Возвращает размах свечи. Разность между High - Low
        /// </summary>
        /// <returns></returns>
        public static double Range(this Candle candle)
        {
            return candle.High - candle.Low;
        }

        /// <summary>
        /// Возвращает размер тела свечи
        /// </summary>
        /// <param name="candle"></param>
        /// <returns></returns>
        public static double Body(this Candle candle)
        {
            return Math.Abs(candle.Close - candle.Open);
        }

        /// <summary>
        /// Возвращает относительный (%) размер тела свечи.
        /// </summary>
        /// <returns></returns>
        public static double PerBody(this Candle candle)
        {
            return 100 * (Math.Max(candle.Open, candle.Close) / Math.Min(candle.Open, candle.Close) - 1);
        }

        /// <summary>
        /// Возвращает относительную разность (%) размеров верхней и нижней тени свечи.
        /// </summary>
        /// <returns></returns>
        public static double PerShadowDiff(this Candle candle)
        {
            return 100 * (Math.Max(candle.TopShadow(), candle.BottomShadow()) / Math.Min(candle.TopShadow(), candle.BottomShadow()) - 1);
        }

        /// <summary>
        /// Возвращает соотношение (%) размеров тела и наибольшей тени свечи.
        /// </summary>
        /// <returns></returns>
        public static double ShadowBodyRatio(this Candle candle)
        {
            return 100 * (Math.Max(candle.TopShadow(), candle.BottomShadow()) / candle.Body() - 1);
        }

        #endregion

        #region Retest

        /// <summary>
        /// Returns the previous day's high price
        /// </summary>
        /// <param name="candle"></param>
        /// <param name="time">Момент от которого ведется расчет, если null, то используется текущее время</param>
        /// <returns></returns>
        public static double MaxPrice(this List<Candle> candles, DateTime? time = null)
        {
            if (candles == null || candles.Count < 2) return Double.NaN;
            var ntime = time == null ? DateTime.UtcNow : time.GetValueOrDefault();
            var prevDate = ntime.Date.AddDays(-1).Date;
            var prevCandles = candles.ToArray().Where(c => c.OpenTime.Date == prevDate.Date);
            if (prevCandles == null || !prevCandles.Any()) return Double.NaN;
            return prevCandles.Max(c => c.High);
        }

        /// <summary>
        /// Returns the previous day's low price
        /// </summary>
        /// <param name="candle"></param>
        /// <param name="time">Момент от которого ведется расчет, если null, то используется текущее время</param>
        /// <returns></returns>
        public static double MinPrice(this List<Candle> candles, DateTime? time = null)
        {
            if (candles == null || candles.Count < 2) return Double.NaN;
            var ntime = time == null ? DateTime.UtcNow : time.GetValueOrDefault();
            var prevDate = ntime.Date.AddDays(-1).Date;
            var prevCandles = candles.ToArray().Where(c => c.OpenTime.Date == prevDate.Date);
            if (prevCandles == null || !prevCandles.Any()) return Double.NaN;
            return prevCandles.Min(c => c.Low);
        }

        /// <summary>
        /// Возвращает характеристики цен (IntervalRange) из заданного интервала
        /// </summary>
        /// <param name="interval">интервал</param>
        /// <param name="exact">если true, то при недостаточном количестве свечей для определенного интервала в candles, возвращается null, 
        /// в противном случае возвращаются IntervalRange, расчитанный на имеющемся наборе свечей</param>
        /// <param name="time">Момент от которого ведется расчет, если null, то используется текущее время</param>
        /// <returns></returns>
        public static IntervalRange? GetIntervalRange(this List<Candle> candles, RangeIntervals interval, bool exact = true, DateTime? time = null)
        {
            if (candles == null || candles.Count < 2 || interval == RangeIntervals.None) return null;

            var ntime = time == null ? DateTime.UtcNow : time.GetValueOrDefault();
            DateTime endRangeDate = ntime.Date.AddDays(-1);
            DateTime startRangeDate = ntime.Date.AddDays(-(int)interval);

            IEnumerable<Candle>? rangeCandles = null;

            if (exact)
            {
                switch (interval)
                {
                    case RangeIntervals.Day:
                        {
                            if (candles.First().OpenTime > endRangeDate) return null; // если нет всех свечей за предыдущий день
                            rangeCandles = candles.ToArray().Where(c => c.OpenTime.Date == endRangeDate.Date);
                            break;
                        }
                    case RangeIntervals.Three:
                        {
                            //DateTime prev3date = ntime.Date.AddDays(-3);
                            if (candles.First().OpenTime > startRangeDate) return null; // если нет всех свечей за предыдущие три дня 
                            rangeCandles = candles.ToArray().Where(c => c.OpenTime.Date >= startRangeDate.Date && c.OpenTime.Date <= endRangeDate.Date);
                            break;
                        }
                    case RangeIntervals.Week:
                        {
                            //DateTime prev7date = ntime.Date.AddDays(-7);
                            if (candles.First().OpenTime > startRangeDate) return null; // если нет всех свечей за предыдущие семь дней
                            rangeCandles = candles.ToArray().Where(c => c.OpenTime.Date >= startRangeDate.Date && c.OpenTime.Date <= endRangeDate.Date);
                            break;
                        }
                    case RangeIntervals.Month:
                        {
                            //DateTime prev30date = ntime.Date.AddDays(-30);
                            if (candles.First().OpenTime > startRangeDate) return null; // если нет всех свечей за предыдущие 30 дней
                            rangeCandles = candles.ToArray().Where(c => c.OpenTime.Date >= startRangeDate.Date && c.OpenTime.Date <= endRangeDate.Date);
                            break;
                        }
                    default:
                        break;
                }
            }
            else
            {
                if (interval == RangeIntervals.Day)
                    rangeCandles = candles.ToArray().Where(c => c.OpenTime.Date >= endRangeDate.Date && c.OpenTime.Date < ntime.Date);
                else
                    rangeCandles = candles.ToArray().Where(c => c.OpenTime.Date >= startRangeDate.Date && c.OpenTime.Date <= endRangeDate.Date);
            }

            if (rangeCandles == null) return null;

            var max = rangeCandles.Max(c => c.High);
            var maxtime = rangeCandles.FirstOrDefault(c => c.High == max).OpenTime;
            var min = rangeCandles.Min(c => c.Low);
            var mintime = rangeCandles.FirstOrDefault(c => c.Low == min).OpenTime;

            var range = new IntervalRange(interval)
            {
                Start = rangeCandles.First().OpenTime,
                End = rangeCandles.Last().OpenTime,
                Max = max,
                Min = min,
                MaxTime = maxtime,
                MinTime = mintime
            };

            return range;
        }

        /// <summary>
        /// Определяет были ли пробой максимальной цены предыдущего дня. Если был, то возвращает время пробоя, если нет, то null.
        /// </summary>
        /// <param name="candle"></param>
        /// <returns></returns>
        public static DateTime? HighBreakDown(this List<Candle> candles, double highPrice, TimeFrames frame, BreakStyles style, DateTime? nowTime = null)
        {
            var ntime = nowTime == null ? DateTime.UtcNow : nowTime.GetValueOrDefault();

            Candle[] nowCandles = [.. candles.ToArray().Where(c => c.OpenTime.Date == ntime.Date)];

            if (frame == TimeFrames.Hour)
                nowCandles = [.. nowCandles.ToHourInterval()];

            Candle breakCandle;

            if (style == BreakStyles.body)
                breakCandle = nowCandles.FirstOrDefault(c => c.Open > highPrice && c.Close > highPrice);
            else if (style == BreakStyles.close)
                breakCandle = nowCandles.FirstOrDefault(c => c.Close > highPrice);
            else
                breakCandle = nowCandles.FirstOrDefault(c => c.High > highPrice);

            if (breakCandle.OpenTime == DateTime.MinValue) return null;
            return breakCandle.OpenTime;
        }

        /// <summary>
        /// Определяет были ли пробой минимальной цены предыдущего дня. Если был, то возвращает время пробоя, если нет, то null.
        /// </summary>
        /// <param name="candle"></param>
        /// <returns></returns>
        public static DateTime? LowBreakDown(this List<Candle> candles, double lowPrice, TimeFrames frame, BreakStyles style, DateTime? nowTime = null)
        {
            var ntime = nowTime == null ? DateTime.UtcNow : nowTime.GetValueOrDefault();

            Candle[] nowCandles = [.. candles.ToArray().Where(c => c.OpenTime.Date == ntime.Date)];

            if (frame == TimeFrames.Hour)
                nowCandles = [.. nowCandles.ToHourInterval()];

            Candle breakCandle;

            if (style == BreakStyles.body)
                breakCandle = nowCandles.FirstOrDefault(c => c.Open < lowPrice && c.Close < lowPrice);
            else if (style == BreakStyles.close)
                breakCandle = nowCandles.FirstOrDefault(c => c.Close < lowPrice);
            else
                breakCandle = nowCandles.FirstOrDefault(c => c.Low < lowPrice);

            if (breakCandle.OpenTime == DateTime.MinValue) return null;
            return breakCandle.OpenTime;
        }

        /// <summary>
        /// Определяет был ли ретест после пробоя максимальной цены предыдущего дня. Если был, то возвращает время ретеста, если нет, то null.
        /// </summary>
        /// <param name="candle"></param>
        /// <returns></returns>
        public static DateTime? HighRetest(this List<Candle> candles, double highPrice, TimeFrames frame, DateTime breakTime)
        {
            Candle[] temp = [.. candles];

            // свечи после пробоя уровня
            var nextBreakCandles = frame == TimeFrames.Hour ? temp.ToHourInterval().Where(c => c.OpenTime > breakTime) : temp.Where(c => c.OpenTime > breakTime);

            // свеча, пробившая уровень в обратном направлении
            var retestCandle = nextBreakCandles.FirstOrDefault(c => c.Low < highPrice);

            if (retestCandle.OpenTime == DateTime.MinValue) return null;
            return retestCandle.OpenTime;
        }

        /// <summary>
        /// Определяет был ли ретест после пробоя минимальной цены предыдущего дня. Если был, то возвращает время ретеста, если нет, то null.
        /// </summary>
        /// <param name="candle"></param>
        /// <returns></returns>
        public static DateTime? LowRetest(this List<Candle> candles, double lowhPrice, TimeFrames frame, DateTime breakTime)
        {
            Candle[] temp = [.. candles];

            // свечи после пробоя уровня
            var nextBreakCandles = frame == TimeFrames.Hour ? temp.ToHourInterval().Where(c => c.OpenTime > breakTime) : temp.Where(c => c.OpenTime > breakTime);

            // свеча, пробившая уровень в обратном направлении
            var retestCandle = nextBreakCandles.FirstOrDefault(c => c.High > lowhPrice);

            if (retestCandle.OpenTime == DateTime.MinValue) return null;
            return retestCandle.OpenTime;
        }

        #endregion

        /// <summary>
        /// Конвертирует свечи в часовой тайм-фрейм.
        /// </summary>
        /// <param name="candles"></param>
        /// <returns></returns>
        public static IEnumerable<Candle> ToHourInterval(this IEnumerable<Candle> candles)
        {
            var newcandles = new List<Candle>();
            DateTime openTime = DateTime.MinValue;
            double open = 0, high = 0, low = 0, close = 0, volume = 0, value = 0;
            bool isRealtime = false, confirm = true;
            Candle[] cndls = [.. candles];

            foreach (var c in cndls)
            {

                if (openTime.Date != c.OpenTime.Date || openTime.Hour != c.OpenTime.Hour)
                {
                    if (openTime != DateTime.MinValue)
                        newcandles.Add(new Candle()
                        {
                            OpenTime = openTime,
                            Open = open,
                            High = high,
                            Low = low,
                            Close = close,
                            Volume = volume,
                            Value = value,
                            Confirm = confirm,
                            IsRealtime = isRealtime
                        });

                    openTime = new DateTime(c.OpenTime.Year, c.OpenTime.Month, c.OpenTime.Day, c.OpenTime.Hour, 0, 0);
                    open = c.Open;
                    high = c.High;
                    low = c.Low;
                    close = c.Close;
                    volume = c.Volume;
                    value = c.Value;
                    isRealtime = c.IsRealtime;
                    confirm = c.Confirm;
                }
                else
                {
                    high = Math.Max(c.High, high);
                    low = Math.Min(c.Low, low);
                    close = c.Close;
                    volume += c.Volume;
                    value += c.Value;
                }


            }

            newcandles.Add(new Candle() { OpenTime = openTime, Open = open, High = high, Low = low, Close = close, Volume = volume, Value = value });

            return newcandles;
        }

        /// <summary>
        /// Конвертирует японские свечи в свечи Heikin Ashi (HA).
        /// </summary>
        /// <param name="candles"></param>
        /// <returns></returns>
        public static List<Candle> ToHeikinAshi(this List<Candle> candles)
        {
            Candle[] temp = [.. candles];
            var hacandles = new List<Candle>();
            Candle firstCandle = temp.First();

            // Первая свеча HA
            Candle fhaCandle = new()
            {
                OpenTime = firstCandle.OpenTime,
                Open = firstCandle.Open,
                High = firstCandle.High,
                Low = firstCandle.Low,
                Close = firstCandle.HeikinAshiClose(),
                Volume = firstCandle.Volume,
                Value = firstCandle.Value
            };

            hacandles.Add(fhaCandle);

            for (var i = 1; i < temp.Length; i++)
            {
                var close = temp[i].HeikinAshiClose();
                var open = (hacandles[i - 1].Open + hacandles[i - 1].Close) / 2;
                var high = Math.Max(Math.Max(close, open), temp[i].High);
                var low = Math.Min(Math.Max(close, open), temp[i].Low);

                hacandles.Add(new Candle
                {
                    OpenTime = temp[i].OpenTime,
                    Open = open,
                    High = high,
                    Low = low,
                    Close = close,
                    Volume = temp[i].Volume,
                    Value = temp[i].Value
                }
                );

            }

            return hacandles;
        }

        /// <summary>
        /// Возвращает Close в нотации HeikinAshi
        /// </summary>
        /// <param name="candle"></param>
        /// <returns></returns>
        public static double HeikinAshiClose(this Candle candle)
        {
            return (candle.Open + candle.High + candle.Low + candle.Close) / 4;
        }

        /// <summary>
        /// Конвертирует свечи в формат OHLC ScottPlot
        /// </summary>
        /// <param name="candles"></param>
        /// <returns></returns>
        public static List<OHLC>? ToOHLC(this List<Candle> candles)
        {
            Candle[] temp = [.. candles];
            if (temp.Length < 2) return null;
            var ts = temp[1].OpenTime - temp[0].OpenTime;
            return [.. temp.Select(c => new OHLC(c.Open, c.High, c.Low, c.Close, c.OpenTime, ts))];
        }

        /// <summary>
        /// Округляет время с заданной точностью
        /// </summary>
        /// <param name="time"></param>
        /// <param name="prec"></param>
        /// <returns></returns>
        public static DateTime Round(this DateTime time, TimeSpan ts)
        {
            long roundedTicks = (long)Math.Round(time.Ticks / (double)ts.Ticks) * ts.Ticks;
            return new DateTime(roundedTicks);
        }

        /// <summary>
        /// Конвертирует CMCCoinInfo в сокращенный формат CoinCupInfo
        /// </summary>
        /// <returns></returns>
        public static CoinCupInfo ToCoinCupInfo(this CMCCoinInfo info)
        {
            return new CoinCupInfo()
            {
                Symbol = info.symbol,
                AddedTime = info.date_added,
                LastUpdatedTime = info.last_updated,
                Rank = info.cmc_rank,
                MaxSupply = info.max_supply,
                CirculatSupply = info.circulating_supply,
                TotalSupply = info.total_supply,
                Price = info.quote.USD.price,
                MarketCap = info.quote.USD.market_cap,
                MarketCapDominance = info.quote.USD.market_cap_dominance
            };
        }

        /// <summary>
        /// Возвращает структуру с данными описательной статистики к контексте волатильности инструмента
        /// </summary>
        /// <param name="candles">набор свечей</param>
        /// <param name="symbol">инструмент</param>
        /// <returns></returns>
        public static VolatilityParams GetVolatilityParams(this IEnumerable<Candle> candles, string symbol)
        {
            var descrStat = new DescriptiveStatistics(candles.ToArray().Select(c => c.PerRange()));

            return new VolatilityParams()
            {
                Symbol = symbol,
                Average = descrStat.Mean,
                Max = descrStat.Maximum,
                Min = descrStat.Minimum,
                SD = descrStat.StandardDeviation
            };
        }

        public static DateTime PvevDayStart(this DateTime time)
        {
            return time.Date.AddDays(-1);
        }

        public static DateTime PvevDayEnd(this DateTime time)
        {
            return time.Date.AddMicroseconds(-1);
        }

        public static double PvevAODayStart(this DateTime time)
        {
            var dt = time.PvevDayStart();
            return dt.ToOADate();
        }

        public static double PvevAODayEnd(this DateTime time)
        {
            var dt = time.PvevDayEnd();
            return dt.ToOADate();
        }

        /// <summary>
        /// Возвращает расстояние между координатами Y горизонтальной линии и курсора мыши
        /// </summary>
        /// <param name="line">линия</param>
        /// <param name="mouseY">координата Y курсора</param>
        /// <returns></returns>
        public static double Grab(this HorizontalLine line, double mouseY)
        {
            return 100 * Math.Abs((line.Y - mouseY) / line.Y);
        }


        public static TimeSpan ToTimeSpan(this RangeIntervals interval)
        {
            return TimeSpan.FromDays((int)interval);
        }

        #region testing

        /// <summary>
        /// Возвращает стастику тестирования стратегий или тестовой торговли на исторических данных
        /// </summary>
        /// <param name="positions"></param>
        /// <returns></returns>
        public static PositionStatistic Statistic(this Position[] positions)
        {
            PositionStatistic statistic = new();

            Position[] shorts = [.. positions.Where(p => p.Side == PositionSides.SHORT)];
            statistic.ShortPositionCount = shorts.Length;
            statistic.ShortPNL = shorts.Sum(p => p.PNL);
            statistic.ShortWin = shorts.Count(p => p.PNL > 0);
            statistic.ShortProfit = shorts.Where(p => p.PNL > 0).Sum(p => p.PNL);
            statistic.ShortLoss = shorts.Where(p => p.PNL < 0).Sum(p => p.PNL);

            Position[] longs = [.. positions.Where(p => p.Side == PositionSides.LONG)];
            statistic.LongPositionCount = longs.Length;
            statistic.LongPNL = longs.Sum(p => p.PNL);
            statistic.LongWin = longs.Count(p => p.PNL > 0);
            statistic.LongProfit = longs.Where(p => p.PNL > 0).Sum(p => p.PNL);
            statistic.LongLoss = longs.Where(p => p.PNL < 0).Sum(p => p.PNL);

            statistic.StartTime = positions.First().OpenTime;
            statistic.EndTime = positions.Last().Time;

            return statistic;
        }

        #endregion

    }
}