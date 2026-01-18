// Copyright(c) [2026], [Sergey Dvortsov]

using bybit.net.api.Models;
using bybit.net.api.Models.Market;
using NLog;
using Synapse.Crypto.Bybit;
using Synapse.Crypto.Trading;
using Synapse.General;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;
using System.Windows.Shapes;

namespace Synapse.Crypto.Patterns
{
    /// <summary>
    /// Information about the candles stored in the file storage a specific instrument.
    /// </summary>
    public class CandleStorageInfo
    {
        /// <summary>
        /// Symbol for loading candles.
        /// </summary>
        public required string Symbol { get; set; }
        /// <summary>
        /// Start time for loading candles for the symbol.
        /// </summary>
        public DateTime Start { get; set; }
        /// <summary>
        /// End time for loading candles for the symbol.
        /// </summary>
        public DateTime End { get; set; }

        /// <summary>
        /// Start time for storage of candles for the symbol.
        /// </summary>
        public DateTime StorageStart { get; set; }

        public bool Active { get; set; }

        public override string ToString()
        {
            return $"{Symbol};{Start:yyyy-MM-dd HH:mm:ss};{End:yyyy-MM-dd HH:mm:ss};{StorageStart:yyyy-MM-dd HH:mm:ss};{Active}";
        }

        public static CandleStorageInfo? Parse(string line)
        {
            var parts = line.Split(';');
            if (parts.Length != 5) return null;

            return new CandleStorageInfo
            {
                Symbol = parts[0],
                Start = DateTime.ParseExact(parts[1], "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                End = DateTime.ParseExact(parts[2], "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                StorageStart = DateTime.ParseExact(parts[3], "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                Active = bool.Parse(parts[4]) // Assuming the last part is Active status
            };

        }

        /// <summary>
        /// Downloads information from the candlestick download log. The log contains data about the symbol, the first date, and the last date of the data in the storage.
        /// </summary>
        /// <param name="filepath">file path</param>
        /// <returns></returns>
        public static List<CandleStorageInfo>? LoadFromStorage(string filepath)
        {
            if (!File.Exists(filepath)) return null;
            var lines = File.ReadAllLines(filepath);
            return [.. lines.Select(CandleStorageInfo.Parse).Where(info => info != null)];
        }

    }
   
    /// <summary>
    /// File storage management
    /// </summary>
    public class StorageManager
    {

        private readonly AppRoot root = AppRoot.GetInstance();
        private readonly Logger logger = LogManager.GetCurrentClassLogger();
        private FolderPaths folders;

        public StorageManager(List<BybitSecurity> securities, InstrTypes instrType, MarketInterval interval)
        {
            folders = root.Folders;
            Securities = securities;
            InstrType = instrType;
            Interval = interval;
            StorageInfo = CandleStorageInfo.LoadFromStorage(folders.LoadLog[InstrType][Interval]);
        }

        public StorageManager()
        {
            folders = root.Folders;
            Securities = root.Swaps;
            InstrType = root.DefaultInstrType;
            Interval = root.DefaultInterval;
            StorageInfo = CandleStorageInfo.LoadFromStorage(folders.LoadLog[InstrType][Interval]);
        }

        public List<CandleStorageInfo>? StorageInfo { get; private set; }

        public List<BybitSecurity> Securities { get; set; }

        public InstrTypes InstrType { get; set; }

        public MarketInterval Interval { get; set; }

        public bool DisplayMessages { set; get; } = true;

        /// <summary>
        /// Updating candle storage information
        /// </summary>
        public void Update()
        {
            if (StorageInfo != null)
            {
                foreach (var sec in Securities)
                {
                    var loadInfo = StorageInfo.FirstOrDefault(li => li.Symbol == sec.Symbol);

                    if (loadInfo == null) // Added new symbols
                    {
                        StorageInfo.Add(new CandleStorageInfo
                        {
                            Symbol = sec.Symbol,
                            Start = sec.StartTime,
                            End = sec.StartTime,
                            Active = true // Set default active status
                        });
                    }
                    else
                    {
                        loadInfo.Active = true; // Activate the symbol if it was previously inactive
                    }
                }

                // Deactivate symbols that are no longer available
                foreach (var li in StorageInfo)
                {
                    if (!Securities.Any(sec => sec.Symbol == li.Symbol))
                    {
                        li.Active = false;
                    }
                }
            }
            else
            {
                StorageInfo = [];
                foreach (var sec in Securities)
                {
                    StorageInfo.Add(new CandleStorageInfo
                    {
                        Symbol = sec.Symbol,
                        Start = sec.StartTime,
                        End = sec.StartTime,
                        Active = true // Set default active status
                    });
                }
            }
        }

        /// <summary>
        /// Downloads candlesticks from the exchange and updates the storage for a single instrument. Used for debugging.
        /// </summary>
        /// <param name="symbol">instrument</param>
        /// <param name="init">true if the method is called during initialization</param>
        /// <returns></returns>
        public async Task UpdateStorageDebug(string symbol, bool init)
        {

            try
            {

                if (StorageInfo == null) throw new NullReferenceException(nameof(StorageInfo));

                if (DisplayMessages)
                    root.OnNewStatusMessage("We are starting to download candles from the exchange and update the storage.");

                // We retrieve candles from the exchange since the last update and save them in storage.
                var li = StorageInfo.FirstOrDefault(c => c.Symbol == symbol);

                if (li == null) throw new NullReferenceException("StorageInfo item");

                if (!li.Active) throw new NullReferenceException("StorageInfo item is not active.");

                if (DisplayMessages)
                    root.OnNewStatusMessage($"Loading candles for {li.Symbol} from {li.End:yyyy-MM-dd HH:mm} to {DateTime.UtcNow:yyyy-MM-dd HH:mm}");

                var candles = await root.BbClient.LoadCandlesHistory(Category.LINEAR, li.Symbol, Interval, li.End.Add(TimeSpan.FromMinutes(int.Parse(Interval))));

                if (candles == null || candles.Length == 0)
                {
                    if (DisplayMessages)
                        root.OnNewStatusMessage($"No candles found for {li.Symbol}.");
                    return;
                }

                if (candles.Length > 1)
                {
                    var candlesForSave = candles.Take(candles.Length - 1);
                    File.AppendAllLines(System.IO.Path.Combine(folders.Candles[InstrType][Interval], $"{li.Symbol}.csv"), candlesForSave.Select(c => c.ToString()));
                    li.End = candlesForSave.Last().OpenTime; // Update end time to the last candle's open time
                }

                if (DisplayMessages)
                    root.OnNewStatusMessage($"Candles for {li.Symbol} are loaded.");

                //If the method is called after initialization, add new candles to Candles
                //if (!init)
                //{
                //    if (root.Candles.TryGetValue(li.Symbol, out List<Candle>? value))
                //    {
                //        value.RemoveAll(c => c.OpenTime >= candles.First().OpenTime);
                //        value.AddRange(candles);
                //    }
                //    else
                //        root.Candles.Add(li.Symbol, [.. candles]);
                //}

                File.WriteAllLines(folders.LoadLog[InstrType][Interval], StorageInfo.Select(li => li.ToString()));

            }
            catch (Exception ex)
            {
                logger.ToError(ex);
            }

        }

        /// <summary>
        /// Downloads candlesticks from the exchange and updates the storage.
        /// </summary>
        /// <param name="swaps"></param>
        /// <param name="instType"></param>
        /// <param name="interval"></param>
        /// <returns></returns>
        public async Task UpdateStorage(bool init)
        {
            try
            {
                if (StorageInfo == null) throw new NullReferenceException(nameof(StorageInfo));

                if (DisplayMessages)
                    root.OnNewStatusMessage("We are starting to download candles from the exchange and update the storage.");

                // We receive candles from the exchange's API since the last update and save the data in storage.
                foreach (var li in StorageInfo)
                {
                    if (li.Active)
                    {

                        if (DisplayMessages)
                            root.OnNewStatusMessage($"Loading candles for {li.Symbol} from {li.End:yyyy-MM-dd HH:mm} to {DateTime.UtcNow:yyyy-MM-dd HH:mm}");

                        var candles = await root.BbClient.LoadCandlesHistory(Category.LINEAR, li.Symbol, Interval, li.End.Add(TimeSpan.FromMinutes(int.Parse(Interval))));

                        if (candles == null || candles.Length == 0)
                        {
                            if (DisplayMessages)
                                root.OnNewStatusMessage($"No candles found for {li.Symbol}.");
                            continue;
                        }

                        if (candles.Length > 1)
                        {
                            // We only store finished candles in the storage, so we discard the last candle.
                            var candlesForSave = candles.Take(candles.Length - 1);

                            File.AppendAllLines(System.IO.Path.Combine(folders.Candles[InstrType][Interval], $"{li.Symbol}.csv"), candlesForSave.Select(c => c.ToString()));
                            li.End = candlesForSave.Last().OpenTime; // Update end time to the last candle's open time
                        }

                        if (DisplayMessages)
                            root.OnNewStatusMessage($"Candles for {li.Symbol} are loaded.");

                        // If the method is called after initialization, add new candles to Candles
                        //if (!init)
                        //{
                        //    if (root.Candles.TryGetValue(li.Symbol, out List<Candle>? value))
                        //    {
                        //        value.RemoveAll(c => c.OpenTime >= candles.First().OpenTime);
                        //        value.AddRange(candles);
                        //    }
                        //    else
                        //        root.Candles.Add(li.Symbol, [.. candles]);
                        //}

                    }
                    else
                            if (DisplayMessages)
                        root.OnNewStatusMessage($"Skipping {li.Symbol} as it is inactive.");

                }

                File.WriteAllLines(folders.LoadLog[InstrType][Interval], StorageInfo.Select(li => li.ToString()));
            }
            catch (Exception ex)
            {
                logger.ToError(ex);
            }

        }

        /// <summary>
        /// Downloads candlesticks from the storage for a single instrument. Used for debugging.
        /// </summary>
        /// <param name="symbol">instrument</param>
        /// <returns></returns>
        public async Task<Dictionary<string, List<Candle>>> GetCandlesFromStorageDebugAsync(string symbol)
        {
            try
            {
                if (StorageInfo == null) throw new NullReferenceException(nameof(StorageInfo));

                if (DisplayMessages)
                    root.OnNewStatusMessage("We are starting to download candles from the storage.");

                var item = StorageInfo.FirstOrDefault(l => l.Symbol == symbol);

                if (item == null || !item.Active) throw new Exception($"The active {symbol} not found.");

                var candles = new Dictionary<string, List<Candle>>();

                if (!File.Exists(System.IO.Path.Combine(folders.Candles[InstrType][Interval], $"{item.Symbol}.csv")))
                    throw new FileNotFoundException($"The {symbol} candles file not found.");

                List<Candle>? list = await Task.Run(() => GetCandles(InstrType, Interval, item.Symbol))
                    ?? throw new Exception($"Failed to load candles from {symbol} file.");

                candles.Add(item.Symbol, list);

                //item.StorageStart = list.First().OpenTime; // Update storage start time to the first candle's open time

                return candles;

            }
            catch (Exception ex)
            {
                logger?.ToError(ex);
            }

            return null;

        }

        /// <summary>
        /// Downloads candlesticks from the storage.
        /// </summary>
        /// <returns></returns>
        public async Task<Dictionary<string, List<Candle>>> GetCandlesFromStorageAsync()
        {
            try
            {
                if (StorageInfo == null) throw new NullReferenceException(nameof(StorageInfo));

                if (DisplayMessages)
                    root.OnNewStatusMessage("We are starting to download candles from the storage.");

                var candles = new Dictionary<string, List<Candle>>();

                List<Task<List<Candle>?>> tasks = [];

                foreach (var item in StorageInfo)
                {
                    if (!item.Active) continue;
                    if (!File.Exists(System.IO.Path.Combine(folders.Candles[InstrType][Interval], $"{item.Symbol}.csv"))) continue;
                    Task<List<Candle>?> task = Task.Run(() => GetCandles(InstrType, Interval, item.Symbol));
                    tasks.Add(task);
                }

                await Task.WhenAll(tasks);

                bool saveInfo = false;
                int i = 0;

                foreach (var item in StorageInfo)
                {
                    if (!item.Active) continue;
                    if (!File.Exists(System.IO.Path.Combine(folders.Candles[InstrType][Interval], $"{item.Symbol}.csv"))) continue;

                    var result = await tasks[i];

                    if (result != null)
                    {
                        if (!candles.TryAdd(item.Symbol, result))
                            candles[item.Symbol] = result;

                        if(item.StorageStart != result.First().OpenTime)
                        {
                            item.StorageStart = result.First().OpenTime; // Update storage start time to the first candle's open time
                            saveInfo = true;
                        }
                    }
                    i++;

                }

                if(saveInfo)
                    File.WriteAllLines(folders.LoadLog[InstrType][Interval], StorageInfo.Select(li => li.ToString()));

                return candles;

            }
            catch (Exception ex)
            {
                logger.ToError(ex);
            }

            return null;

        }

        private List<Candle>? GetCandles(InstrTypes instType, MarketInterval interval, string symbol)
        {
            var lines = File.ReadAllLines(System.IO.Path.Combine(folders.Candles[instType][interval], $"{symbol}.csv"));
            if (lines.Length == 0) return null;
            return [.. lines.Select(line => Candle.Parse(line))];
        }

    }
}
