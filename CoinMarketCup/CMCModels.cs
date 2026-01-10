using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;
using NLog;

namespace Synapse.Crypto.Patterns
{
    public class CMCResponse
    {
        public CMCStatus Status { get; set; }
        public CMCCoinInfo[] Data { get; set; }
    }

    public class CMCStatus
    {
        //"timestamp": "2018-06-02T22:51:28.209Z",
        public DateTime timestamp { get; set; }
        //"error_code": 0,
        public int error_code { get; set; }
        //"error_message": "",
        public string error_message { get; set; }
        // "elapsed": 10,
        public int elapsed { get; set; }
        // "credit_count": 1,
        public int credit_count { get; set; }
    }

    public class CMCQuote
    {
        public CMCQuoteInfo USD { get; set; }
    }

    public class CMCQuoteInfo
    {
        // "price": 95128.598935962247,
        public double price { get; set; }
        // "volume_24h": 36190572136.932671,
        public double volume_24h { get; set; }
        // "volume_change_24h": 24.854,
        public double volume_change_24h { get; set; }
        // "percent_change_1h": 0.93493407,
        public double volume_change_1h { get; set; }
        // "percent_change_24h": -0.92606262,
        public double percent_change_24h { get; set; }
        // "percent_change_7d": -0.57640514,
        public double percent_change_7d { get; set; }
        // "percent_change_30d": -9.6528832,
        public double percent_change_30d { get; set; }
        // "percent_change_60d": -0.75320167,
        public double percent_change_60d { get; set; }
        // "percent_change_90d": 0.80247865,
        public double percent_change_90d { get; set; }
        // "market_cap": 1886032064350.8481,
        public double market_cap { get; set; }
        // "market_cap_dominance": 60.4279,
        public double market_cap_dominance { get; set; }
        //"fully_diluted_market_cap": 1997700577655.21,
        public double fully_diluted_market_cap { get; set; }
        // "tvl": null,
        public double? tvl { get; set; }
        // "last_updated": "2025-02-18T21:24:00Z"
        public DateTime last_updated { get; set; }
    }

    public class CMCCoinInfo
    {
        // "id": 1,
        public int id { get; set; }
        public string name { get; set; }
        public string symbol { get; set; }
        public string slug { get; set; }
        public int num_market_pairs { get; set; }
        public DateTime date_added { get; set; }
        public string[] tags { get; set; }
        public double? max_supply { get; set; }
        public double? circulating_supply { get; set; }
        public double? total_supply { get; set; }
        public bool infinite_supply { get; set; }
        public object platform { get; set; }
        public int cmc_rank { get; set; }
        public double? self_reported_circulating_supply { get; set; }
        public double? self_reported_market_cap { get; set; }
        public double? tvl_ratio { get; set; }
        public DateTime last_updated { get; set; }
        public CMCQuote quote { get; set; }


        public override string ToString()
        {
            return string.Format("{0};{1};{2};{3}", symbol, date_added, last_updated, cmc_rank);
        }
    }

    //public interface IStorage<T>
    //{
    //    static T Parse(string line);
    //}


    /// <summary>
    /// Содержит сокращенную информацию о рыночной капитализации монет
    /// </summary>
    public struct CoinCupInfo
    {
        public string Symbol { get; set; }
        /// <summary>
        /// Дата включения в базу
        /// </summary>
        public DateTime AddedTime { get; set; }
        /// <summary>
        /// Время последнего обновления данных
        /// </summary>
        public DateTime LastUpdatedTime { get; set; }
        /// <summary>
        /// Ранг капитализации по версии CoinMarketCap
        /// </summary>
        public int Rank { get; set; }
        /// <summary>
        /// Максимальное предложение монет
        /// </summary>
        public double? MaxSupply { get; set; }
        /// <summary>
        /// Циркулирующее предлложение монет
        /// </summary>
        public double? CirculatSupply { get; set; }
        /// <summary>
        /// Общее предложение монет
        /// </summary>
        public double? TotalSupply { get; set; }
        /// <summary>
        /// Последняя цена
        /// </summary>
        public double Price { get; set; }
        /// <summary>
        /// Рыночная капитализация
        /// </summary>
        public double MarketCap { get; set; }
        /// <summary>
        /// % доминирования в общей капитализации крипторынка
        /// </summary>
        public double MarketCapDominance { get; set; }

        public override string ToString()
        {
            return $"{Symbol};{AddedTime};{LastUpdatedTime};{Rank};{MaxSupply};{CirculatSupply};{TotalSupply};{Price};{MarketCap};{MarketCapDominance}";
        }

        public static CoinCupInfo? Parse(string line)
        {  
            var arr = line.Split(';');

            //"{Symbol};{AddedTime};{LastUpdatedTime};{Rank};
            //{MaxSupply};{CirculatSupply};{TotalSupply};{Price};{MarketCap};{MarketCapDominance}"

            try
            {

           

            return new CoinCupInfo()
            {
                Symbol = arr[0],
                AddedTime = DateTime.Parse(arr[1]),
                LastUpdatedTime = DateTime.Parse(arr[2]),
                Rank = string.IsNullOrWhiteSpace(arr[3]) ? 0 : int.Parse(arr[3]),
                MaxSupply = string.IsNullOrWhiteSpace(arr[4]) ? 0 : double.Parse(arr[4]),
                CirculatSupply = string.IsNullOrWhiteSpace(arr[5]) ? 0 : double.Parse(arr[5]),
                TotalSupply = string.IsNullOrWhiteSpace(arr[6]) ? 0 : double.Parse(arr[6]), 
                Price = string.IsNullOrWhiteSpace(arr[7]) ? 0 : double.Parse(arr[7]),
                MarketCap = string.IsNullOrWhiteSpace(arr[8]) ? 0 : double.Parse(arr[8]),
                MarketCapDominance = string.IsNullOrWhiteSpace(arr[9]) ? 0 : double.Parse(arr[9])
            };

            }
            catch (Exception ex)
            {
                var r = arr;
                var l = line;

                var msg = ex.Message;

            }

            return null ;

        }

        public static List<CoinCupInfo> LoadFromStorage(string filepath)
        {
            if (File.Exists(filepath))
            {
                var lines = File.ReadAllLines(filepath);
                return lines.Select(l => CoinCupInfo.Parse(l).GetValueOrDefault()).ToList();
            }

            return null;
        }

    }

}
