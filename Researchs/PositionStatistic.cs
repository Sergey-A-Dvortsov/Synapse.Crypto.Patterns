using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Synapse.Crypto.Patterns
{
    /// <summary>
    /// Возвращает стастику тестирования стратегий или симуляции торговли на исторических данных
    /// </summary>
    public class PositionStatistic
    {


        public double Size { get; set; } = 10000;


        /// <summary>
        /// Число закрытых позиций
        /// </summary>
        public int PositionCount { get => LongPositionCount + ShortPositionCount; }

        /// <summary>
        /// Rоличество прибыльных сделок
        /// </summary`
        public double Win { get => LongWin + ShortWin; }

        /// <summary>
        /// Относительное количество прибыльных сделок (%)
        /// </summary`
        public double WinRate { get => Math.Round(100 * Win / PositionCount, 2); }

        /// <summary>
        /// Общие прибыль/убыток
        /// </summary>
        public double PNL { get => LongPNL + ShortPNL; }

        /// <summary>
        /// Годовая доходность
        /// </summary>
        public double APY { get => Math.Round(365 * 100 * (PNL / Duration.Days) / Size, 2); }

        /// <summary>
        /// Прибыль всех позиций
        /// </summary>
        public double Profit { get => LongProfit + ShortProfit; }

        /// <summary>
        /// Убыток всех позиций
        /// </summary>
        public double Loss { get => LongLoss + ShortLoss; }

        /// <summary>
        /// Соотношение чистой прибыли к чистому убытку
        /// </summary`
        public double ProfitLossRate { get => Math.Round(Profit / Loss, 2); }

        /// <summary>
        /// Число закрытых длинных позиций
        /// </summary>
        public int LongPositionCount { get; set; }

        /// <summary>
        /// Прибыль/убыток длинных позиций
        /// </summary>
        public double LongPNL { get; set; }

        /// <summary>
        /// Прибыль длинных позиций
        /// </summary>
        public double LongProfit { get; set; }

        /// <summary>
        /// Убыток длинных позиций
        /// </summary>
        public double LongLoss { get; set; }

        /// <summary>
        /// Количество прибыльных сделок в лонг
        /// </summary>
        public double LongWin { get; set; }

        /// <summary>
        /// Относительное количество прибыльных длинных сделок (%)
        /// </summary`
        public double LongWinRate { get => Math.Round(100 * LongWin / LongPositionCount, 2); }

        /// <summary>
        /// Число закрытых коротких позиций
        /// </summary>
        public int ShortPositionCount { get; set; }

        /// <summary>
        /// Прибыль/убыток коротких позиций
        /// </summary>
        public double ShortPNL { get; set; }

        /// <summary>
        /// Прибыль коротких позиций
        /// </summary>
        public double ShortProfit { get; set; }

        /// <summary>
        /// Убыток коротких позиций
        /// </summary>
        public double ShortLoss { get; set; }

        /// <summary>
        /// Количество прибыльных сделок в шорт (%)
        /// </summary>
        public double ShortWin { get; set; }

        /// <summary>
        /// Относительное количество прибыльных коротких сделок (%)
        /// </summary`
        public double ShortWinRate { get => Math.Round(100 * ShortWin / ShortPositionCount, 2); }

        public DateTime StartTime { get; set; }

        public DateTime EndTime { get; set; }

        public TimeSpan Duration { get => EndTime - StartTime; }

    }
}
