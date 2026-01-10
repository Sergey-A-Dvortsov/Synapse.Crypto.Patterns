using bybit.net.api.Models.Trade;
using Microsoft.Extensions.Logging;
using NLog;
using ScottPlot.Colormaps;
using ScottPlot.TickGenerators.TimeUnits;
using Synapse.General;
using Synapse.Crypto.Bybit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;
using Synapse.Crypto.Trading;

namespace Synapse.Crypto.Patterns
{
    /// <summary>
    /// Исследование. Получение статистики по патернам. Базовый класс функционала
    /// </summary>
    public abstract class Research
    {
        public Research(List<Candle> candles)
        {
            Candles = [.. candles];
        }

        public Candle[] Candles { get; private set; }

        /// <summary>
        /// Проверка списка на наличие пропусков. При наличии пропусков возвращается списов пропусков, в противном случае null.  
        /// </summary>
        /// <returns></returns>
        public List<DateTime>? GapsValidate()
        {
            TimeSpan interval = Candles[1].OpenTime - Candles[0].OpenTime; // определяем таймфрейм

            List<DateTime> gaps = [];

            for (int i = 1; i < Candles.Length; i++)
            {
                if (Candles[i].OpenTime - Candles[i - 1].OpenTime != interval)
                {
                    gaps.Add(Candles[i - 1].OpenTime);
                }
            }

            return gaps.Count > 0 ? gaps : null;
        }



    }

    public abstract class FalseBreakout
    {
        public FalseBreakout(Candle[] candles)
        {
            Candles = candles;
        }

        public Logger Logger { get; private set; } = LogManager.GetCurrentClassLogger();
        public Candle[] Candles { get; private set; }
        public int MaxBreakCount { set; get; }
        public int MinBreakCount { set; get; }

        /// <summary>
        /// Смещение уровня стоп-лосса. 
        /// Число, которое прибавляется/вычитается из экстремума свечи, по которой расчитывается стоп.
        /// </summary>
        public double StopLossOfset { get; set; } = 1;

        /// <summary>
        /// Соотношения потенциальной прибыли к потенциальному убытку
        /// </summary>
        public double ProfitStopRatio { get; set; } = 2;

        /// <summary>
        /// Размер позиции (сделки) в USD
        /// </summary>
        public double Size { get; set; } = 10000;

        /// <summary>
        /// Комиссия тейкера в %.
        /// </summary>
        public double TakerFee { get; set; } = 0.1;

        /// <summary>
        /// Комиссия мейкера в %.
        /// </summary>
        public double MakerFee { get; set; } = 0.03;

        public IntervalRange GetRange(int intervalLength, int index)
        {
            Range rng = new(index - intervalLength, index);
            var rangeCandles = Candles.Take(rng).ToArray();
            return IntervalRange.Create(rangeCandles);
        }

        /// <summary>
        /// Симуляция процесса открытия и закрытия позиции
        /// </summary>
        /// <param name="idx">Индекс свечи, на которой открыта позиция</param>
        /// <param name="slIdx">Индекс свечи, используемой для вычисления стопа</param>
        /// <param name="side">Направление позиции</param>
        public virtual Position PositionProcessing(int idx, int slIdx, Sides side)
        {
            try
            {

                DateTime openTime = Candles[idx].OpenTime;
                double openPrice = Candles[idx].Close;
                double stopLoss = side == Sides.Buy ? Candles[slIdx].Low - StopLossOfset : Candles[slIdx].High + StopLossOfset;

                double loss = Math.Abs(openPrice - stopLoss);
                double profit = loss * ProfitStopRatio;
                double takeprofit = side == Sides.Buy ? openPrice + profit : openPrice - profit;

                Position position = new(openTime, openPrice, side, Size, TakerFee);

                for (int i = idx + 2; i < Candles.Length; i++)
                {
                    if (position.IsClose(Candles[i], stopLoss, takeprofit, MakerFee)) break;

                    if (i == Candles.Length - 1)
                        position.ForseClose(Candles[i], TakerFee);

                }

                return position;
            }
            catch (Exception ex)
            {
                Logger.ToError(ex);
            }

            return null;
        }

        /// <summary>
        /// Возвращает true если паттерн идентифицирован.
        /// </summary>
        /// <param name="firstCandle"></param>
        /// <param name="secondCandle"></param>
        /// <param name="range"></param>
        /// <returns></returns>
        public abstract bool PatternIdentified(Candle firstCandle, Candle secondCandle, IntervalRange range, BreakDownSide side);

    }

    public class SimpleFalseBreakout : FalseBreakout
    {
        public SimpleFalseBreakout(Candle[] candles) : base(candles)
        {
        }

        public override bool PatternIdentified(Candle firstCandle, Candle secondCandle, IntervalRange range, BreakDownSide side)
        {
            if (side == BreakDownSide.upside)
            {
                // если "пробит" верхний уровень, то
                // 1. High второй свечи должен быть ниже High второй свечи.
                // 2. Close второй свечи должен быть ниже максимума диапазона 
                // TODO Здесь представлен алгоритм пробоя в "общем" виде. Затем он будет выделен в отдельный
                // метод (возможно будет интерфейс), чтобы можно было использовать разные алгоритмы
                if (secondCandle.High < firstCandle.High && secondCandle.Close < range.Max)
                {
                    MaxBreakCount++;
                    return true;
                }

            }
            else if (side == BreakDownSide.downside)
            {
                // eсли "пробит" нижний уровень, то
                // 1. Low второй свечи должно быть выше Low первой свечи,
                // 2. Close второй свечи должен быть выше минимума диапазона
                if (secondCandle.Low > firstCandle.Low && secondCandle.Close > range.Min)
                {
                    MinBreakCount++;
                    return true;
                }
            }
                return false;

        }
    }

    public class ContrColorFalseBreakout : FalseBreakout
    {
        public ContrColorFalseBreakout(Candle[] candles) : base(candles)
        {
        }

        public override bool PatternIdentified(Candle firstCandle, Candle secondCandle, IntervalRange range, BreakDownSide side)
        {
            if (side == BreakDownSide.upside)
            {
                // если "пробит" верхний уровень, то
                // 1. High второй свечи должен быть ниже High второй свечи.
                // 2. Close второй свечи должен быть ниже максимума диапазона 
                // TODO Здесь представлен алгоритм пробоя в "общем" виде. Затем он будет выделен в отдельный
                // метод (возможно будет интерфейс), чтобы можно было использовать разные алгоритмы
                if (secondCandle.High < firstCandle.High && secondCandle.Close < range.Max && secondCandle.IsRed())
                {
                    MaxBreakCount++;
                    return true;
                }

            }
            else if (side == BreakDownSide.downside)
            {
                // eсли "пробит" нижний уровень, то
                // 1. Low второй свечи должно быть выше Low первой свечи,
                // 2. Close второй свечи должен быть выше минимума диапазона
                if (secondCandle.Low > firstCandle.Low && secondCandle.Close > range.Min)
                {
                    MinBreakCount++;
                    return true;
                }
            }
            return false;

        }
    }

    /// <summary>
    /// Исследование статистики ложных пробоев. 
    /// </summary>
    /// <param name="candles"></param>
    public class FalseBreakoutResearch(List<Candle> candles) : Research(candles)
    {

        private Logger logger = LogManager.GetCurrentClassLogger();

        //public FalseBreakout breakout {  get; set; } = new SimpleFalseBreakout([.. candles]);

        public FalseBreakout breakout { get; set; } = new ContrColorFalseBreakout([.. candles]);


        //  Паттерн "ложный пробой" в общем виде.
        //  1. Для выявления паттерна используются две последовательные свечи.
        //  2. Экстремум первой свечи должен выйти за пределы заданного диапазона.
        //  3.1. Если "пробит" нижний уровень, то Low второй свечи должно быть выше Low первой свечи,
        //       если "пробит" верхний уровень, то High второй свечи должен быть ниже High второй свечи.
        //  3.2. Цена закрытия второй свечи должна находится в пределах диапазона.

        /// <summary>
        /// Размер предыдущего временного диапазона, для которого вычисляются ценовые экстремумы.
        /// </summary>
        public TimeSpan RangeFrame { get; set; }

        /// <summary>
        /// Смещение уровня стоп-лосса. 
        /// Число, которое прибавляется/вычитается из экстремума свечи, по которой расчитывается стоп.
        /// </summary>
        public double StopLossOfset { get; set; } = 1;

        /// <summary>
        /// Соотношения потенциальной прибыли к потенциальному убытку
        /// </summary>
        public double ProfitStopRatio { get; set; } = 2;

        /// <summary>
        /// Размер позиции (сделки) в USD
        /// </summary>
        public double Size { get; set; } = 10000;

        /// <summary>
        /// Комиссия тейкера в %.
        /// </summary>
        public double TakerFee { get; set; } = 0.1;

        /// <summary>
        /// Комиссия мейкера в %.
        /// </summary>
        public double MakerFee { get; set; } = 0.03;


        public int MaxFalseBreaks { get; private set; }
        public int MinFalseBreaks { get; private set; }
        public int FalseBreaks
        {
            get => MaxFalseBreaks + MinFalseBreaks;
        }

        public int MaxFalseFBreaks { get; private set; }
        public int MinFalseFBreaks { get; private set; }
        public int FalseFBreaks
        {
            get => MaxFalseFBreaks + MinFalseFBreaks;
        }

        public void Research()
        {
            var positions = StrategyTesting();
            var statistic = positions.Statistic();

            var y = 0;

        }


        /// <summary>
        /// Выполняет тестирование стратегии
        /// </summary>
        /// <returns></returns>
        public Position[] StrategyTesting()
        {
            int startindex = GetStartIndex();
            int intervalLength = (int)(RangeFrame / TimeSpan.FromMinutes(15));
            IntervalRange? range = null;

            List<Task<Position>> tasks = [];

            for (int i = startindex; i < Candles.Length - 2; i++)
            {
                var firstCandle = Candles[i];
                var secondCandle = Candles[i + 1];

                if (firstCandle.OpenTime.Hour == 0 && firstCandle.OpenTime.Minute == 0)
                {
                    range = breakout.GetRange(intervalLength, i);
                    breakout.MaxBreakCount = 0;
                    breakout.MinBreakCount = 0;
                }

                if (firstCandle.High > range.Max) // пробита верхняя граница
                {
                    if(breakout.PatternIdentified(firstCandle, secondCandle, range, BreakDownSide.upside))
                    {
                        if(breakout.MaxBreakCount == 1)
                        {
                            tasks.Add(Task.Factory.StartNew(() => breakout.PositionProcessing(i + 1, i, Sides.Sell)));
                        }
                    }
                }
                else if (firstCandle.Low < range.Min) // пробита нижняя граница
                {
                    if (breakout.PatternIdentified(firstCandle, secondCandle, range, BreakDownSide.downside))
                    {
                        if (breakout.MinBreakCount == 1)
                        {
                            tasks.Add(Task.Factory.StartNew(() => breakout.PositionProcessing(i + 1, i, Sides.Buy)));
                        }
                    }
                }
            }

            var tsks = tasks.ToArray();

            Task.WaitAll(tsks);

            Position[] positions = [.. tsks.Select(t => t.Result)];

            return positions;

        }

        ///// <summary>
        ///// Симуляция процесса открытия и закрытия позиции
        ///// </summary>
        ///// <param name="first"></param>
        ///// <param name="second"></param>
        ///// <param name="idx"></param>
        ///// <param name="side"></param>
        //private void PositionProcessing(Candle first, Candle second, int idx, Sides side)
        //{
        //    double openPrice = second.Close;
        //    double stopLoss = side == Sides.Buy ? first.Low - 1 : first.High + 1;
        //    double loss = Math.Abs(openPrice - stopLoss);
        //    double profit = loss * 2;

        //    double takeprofit = side == Sides.Buy ? openPrice + profit : openPrice - profit;

        //    for (int i = idx + 2; i < Candles.Length; i++)
        //    {
        //        var candle = Candles[i];

        //        if (side == Sides.Buy)
        //        {
        //            if (candle.High >= takeprofit)
        //            {
        //                //TODO
        //            }
        //            else if (candle.Low <= stopLoss)
        //            {
        //                //TODO
        //            }
        //        }
        //        else if (side == Sides.Sell)
        //        {
        //            if (candle.High >= stopLoss)
        //            {
        //                //TODO
        //            }
        //            else if (candle.Low <= takeprofit)
        //            {
        //                //TODO
        //            }
        //        }

        //    }

        //}

        ///// <summary>
        ///// Симуляция процесса открытия и закрытия позиции
        ///// </summary>
        ///// <param name="idx">Индекс свечи, на которой открыта позиция</param>
        ///// <param name="slIdx">Индекс свечи, используемой для вычисления стопа</param>
        ///// <param name="side">Направление позиции</param>
        //private Position PositionProcessing(int idx, int slIdx, Sides side)
        //{
        //    try
        //    {

        //        DateTime openTime = Candles[idx].OpenTime;
        //        double openPrice = Candles[idx].Close;
        //        double stopLoss = side == Sides.Buy ? Candles[slIdx].Low - StopLossOfset : Candles[slIdx].High + StopLossOfset;

        //        double loss = Math.Abs(openPrice - stopLoss);
        //        double profit = loss * ProfitStopRatio;
        //        double takeprofit = side == Sides.Buy ? openPrice + profit : openPrice - profit;

        //        Position position = new(openTime, openPrice, side, Size, TakerFee);

        //        for (int i = idx + 2; i < Candles.Length; i++)
        //        {
        //            if (position.IsClose(Candles[i], stopLoss, takeprofit, MakerFee)) break;

        //            if (i == Candles.Length - 1)
        //                position.ForseClose(Candles[i], TakerFee);

        //        }

        //        return position;
        //    }
        //    catch (Exception ex)
        //    {
        //        logger.ToError(ex);
        //    }

        //    return null;

        //}

        /// <summary>
        /// Индекс свечи, с которой начинается мониторинг паттернов
        /// </summary>
        /// <returns></returns>
        private int GetStartIndex()
        {
            var dt = Candles[0].OpenTime.Date + TimeSpan.FromDays(1) + RangeFrame;
            Candle candle = Candles.FirstOrDefault(c => c.OpenTime == dt);
            return Array.IndexOf(Candles, candle);
        }

    }




}
