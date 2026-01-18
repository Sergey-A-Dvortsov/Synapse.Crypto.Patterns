using Synapse.Crypto.Bybit;
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
    // Advance-Decline Line
    // Текущее значение A/D = (Число растущих акций - Число падающих акций) + Значение A/D за предыдущий день
    // Растущей считается акция, цена закрытия которой выше, чем на предыдущей торговой сессии.
    // Падающей — акция, цена закрытия которой ниже
    // Осциллятор Макклеллана
    //Осциллятор = (19-дневная EMA от(A − D)) − (39-дневная EMA от(A − D))
    // McClellan Summation Index
    // Текущее значение Summation Index = Предыдущее значение + (Значение Осциллятора Макклеллана / Коэффициент сглаживания)
    // Стандартный коэффициент сглаживания(K) обычно равен 0.1 или 0.025. Это преобразует осциллятор, колеблющийся вокруг нуля, в долгосрочный трендовый индикатор.

//    Ключевые сигналы и интерпретация
//Главный способ использования — анализ направления и положения линии индекса относительно определенных уровней и ее дивергенций с ценой основного рыночного индекса.

//Сигнал / Паттерн Интерпретация
//Направление тренда  Восходящая линия(> +800) указывает на сильный и устойчивый бычий тренд с широким участием.Нисходящая линия (< -800) — признак глубокого и широкого медвежьего рынка.
//Уровни перекупленности/перепроданности Ключевые пороги: +1000 / -1000. Значения выше +1000 сигнализируют об экстремальной перекупленности, ниже -1000 — о глубокой перепроданности.Это зоны повышенного риска (для покупок) или возможности (для продаж).
//Пересечение нулевой линии Более значимый сигнал, чем у осциллятора.Пересечение снизу вверх часто подтверждает начало нового бычьего цикла.Пересечение сверху вниз — возможное начало медвежьего этапа.
//Медвежья дивергенция    Цена индекса растет, а Summation Index делает более низкие пики.Это классический признак истощения восходящего тренда и сужения его базы.
//Бычья дивергенция   Цена индекса падает, а Summation Index формирует более высокие минимумы.Указывает на скрытое улучшение ширины рынка и возможное дно.
//🔄 Сравнение с Осциллятором Макклеллана
//Параметр Осциллятор Макклеллана Summation Index
//Представление   Осциллятор, колеблется вокруг нуля.Кумулятивная линия тренда.
//Временной горизонт  Краткосрочный (несколько дней/недель). Показывает импульс и скорость изменений.Средне- и долгосрочный(недели/месяцы). Показывает устойчивую тенденцию.
//Основное назначение Выявление моментов перекупленности/перепроданности и краткосрочных дивергенций.Определение силы основного тренда и главных точек его разворота.
//Ключевые сигналы    Пересечение нуля, экстремумы (например, +/-70), краткосрочные дивергенции.Направление тренда, положение относительно уровней (+/-1000), долгосрочные дивергенции, пересечение нуля.
//💡 Практическое применение и примеры
//Определение фазы рынка: Линия в устойчивом восходящем тренде(выше нуля и растет) дает "зеленый свет" для долгосрочных инвестиций в акции.Нисходящий тренд — сигнал к осторожности.

//Классический пример разворота: На вершине пузыря доткомов в 2000 году и перед кризисом 2008 года Summation Index сформировал ярко выраженные медвежьи дивергенции с основными индексами, задолго до обвала.

//Подтверждение дна: Сильное и быстрое восстановление Summation Index из глубокой отрицательной зоны(ниже -1000) часто является надежным признаком формирования долгосрочного рыночного дна(как было в марте 2009 или марте 2020 года).

//Практический совет: Summation Index наиболее полезен для позиционных инвесторов и трейдеров, работающих на среднесрочных горизонтах.Для точного входа лучше сочетать его сигналы с данными осциллятора и другими индикаторами.

//Для расчета и построения индекса Summation Index используются специализированные платформы, такие как StockCharts.com или инструменты от самих создателей — McClellan Financial Publications.

//Если вас интересует, как рассчитывается конкретный коэффициент сглаживания (K) для перевода осциллятора в Summation Index или где можно увидеть его актуальные графики, обращайтесь.



    public class MarketBreadth
    {
        private AppRoot root; 

        public void MarketBreadthCalc(List<BybitSecurity> securities)
        {

        //    //OnNewStatusMessage("Начата калькуляция индикатора MarketBreadth.");

         var mbItems = new List<MarketBreadthItem>();

         MarketBreadthItem mbitem = null;

        try
         {

           //var BTCdi = new DirectoryInfo(Path.Combine(RawCandleFldr, "BTCUSDT"));
           //var files = BTCdi.GetFiles().ToArray();
           //var times = BTCdi.GetFiles().Select(f => bnadapt.Helpers.GetDateFromFile(f.Name)).ToList();
           //var dict = new Dictionary<string, List<Candle>>();
           //int minCnt = 60 * 24; // 1440 

           //bool isfirstday = true;

           //        foreach (var date in times)
           //        {

           //             foreach (var sec in securities)
           //             {
           //                 var file = Path.Combine(RawCandleFldr, sec.Symbol, bnadapt.Helpers.GetFileNameFromDate(date));

           //                 if (File.Exists(file))
           //                 {
           //                     var candles = bnadapt.Helpers.GetCandlesFromFile(file, date);
           //                     if (dict.ContainsKey(sec.Symbol))
           //                     {
           //                         dict[sec.Symbol].AddRange(candles);
           //                     }
           //                     else
           //                         dict.Add(sec.Symbol, candles);
           //                 }
           //             }

           //             if (isfirstday)
           //             {
           //                 isfirstday = false;
           //                 continue;
           //             }

           //             int idx = minCnt;
           //             int endCnt = 0;

           //             if (dict["BTCUSDT"].Count <= minCnt * 2)
           //                 endCnt = dict["BTCUSDT"].Count;
           //             else
           //                 endCnt = minCnt * 2;

           //         for (var i = minCnt; i < endCnt; i++)
           //         {

           //             try
           //             {
           //                 mbitem = new MarketBreadthItem() { Time = dict["BTCUSDT"][i].OpenTime };
           //             }
           //             catch (Exception ex)
           //             {
           //                 var r = 9;
           //             }

           //             foreach (var kvp in dict)
           //             {
           //                 if (kvp.Value.Count < minCnt * 2) continue;
           //                 //var diff = 100 * (kvp.Value[idx].ClosePrice / kvp.Value[idx - minCnt].ClosePrice - 1);
           //                 //var diff = kvp.Value[i].ClosePrice - kvp.Value[i - minCnt].ClosePrice;

           //                 //mbitem.BullCnt += diff > 0 ? 1 : 0;
           //                 //mbitem.BearCnt += diff < 0 ? 1 : 0;
           //             }

           //             //            mbItems.Add(mbitem);

           //             //        }

           //             //        foreach (var kvp in dict)
           //             //        {
           //             //            if (kvp.Value.Count < minCnt * 2) continue;
           //             //            //var diff = 100 * (kvp.Value[idx].ClosePrice / kvp.Value[idx - minCnt].ClosePrice - 1);
           //             //            kvp.Value.RemoveAll(c => c.OpenTime.Date < date.Date);
           //             //        }

           //             //        var kk = 0;


           //         }

           //       // var savefile = Path.Combine(MarketBreadthFldr, "MarketBreadth.csv");

           //        //mbItems.SaveToFile(savefile);

           //        root.OnNewStatusMessage("Калькуляция индикатора MarketBreadth закончена.");

          }
            catch (Exception ex)
        {
        //    //    var itms = mbItems;
        //    //    var itm = mbitem;
        //    //    logger.ToError(ex);
        }



        }

        private void ConvertCandles(int interval)
        {
            //try
            //{


            //    var lines = File.ReadAllLines(Path.Combine(MarketBreadthFldr, "MarketBreadth.csv"));
            //    var items = lines.Select(l => new MarketBreadthItem()
            //    {
            //        Time = DateTime.Parse(l.Split(";")[0]),
            //        BullCnt = int.Parse(l.Split(";")[1]),
            //        BearCnt = int.Parse(l.Split(";")[2])
            //    }).ToList();

            //    var candles = new List<Candle>();

            //    for (var i = 0; i < items.Count(); i++)
            //    {
            //        var item = items[i];
            //        var cndlTime = item.Time.ToCandleTime(interval);
            //        var candle = candles.FirstOrDefault(c => c.OpenTime == cndlTime);

            //        var value = Math.Round(100 * (double)item.BullCnt / (double)(item.BullCnt + item.BearCnt), 2);

            //        if (candle == null)
            //        {
            //            candle = new Candle()
            //            {
            //                OpenTime = cndlTime,
            //                Open = value,
            //                High = value,
            //                Low = value,
            //                Close = value
            //            };
            //            candles.Add(candle);
            //        }
            //        else
            //        {
            //            candle.Close = value;
            //            candle.Low = Math.Min(value, candle.Low);
            //            candle.High = Math.Max(value, candle.High);
            //        }

            //    }

            //    var fileName = Path.Combine(MarketBreadthFldr, string.Format("MarketBreadth-{0}.csv", interval));

            //    candles.SaveToFile(fileName);

            //}
            //catch (Exception ex)
            //{
            //    logger.ToError(ex);
            //}




        }

        
    }
}
