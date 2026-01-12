using Synapse.Crypto.Trading;
using Synapse.General;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Synapse.Crypto.Patterns
{
    public class CryptoIndexService
    {
        private AppRoot root = AppRoot.GetInstance();

        public async Task<List<TimeSeriesItem>>? FullCalculateCryptoIndex()
        {
            //// 1. Загрузить все свечи
            ////CandleFldr = @"D:\Storage\Binance\Candles\SPOT\1min";

            //OnNewStatusMessage("Начата загрузка всех минутных свечей!");

            //var startTime = DateTime.Now;

            //var candles = await GetRawCandlesFromFile(RawCandleFldr);

            //OnNewStatusMessage(string.Format("Загрузка завершена : {0}", DateTime.Now - startTime));

            //CandlesIndexValidate(candles);

            //var tpl = CalculateIndex(candles);

            //tpl.Item1.SaveToFile(Path.Combine(CryptoIndexFldr, "indexes.csv"));
            //tpl.Item2.SaveToFile(Path.Combine(CryptoIndexFldr, "supportinfo.csv"));

            //Indexes = tpl.Item1;

            //return tpl.Item1;

            return null;
        }

        private void CorrectData(DateTime date)
        {
            //var smbsDi = new DirectoryInfo(RawCandleFldr);

            //foreach (var smbDi in smbsDi.GetDirectories())
            //{
            //    var fi = smbDi.GetFiles().FirstOrDefault(f => f.Name == bnadapt.Helpers.GetFileNameFromDate(date));

            //    if (fi == null) continue;

            //    var candles = bnadapt.Helpers.GetCandlesFromFile(fi.FullName, date);

            //    var cndls = candles.Distinct<BnCandle>(new BnCandleEqualityComparer()).ToList();

            //    BinanceKlineEqualityComparer

            //    cndls.CandlesSaveToFile(fi.FullName);


            //    var t = 0;

            //    candles.Add(smbDi.Name, temp);
            //}

        }

        public List<TimeSeriesItem> LoadCryptoIndex()
        {
            //var file = Path.Combine(CryptoIndexFldr, "indexes.csv");
            //var indexes = bnadapt.Helpers.GetTimeSeriesFromFile(file);
            //return indexes;
            return null;
        }

        public async Task RelativeCryptoIndex()
        {
            // Описание индекса
            // Индекс расчитанный на базе среднего относительного изменения цены первых 100 монет из списка, упорядоченного по капитализации,
            // скоректированный по волатильности каждой монеты.
            // Название RC100 (Relative Crypto 100)
            // Начальное значение индекса 1000.
            // Начальная дата расчета 01.10.2024
            // Ребалансировка 1 раз в месяц. 1 числа, в 11 UTC. Во время ребалансировки происходит замена монет из топ 100 и пересчет коэффициентов волатильности wkv.
            // а также расчитывается RK (коррекциооный коэффициент ребалансировки) 
            // Методика:
            // RC100t = RC100t_1 * (1 + SUM((Pt/Pt_1-1) * cv)/100) * RK, где
            // RC100t - текущий коэфициент
            // RC100t-1 - коэфициент в предыдущий период
            // 1 + SUM((Pt/Pt_1-1) * wvk)/100 - среднее изменение цен компонентов индекса по отношению к предыдущему, корректированное по волатильности (wkv)
            // RK - коррекционный коэфициент, который обеспечивает "плавное сшивание" индекса после ребалансировки. 
            // cv - приведенный коэфициент волатильности = средняя волатильность по 100 инструментам / волатильность инструмента
            // волатильность рассчитывается, как средний размах часовой свечи (High - low) за последние 180 дней
            // RK = RC100t со "старыми компонентами и коэфф. волатильности" / RC100t с "новыми компонентами и коэфф. волатильности" на момент ребалансировки t

            var indexSeries = new List<TimeSeriesElement>();

            DateTime startTime = new(2024, 10, 1, 11, 0, 0); // Начало расчета индекса RC100 (Relative Crypto 100) 
            double startValue = 1000; // стартовое значение
            double RK = 1;

            // Получаем набор инструментов для расчета индекса на начальную дату.
            // 1. Берем из файла в хранилище с данными о капитализации, с датой наиболее близкой к startDate
            var fi = new DirectoryInfo(root.Folders.CoinMarketCap).GetFiles().First();
            var caps = CoinCupInfo.LoadFromStorage(fi.FullName);

            indexSeries.Add(new TimeSeriesElement { Time = startTime, Value = startValue });

            var rebalTimes = GetRebalanceTimes(startTime);

            List<RelIndexCalcItem>? indexItems = null;
            DateTime reblTime = DateTime.MinValue;

            for (var t = 0; t < rebalTimes.Count - 1; t++)
            {
                reblTime = rebalTimes[t];
                var nextReblTime = rebalTimes[t + 1];

                // Получаем список с инструментами и данными о их волатильности, расчитанными за предыдущие 180 дней
                var volStats = await GetVolatilityParams(reblTime);
                indexItems = GetRelIndexCalcItems(volStats, caps, reblTime);
                //if(t > 0)
                //{
                //    var lastIndex = indexSeries.Last().Value;
                //    var newIndex = CalculateIndexValue(indexItems, lastIndex, RK);
                //    RK = lastIndex / newIndex;
                //}
                var vls = CalculateIndexValues(indexItems, reblTime, nextReblTime, indexSeries.Last().Value, RK);

                indexSeries.AddRange(vls);
            }

            var vlss = CalculateIndexValues(indexItems, reblTime, null, indexSeries.Last().Value, RK);

            indexSeries.AddRange(vlss);

            indexSeries.SaveToFile("index.csv");

            // Получаем список с инструментами и данными о их волатильности, расчитанными за предыдущие 180 дней
            //volStats = await GetVolatilityParams(rebalTime);




        }

        // расчитывает параметры для каждого компонента, необходимые для калькуляции индекса 
        private List<RelIndexCalcItem> GetRelIndexCalcItems(List<VolatilityParams> volStats, List<CoinCupInfo> caps, DateTime reblTime)
        {
            var indexItems = new List<RelIndexCalcItem>();
            // 2.Расчитываем капитализацию для каждого инструмента из списка volStats, и затем отбираем топ 100 инструментов
            foreach (var vs in volStats)
            {
                var capInfo = caps.FirstOrDefault(c => $"{c.Symbol}USDT" == vs.Symbol);

                if (capInfo.AddedTime == DateTime.MinValue) continue;

                var prevCandle = root.Candles[vs.Symbol].FirstOrDefault(c => c.OpenTime == reblTime.AddMinutes(-15));
                var candle = root.Candles[vs.Symbol].FirstOrDefault(c => c.OpenTime == reblTime);

                var item = new RelIndexCalcItem
                {
                    Symbol = vs.Symbol,
                    RebalPrice = candle.Close,
                    PrevPrice = prevCandle.Close,
                    NowPrice = candle.Close,
                    CircSupply = capInfo.CirculatSupply.GetValueOrDefault(),
                    Volat = vs.Average
                };

                indexItems.Add(item);
            }

            indexItems = [.. indexItems.OrderByDescending(i => i.Capitalization).Take(100)];

            var avgVolat = indexItems.Average(i => i.Volat);

            indexItems.ForEach(i => i.СorrectedVolat = avgVolat / i.Volat);

            return indexItems;

        }

        private List<TimeSeriesElement> CalculateIndexValues(List<RelIndexCalcItem> indexItems, DateTime lastReblTime, DateTime? nextRebTime, double lastIndex, double RK)
        {
            var series = new List<TimeSeriesElement>();

            if (nextRebTime == null)
                nextRebTime = root.Candles[indexItems[0].Symbol].Last().OpenTime;

            var ts = nextRebTime.GetValueOrDefault() - lastReblTime;

            var t = 1 + (int)(ts.TotalMinutes / 15);
            double index = lastIndex;

            Candle[][] cndls = new Candle[indexItems.Count][];

            for (var i = 0; i < indexItems.Count; i++)
            {
                var temp = root.Candles[indexItems[i].Symbol].Where(c => c.OpenTime >= lastReblTime && c.OpenTime <= nextRebTime.GetValueOrDefault());
                cndls[i] = [.. temp];
            }

            for (var i = 1; i < cndls[0].Length; i++)
            {
                for (var j = 0; j < indexItems.Count; j++)
                {
                    var item = indexItems[j];
                    item.PrevPrice = item.NowPrice;
                    item.NowPrice = cndls[j][i].Close;
                }
                index = index * (1 + indexItems.Sum(i => i.RelativePriceChange) / 100) * RK;
                series.Add(new TimeSeriesElement { Time = cndls[0][i].OpenTime, Value = index });
            }

            return series;

        }

        private double CalculateIndexValue(List<RelIndexCalcItem> indexItems, double lastIndex, double RK)
        {
            double index = lastIndex * (1 + indexItems.Sum(i => i.RelativePriceChange) / 100) * RK;
            return index;
        }

        private List<DateTime> GetRebalanceTimes(DateTime start)
        {
            List<DateTime> times = [start];

            DateTime rebalTime = start;

            while (true)
            {
                rebalTime = rebalTime.AddMonths(1);
                if (rebalTime <= DateTime.UtcNow)
                {
                    times.Add(rebalTime);
                }
                else
                {
                    break;
                }
            }

            return times;
        }

        /// <summary>
        /// Возвращает параметры волатильности инструментов, рассчитанные для заданных даты и глубины истории
        /// </summary>
        /// <param name="to">конечное время</param>
        /// <param name="depth">глубина истории - число баров от времени to в глубину. По умолчанияю 180 дней</param>
        /// <returns></returns>
        public async Task<List<VolatilityParams>> GetVolatilityParams(DateTime? to = null, int depth = 17280)
        {
            var volParams = new List<VolatilityParams>();

            //var limit = 180 * 24 * 4; //1/2 года 17280

            DateTime time = to != null ? to.Value : DateTime.UtcNow;

            await Task.Factory.StartNew(() =>
            {
                foreach (var kvp in root.Candles)
                {
                    var temp = kvp.Value.Where(c => c.OpenTime >= time.Date.AddDays(-180) && c.OpenTime < time.Date).ToList();
                    if (temp.Count < depth) continue;
                    Candle[] candles = [.. temp.ToHourInterval()];
                    var volPrm = candles.GetVolatilityParams(kvp.Key);
                    volParams.Add(volPrm);
                }
            });

            return volParams;

        }

    }
}
