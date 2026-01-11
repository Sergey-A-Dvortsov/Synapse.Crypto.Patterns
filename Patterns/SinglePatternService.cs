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
    /// A module that combines functionality for working with single candlestick patterns.
    /// Recognition, statistics, etc.
    /// </summary>
    public class SinglePatternService
    {
        /// <summary>
        /// Статистические данные по свечным паттернам. Используются для распознавания одипочного свечного паттерна.
        /// </summary>
        //public Dictionary<string, Dictionary<CandlePatterns, CandlePatternStat>> CandlePatternStats { private set; get; }

        public static Dictionary<CandlePatterns, CandlePatternStat> GetCandlePatternStatistics(MasterTableItem item, List<Candle> candles)
        {
            var stats = new Dictionary<CandlePatterns, CandlePatternStat>();

            foreach (var pattern in Enum.GetValues(typeof(CandlePatterns)))
            {
                stats.Add((CandlePatterns)pattern, GetCandlePatternStatistic(item, (CandlePatterns)pattern, candles));
            }
            return stats;
        }

        private static CandlePatternStat GetCandlePatternStatistic(MasterTableItem item, CandlePatterns pattern, List<Candle> candles)
        {
            var times = item.CandleMarkups.Where(m => m.Pattern == pattern).Select(c => c.Time);
            var criteria = new HashSet<DateTime>(times);
            var selection = candles.Where(c => criteria.Contains(c.OpenTime));
            var bodyStat = new MathNet.Numerics.Statistics.DescriptiveStatistics(selection.Select(c => c.PerBody()));
            var shadowRatioStat = new MathNet.Numerics.Statistics.DescriptiveStatistics(selection.Select(c => c.PerShadowDiff()));
            var shadowBodyRatioStat = new MathNet.Numerics.Statistics.DescriptiveStatistics(selection.Select(c => c.ShadowBodyRatio()));
            return new CandlePatternStat(bodyStat, shadowRatioStat, shadowBodyRatioStat);
        }

        public static async Task<Dictionary<string, Dictionary<CandlePatterns, CandlePatternStat>>> SetCandlePatternStatistic(List<MasterTableItem> items, 
            Dictionary<string, List<Candle>> candles)
        {
            var stats = new Dictionary<string, Dictionary<CandlePatterns, CandlePatternStat>>();

            var itms = items.Where(s => s.CandleMarkups.Count != 0).ToArray();

            Task<Dictionary<CandlePatterns, CandlePatternStat>>[] tasks = new Task<Dictionary<CandlePatterns, CandlePatternStat>>[itms.Length];

            for (var i = 0; i < itms.Length; i++)
            {
                tasks[i] = Task.Run(() => GetCandlePatternStatistics(items[i], candles[itms[i].Symbol]));
            }

            Task.WaitAll(tasks);

            for (var i = 0; i < itms.Length; i++)
            {
                stats.Add(items[i].Symbol, await tasks[i]);
            }

            return stats;

        }


    }
}
