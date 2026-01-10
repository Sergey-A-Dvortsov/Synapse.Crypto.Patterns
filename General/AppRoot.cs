using bybit.net.api.Models;
using bybit.net.api.Models.Market;
using MathNet.Numerics.IntegralTransforms;
using NLog;
using Synapse.Crypto.Bybit;
using Synapse.Crypto.Patterns.CoinMarketCup;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using Path = System.IO.Path;
using TimeFrames = Synapse.Crypto.Bybit.TimeFrames;
using Synapse.General;
using Synapse.Crypto.Trading;

namespace Synapse.Crypto.Patterns
{
    // Copyright(c) [2026], [Sergey Dvortsov]
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

        //

        /// <summary>
        /// Downloads information from the candlestick download log. The log contains data about the symbol, the first date, and the last date of the data in the storage.
        /// </summary>
        /// <param name="filepath">file path</param>
        /// <returns></returns>
        public static List<CandleStorageInfo>? LoadFromStorage(string filepath)
        {
            if(!File.Exists(filepath)) return null;
            var lines = File.ReadAllLines(filepath);
            return [.. lines.Select(CandleStorageInfo.Parse).Where(info => info != null)];
        }

    }

    public class IndexCalcItem
    {
        public required string Symbol { set; get; }
        public DateTime FirstTime { set; get; }
        public DateTime LastTime { set; get; }
        public int StartIndex { set; get; }
        public double FirstPrice { set; get; }
        public DateTime NowTime { set; get; }
        public double NowPrice { set; get; }
        public double Prev24hPrice { set; get; }
        public bool Active { set; get; }
        public int Count { set; get; }

        public double FullChange
        {
            get => 100 * (NowPrice / FirstPrice - 1);
        }

        public double Change24h
        {
            get => 100 * (NowPrice / Prev24hPrice - 1);
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append(Symbol + ";");
            sb.Append(FirstTime + ";");
            sb.Append(LastTime + ";");
            sb.Append(StartIndex + ";");
            sb.Append(FirstPrice);
            return sb.ToString();
        }


    }

    /// <summary>
    /// Folder paths
    /// </summary>
    public class FolderPaths
    {
        public FolderPaths()
        {
            Storage = @"D:\Storage\Bybit";
            InstrType = new Dictionary<InstrTypes, string>
            {
                {InstrTypes.SPOT, Path.Combine(Storage, InstrTypes.SPOT.ToString()) },
                {InstrTypes.SWAP, Path.Combine(Storage, InstrTypes.SWAP.ToString()) },
                {InstrTypes.FUTURE, Path.Combine(Storage, InstrTypes.FUTURE.ToString()) },
                {InstrTypes.PATTERN, Path.Combine(Storage, InstrTypes.PATTERN.ToString()) },
            };

            var interval = MarketInterval.FifteenMinutes;

            LoadLog = new Dictionary<InstrTypes, Dictionary<MarketInterval, string>>
            {
                { InstrTypes.SPOT, new Dictionary<MarketInterval, string>() }
            };
            LoadLog[InstrTypes.SPOT].Add(interval, Path.Combine(InstrType[InstrTypes.SPOT], "Candles", $"loadlog{interval}.txt"));

            LoadLog.Add(InstrTypes.SWAP, []);
            LoadLog[InstrTypes.SWAP].Add(interval, Path.Combine(InstrType[InstrTypes.SWAP], "Candles", $"loadlog{interval}.txt"));
            LoadLog.Add(InstrTypes.FUTURE, []);
            LoadLog[InstrTypes.FUTURE].Add(interval, Path.Combine(InstrType[InstrTypes.FUTURE], "Candles", $"loadlog{interval}.txt"));

            Candles = [];
            var type = InstrTypes.SPOT;

            Candles.Add(type, []);
            Candles[type].Add(interval, Path.Combine(InstrType[type], "Candles", interval + "min"));
            type = InstrTypes.SWAP;
            Candles.Add(type, []);
            Candles[type].Add(interval, Path.Combine(InstrType[type], "Candles", interval + "min"));
            type = InstrTypes.FUTURE;
            Candles.Add(type, []);
            Candles[type].Add(interval, Path.Combine(InstrType[type], "Candles", interval + "min"));

            Patterns = [];
            type = InstrTypes.PATTERN;
            Patterns.Add("Single", Path.Combine(InstrType[type], "Single"));

            CoinMarketCap = Path.Combine(Storage, "CoinMarketCap");

        }

        public string App { private set; get; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Scanner");
        public string Storage { get; }
        public Dictionary<InstrTypes, string> InstrType { get; }
        public Dictionary<InstrTypes, Dictionary<MarketInterval, string>> Candles { get; }
        public Dictionary<InstrTypes, Dictionary<MarketInterval, string>> LoadLog { get; }
        public Dictionary<string, string> Patterns { get; }
        public string CoinMarketCap { get; }

        public void CreateFolders()
        {
            foreach (var key in InstrType.Keys)
            {
                if (!Directory.Exists(InstrType[key]))
                    Directory.CreateDirectory(InstrType[key]);

                if (key == InstrTypes.PATTERN)
                {
                    if (!Directory.Exists(Path.Combine(InstrType[key], "Single")))
                        Directory.CreateDirectory(Path.Combine(InstrType[key], "Single"));
                }
                else
                {
                    if (!Directory.Exists(Path.Combine(InstrType[key], "Candles")))
                        Directory.CreateDirectory(Path.Combine(InstrType[key], "Candles"));

                    if (!Directory.Exists(Path.Combine(InstrType[key], "Candles", "15min")))
                        Directory.CreateDirectory(Path.Combine(InstrType[key], "Candles", "15min"));
                }
            }

            if (!Directory.Exists(CoinMarketCap))
                Directory.CreateDirectory(CoinMarketCap);

        }

    }

    /// <summary>
    /// Root class of application
    /// </summary>
    public class AppRoot
    {
        private static AppRoot? root;

        private readonly Timer? timer;

        private readonly List<string> excludes = [ "USDC", "USDE", "USD1", "ETHBTC" ];

        private readonly List<string> subscriptions = []; // streaming exchanges data subscriptions

        //private List<CandleStorageInfo>? StorageInfo { get; set; }

        private StorageManager storageManager; // to work with file storage

        private AppRoot()
        {
            logger = LogManager.GetCurrentClassLogger();
            Folders.CreateFolders();
            //timer = new Timer(new TimerCallback(OnTimerTick), null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
        }

        /// <summary>
        /// Return AppRoot instance in singleton mode
        /// </summary>
        /// <returns></returns>
        public static AppRoot GetInstance()
        {
            if (root == null)
            {
                lock (typeof(AppRoot))
                {
                    root = new AppRoot();
                }
            }
            return root;
        }

        public Logger logger; // logging

        #region events

        /// <summary>
        /// Message event that is displayed in the status bar
        /// </summary>
        public event Action<string> NewStatusMessage = delegate { };

        public void OnNewStatusMessage(string message)
        {
            NewStatusMessage?.Invoke(message);
        }

        /// <summary>
        /// The "master" table update event
        /// </summary>
        public event Action MasterTableUpdate = delegate { };

        private void OnMasterTableUpdate()
        {
            MasterTableUpdate?.Invoke();
        }

        #endregion

        #region properties

        /// <summary>
        /// Paths to folders used by the application
        /// </summary>
        public FolderPaths Folders { private set; get; } = new FolderPaths();

        /// <summary>
        /// Client API Bybit
        /// </summary>
        public BybitClient BbClient { private set; get; } = new BybitClient();

        /// <summary>
        /// Bybit perpetual futures List
        /// </summary>
        public List<BybitSecurity> Swaps { private set; get; }

        /// <summary>
        /// Data source of the "main" table
        /// </summary>
        public List<MasterTableItem> MasterItems { private set; get; } = [];

        //public List<TimeSeriesItem> Indexes { set; get; }

        /// <summary>
        /// Candles
        /// </summary>
        public Dictionary<string, List<Candle>> Candles { private set; get; } = [];

        public Dictionary<string, List<Candle>> OnlineCandles { private set; get; } = [];

        /// <summary>
        /// Статистические данные по свечным паттернам. Используются для распознавания одипочного свечного паттерна.
        /// </summary>
        public Dictionary<string, Dictionary<CandlePatterns, CandlePatternStat>> CandlePatternStats { private set; get; }

        public CMCClient CMCClient { private set; get; }

        public List<VolatilityParams> VolatilityParams { private set; get; }

        private int _loadedBars = 200;
        /// <summary>
        /// The default number of bars loaded into the chart
        /// </summary>
        public int LoadedBars
        {
            get { return _loadedBars; }
            set
            {
                _loadedBars = value;
            }
        }

        private Bybit.TimeFrames _timeFrame =  Bybit.TimeFrames.Hour;
        /// <summary>
        /// Default interval
        /// </summary>
        public Bybit.TimeFrames TimeFrame
        {
            get { return _timeFrame; }
            set
            {
                _timeFrame = value;
            }
        }
        
        private BreakStyles _breakDownStyle = BreakStyles.body;
        /// <summary>
        /// Level breakout style. Used to identify retests
        /// </summary>
        public BreakStyles BreakDownStyle
        {
            get => _breakDownStyle;
            set => _breakDownStyle = value;
        }

        /// <summary>
        /// Default instrument type
        /// </summary>
        public InstrTypes DefaultInstrType { get; private set; } = InstrTypes.SWAP;

        /// <summary>
        /// Default interval
        /// </summary>
        public MarketInterval DefaultInterval { get; private set; } = MarketInterval.FifteenMinutes;

        #endregion

        /// <summary>
        /// Initial actions when launching an application
        /// </summary>
        public async void InitAsync() 
        {
            var start = DateTime.Now;

            CMCClient = CMCClient.GetInstance();

            var securities = await BbClient.LoadSecurity([Category.LINEAR]);

            Swaps = [.. securities.Where(s => s.ContractType == ContractType.LinearPerpetual && s.QuoteCoin == "USDT" 
            && ! excludes.Any(e => e == s.BaseCoin))];

            storageManager = new ();

            storageManager.Update();

            bool debug = false; 

            if (debug)
            {
                await DebugInit();
                //var research = new FalseBreakoutResearch(Candles["BTCUSDT"]) { RangeFrame = TimeSpan.FromDays(1) };
                //research.Research();
                //var ts = DateTime.Now - start;
            }
            else
            {
                await ProductInit(start);
            }

            logger.Info("The application is initialized.");

        }

        /// <summary>
        /// Actions when the application is terminated
        /// </summary>
        public async Task ShutdownAsync()
        {
            if(subscriptions.Count > 0)
            {
                foreach (var subsc in subscriptions)
                {
                    await BbClient.Unsubscribe(subsc);
                }
            }

            BbClient.CandleUpdate -= OnCandleUpdate;
        }

        private async Task DebugInit()
        {
                BbClient.CandleUpdate += OnCandleUpdate;

                await storageManager.UpdateStorageDebug("BTCUSDT", true);

                Candles = await storageManager.GetCandlesFromStorageDebugAsync("BTCUSDT");

                OnNewStatusMessage($"Загрузка свечей и обновление хранилища закончены.");

                var symbols = Candles.Keys.ToArray();

                subscriptions.Add(await BbClient.SubscribeCandles(symbols, TimeFrames.Min15));

                await CMCClient.LoadCoinCapInfo(CMCClient.GetCoinCaps());

                UpdateScanTable(true);
        }
        
        private async Task ProductInit(DateTime start)
        {
                BbClient.CandleUpdate += OnCandleUpdate;

                await storageManager.UpdateStorage(true);

                Candles = await storageManager.GetCandlesFromStorageAsync();

                OnNewStatusMessage($"Candle loading and storage update complete. {DateTime.Now - start}");

                // await CandelGapsMonitoring();

                var symbols = Candles.Keys.ToArray();

                subscriptions.Add(await BbClient.SubscribeCandles(symbols, TimeFrames.Min15));

                // CandelStorageMonitoring();

                await CMCClient.LoadCoinCapInfo(CMCClient.GetCoinCaps());

                //VolatilityParams = await GetVolatilityParams();

                UpdateScanTable(true);

        }

        /// <summary>
        /// New (online) candle event handler
        /// </summary>
        /// <param name="symbol">instrument</param>
        /// <param name="frame">interval</param>
        /// <param name="candles">candles</param>
        private void OnCandleUpdate(string symbol, TimeFrames frame, List<Candle> candles)
        {
            if (Candles[symbol].Last().OpenTime == candles.Last().OpenTime)
            {
                int indx = Candles[symbol].Count - 1;
                Candles[symbol][indx] = candles.Last();
            }
            else if (Candles[symbol].Last().OpenTime < candles.Last().OpenTime)
            {
                Candles[symbol].Add(candles.Last());
            }

            //var temp = Candles[symbol].TakeLast(20).ToList(); 
        }

        public void UpdateScanTable(bool init)
        {
            OnNewStatusMessage("Обновление данных в ScreenItems.");

            bool prevDayExtremums = true;
            bool prevDayRetest = true;
            bool prevDayFalseBreake = true;

            FileInfo[]? markupfiles = init ? new DirectoryInfo(Folders.Patterns["Single"]).GetFiles(): null;

            foreach (var info in storageManager.StorageInfo)
            {
                if (!Candles.ContainsKey(info.Symbol)) continue;

                // если нет данных за предыдущий день
                if (Candles[info.Symbol].First().OpenTime.Date >= DateTime.UtcNow.Date) continue;

                var item = MasterItems.FirstOrDefault(i => i.Symbol == info.Symbol);

                if (item == null)
                {
                    var sec = Swaps.FirstOrDefault(s => s.Symbol == info.Symbol) ?? throw new NullReferenceException(info.Symbol);
                    var ci = CMCClient.CoinCaps.FirstOrDefault(s => s.Symbol.Equals(sec?.BaseCoin, StringComparison.CurrentCultureIgnoreCase));
                    int rank = 99999;
                    if (ci.AddedTime != DateTime.MinValue)
                        rank = ci.Rank;
                    item = new MasterTableItem(sec, info) { Rank = rank };
                    MasterItems.Add(item);
                }


                if (prevDayExtremums)
                {
                    item.PrevDayHigh = Candles[info.Symbol].MaxPrice();
                    item.PrevDayLow = Candles[info.Symbol].MinPrice();
                }

                if (prevDayRetest)
                {
                    var highBreakTime = Candles[info.Symbol].HighBreakDown(item.PrevDayHigh, TimeFrame, BreakDownStyle);
                    item.IsHighBreakdown = highBreakTime != null;
                    item.HighBreakTime = highBreakTime;

                    if (item.IsHighBreakdown)
                    {
                        var highRetestTime = Candles[info.Symbol].HighRetest(item.PrevDayHigh, TimeFrame, highBreakTime.GetValueOrDefault());
                        item.IsHighRetest = highRetestTime != null;
                        item.HighRetestTime = highRetestTime;
                    }

                    var lowBreakTime = Candles[info.Symbol].LowBreakDown(item.PrevDayLow, TimeFrame, BreakDownStyle);
                    item.IsLowBreakdown = lowBreakTime != null;
                    item.LowBreakTime = lowBreakTime;

                    if (item.IsLowBreakdown)
                    {
                        var lowRetestTime = Candles[info.Symbol].LowRetest(item.PrevDayLow, TimeFrame, lowBreakTime.GetValueOrDefault());
                        item.IsLowRetest = lowRetestTime != null;
                        item.LowRetestTime = lowRetestTime;
                    }
                }

                if (prevDayFalseBreake)
                {

                }



                // Если метод выполняется при инициализации, то загружаем данные маркировки из хранилища.
                if (markupfiles != null)
                {
                    //var file = markupfiles.FirstOrDefault(f => f.Name.Replace(".csv", "") == item.Symbol);
                    //if (file == null) continue;
                    //var lines = file.ReadAllLines(); 
                    //item.CandleMarkups.AddRange([.. lines.Select(l => CandleMarkup.Parse(l))]);
                }

            }

            OnMasterTableUpdate();
        }

        public void SaveMarkupForScreenItem(MasterTableItem item)
        {
            var path = Path.Combine(Folders.Patterns["Single"], $"{item.Symbol}.csv");
            if(item.CandleMarkups.Count != 0)
                item.CandleMarkups.SaveToFile(path);
        }

        public async Task<Dictionary<string, Dictionary<CandlePatterns, CandlePatternStat>>> SetCandlePatternStatistic()
        {
            var stats = new Dictionary<string, Dictionary<CandlePatterns, CandlePatternStat>>();

            var items = MasterItems.Where(s => s.CandleMarkups.Count != 0).ToArray();
            Task<Dictionary<CandlePatterns, CandlePatternStat>>[] tasks = new Task<Dictionary<CandlePatterns, CandlePatternStat>>[items.Length];

            for(var i = 0; i < items.Length; i++)
            {
                tasks[i] = Task.Run(() => GetCandlePatternStatistics(items[i], Candles[items[i].Symbol]));
            }

            Task.WaitAll(tasks);

            for (var i = 0; i < items.Length; i++)
            {
                stats.Add(items[i].Symbol, await tasks[i]);    
            }

            return stats;

        }

        private static Dictionary<CandlePatterns, CandlePatternStat> GetCandlePatternStatistics(MasterTableItem item, List<Candle> candles)
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

        ///// <summary>
        ///// Updating candle storage information
        ///// </summary>
        //private void UpdateStorageInfo()
        //{
        //    OnNewStatusMessage("Обновляем загрузочную информации.");

        //    //Загружаем информацию из лога загрузки (свечей). Лог содержит данные о символе, первой и последней дате данных в хранилище. 
        //    StorageInfo = CandleStorageInfo.LoadFromStorage(Folders.LoadLog[DefaultInstrType][DefaultInterval]);

        //    //if (StorageInfo == null) throw new NullReferenceException(nameof(StorageInfo));

        //    if (StorageInfo != null)
        //    {
        //        foreach (var sec in Swaps)
        //        {
        //            var loadInfo = StorageInfo.FirstOrDefault(li => li.Symbol == sec.Symbol);

        //            if (loadInfo == null) // Added new symbols
        //            {
        //                StorageInfo.Add(new CandleStorageInfo
        //                {
        //                    Symbol = sec.Symbol,
        //                    Start = sec.StartTime,
        //                    End = sec.StartTime,
        //                    Active = true // Set default active status
        //                });
        //            }
        //            else
        //            {
        //                loadInfo.Active = true; // Activate the symbol if it was previously inactive
        //            }
        //        }

        //        // Deactivate symbols that are no longer available
        //        foreach (var li in StorageInfo)
        //        {
        //            if (!Swaps.Any(sec => sec.Symbol == li.Symbol))
        //            {
        //                li.Active = false; 
        //            }
        //        }
        //    }
        //    else
        //    {
        //        StorageInfo = [];
        //        foreach (var sec in Swaps)
        //        {
        //            StorageInfo.Add(new CandleStorageInfo
        //            {
        //                Symbol = sec.Symbol,
        //                Start = sec.StartTime,
        //                End = sec.StartTime,
        //                Active = true // Set default active status
        //            });
        //        }

        //    }

        //}

        ///// <summary>
        ///// Загружает свечи из биржи и обновляет хранилище.
        ///// </summary>
        ///// <param name="swaps"></param>
        ///// <param name="instType"></param>
        ///// <param name="interval"></param>
        ///// <returns></returns>
        //public async Task LoadCandlesFromBurseAndUpdateStorage(bool init)
        //{
        //    OnNewStatusMessage("Начинаем загрузку свечей c биржи и обновление хранилища.");

        //    // Получаем свечи с биржи с момента последнего обновления и сохраняем данные в хранилище
        //    foreach (var li in StorageInfo)
        //    {
        //        if (li.Active)
        //        {
        //            //if (li.End > DateTime.UtcNow.Add(-TimeSpan.FromMinutes(int.Parse(currentInterval)))) continue;

        //            OnNewStatusMessage($"Loading candles for {li.Symbol} from {li.End:yyyy-MM-dd HH:mm} to {DateTime.UtcNow:yyyy-MM-dd HH:mm}");
        //            var candles = await BbClient.LoadCandlesHistory(Category.LINEAR, li.Symbol, DefaultInterval, li.End.Add(TimeSpan.FromMinutes(int.Parse(DefaultInterval))));

        //            //var lines = File.ReadAllLines(Path.Combine(Folders.Candles[InstrTypes.SWAP][MarketInterval.FifteenMinutes], $"{li.Symbol}.csv"));
        //            //if (lines.Length == 0) continue;
        //            //var lastcandle = Bybit.Candle.GetCandle(lines.Last());
        //            //var candles = await BbClient.LoadCandlesHistory(Category.LINEAR, li.Symbol, currentInterval, lastcandle.OpenTime.Add(TimeSpan.FromMinutes(int.Parse(currentInterval))));

        //            if (candles == null || candles.Length == 0)
        //            {
        //                OnNewStatusMessage($"No candles found for {li.Symbol}.");
        //                continue;
        //            }

        //            if (candles.Length > 1)
        //            {
        //                var candlesForSave = candles.Take(candles.Length - 1);
        //                File.AppendAllLines(Path.Combine(Folders.Candles[DefaultInstrType][DefaultInterval], $"{li.Symbol}.csv"), candlesForSave.Select(c => c.ToString()));
        //                li.End = candlesForSave.Last().OpenTime; // Update end time to the last candle's open time
        //            }

        //            OnNewStatusMessage($"Candles for {li.Symbol} are loaded.");

        //            // Если метод вызван после инициализации добавляем новые свечи в Candles
        //            if (!init)
        //            {
        //                if(Candles.TryGetValue(li.Symbol, out List<Candle>? value))
        //                {
        //                    value.RemoveAll(c => c.OpenTime >= candles.First().OpenTime);
        //                    value.AddRange(candles);
        //                }
        //                else 
        //                    Candles.Add(li.Symbol, [.. candles]);
        //            }

        //        }
        //        else
        //            OnNewStatusMessage($"Skipping {li.Symbol} as it is inactive.");

        //    }

        //    File.WriteAllLines(Folders.LoadLog[DefaultInstrType][DefaultInterval], StorageInfo.Select(li => li.ToString()));

        //}

        ///// <summary>
        ///// Загружает свечи из биржи и обновляет хранилище для одного инструмента. Используется для отладки
        ///// </summary>
        ///// <param name="swaps"></param>
        ///// <param name="instType"></param>
        ///// <param name="interval"></param>
        ///// <returns></returns>
        //public async Task LoadCandlesFromBurseAndUpdateStorageDebugMode(string symbol, bool init)
        //{
        //    OnNewStatusMessage("Начинаем загрузку свечей c биржи и обновление хранилища.");

        //    // Получаем свечи с биржи с момента последнего обновления и сохраняем данные в хранилище

        //    var li = StorageInfo.FirstOrDefault(c => c.Symbol == symbol);

        //        if (li.Active)
        //        {

        //            OnNewStatusMessage($"Loading candles for {li.Symbol} from {li.End:yyyy-MM-dd HH:mm} to {DateTime.UtcNow:yyyy-MM-dd HH:mm}");
        //            var candles = await BbClient.LoadCandlesHistory(Category.LINEAR, li.Symbol, DefaultInterval, li.End.Add(TimeSpan.FromMinutes(int.Parse(DefaultInterval))));

        //            //var lines = File.ReadAllLines(Path.Combine(Folders.Candles[InstrTypes.SWAP][MarketInterval.FifteenMinutes], $"{li.Symbol}.csv"));
        //            //if (lines.Length == 0) continue;
        //            //var lastcandle = Bybit.Candle.GetCandle(lines.Last());
        //            //var candles = await BbClient.LoadCandlesHistory(Category.LINEAR, li.Symbol, currentInterval, lastcandle.OpenTime.Add(TimeSpan.FromMinutes(int.Parse(currentInterval))));

        //            if (candles == null || candles.Length == 0)
        //            {
        //                OnNewStatusMessage($"No candles found for {li.Symbol}.");
        //                return;
        //            }

        //            if (candles.Length > 1)
        //            {
        //                var candlesForSave = candles.Take(candles.Length - 1);
        //                File.AppendAllLines(Path.Combine(Folders.Candles[DefaultInstrType][DefaultInterval], $"{li.Symbol}.csv"), candlesForSave.Select(c => c.ToString()));
        //                li.End = candlesForSave.Last().OpenTime; // Update end time to the last candle's open time
        //            }

        //            OnNewStatusMessage($"Candles for {li.Symbol} are loaded.");

        //            // Если метод вызван после инициализации добавляем новые свечи в Candles
        //            if (!init)
        //            {
        //                if (Candles.TryGetValue(li.Symbol, out List<Candle>? value))
        //                {
        //                    value.RemoveAll(c => c.OpenTime >= candles.First().OpenTime);
        //                    value.AddRange(candles);
        //                }
        //                else
        //                    Candles.Add(li.Symbol, [.. candles]);
        //            }

        //        }
        //        else
        //            OnNewStatusMessage($"Skipping {li.Symbol} as it is inactive.");


        //    File.WriteAllLines(Folders.LoadLog[DefaultInstrType][DefaultInterval], StorageInfo.Select(li => li.ToString()));

        //}

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
                foreach (var kvp in Candles)
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

        //private async Task<Dictionary<string, List<Candle>>> LoadCandlesFromStorage(InstrTypes instType, MarketInterval interval)
        //{
        //    OnNewStatusMessage("Начинаем загрузку свечей из хранилища.");

        //    var candles = new Dictionary<string, List<Candle>>();

        //    List<Task<List<Candle>?>> tasks = [];

        //    foreach (var item in StorageInfo)
        //    {
        //        if (!item.Active) continue;
        //        if (!File.Exists(Path.Combine(Folders.Candles[instType][interval], $"{item.Symbol}.csv"))) continue;
        //        Task<List<Candle>?> task = Task.Run(() => GetCandles(instType, interval, item.Symbol));   
        //        tasks.Add(task); 
        //    }

        //    await Task.WhenAll(tasks);

        //    int i = 0;

        //    foreach (var item in StorageInfo)
        //    {

        //        if (!item.Active) continue;
        //        if (!File.Exists(Path.Combine(Folders.Candles[instType][interval], $"{item.Symbol}.csv"))) continue;
        //        var result = await tasks[i];

        //        if (result != null)
        //        { 
        //            if (!candles.TryAdd(item.Symbol, result))
        //                candles[item.Symbol] = result;
        //            item.StorageStart = result.First().OpenTime; // Update storage start time to the first candle's open time
        //        }

        //        i++;

        //    }

        //    return candles;

        //}

        //// Получает свечи для одного инстумента. Используется для отладочных целей
        //private async Task<Dictionary<string, List<Candle>>> LoadCandlesFromStorageDebugMode(InstrTypes instType, MarketInterval interval, string symbol)
        //{
        //    OnNewStatusMessage("Начинаем загрузку свечей из хранилища.");

        //    var candles = new Dictionary<string, List<Candle>>();

        //    var item = StorageInfo.FirstOrDefault(l => l.Symbol == symbol);

        //    if (item == null || !item.Active) return null;
        //    if (!File.Exists(Path.Combine(Folders.Candles[instType][interval], $"{item.Symbol}.csv"))) return null;

        //    List<Candle>? list = await Task.Run(() => GetCandles(instType, interval, item.Symbol));

        //    if (list != null)
        //    {
        //            if (!candles.TryAdd(item.Symbol, list))
        //                candles[item.Symbol] = list;
        //            item.StorageStart = list.First().OpenTime; // Update storage start time to the first candle's open time
        //    }

        //    return candles;

        //}

        //private List<Candle>? GetCandles(InstrTypes instType, MarketInterval interval, string symbol)
        //{
        //    var lines = File.ReadAllLines(Path.Combine(Folders.Candles[instType][interval], $"{symbol}.csv"));
        //    if (lines.Length == 0) return null;
        //    var candles = lines.Select(line => Candle.Parse(line)).ToList();
        //    return candles;
        //}

        //private List<CandleStorageInfo> GetLoadSymbolInfo(InstrTypes instType, MarketInterval interval)
        //{
        //    var lines = File.ReadAllLines(Folders.LoadLog[instType][interval]);
        //    return [.. lines.Select(CandleStorageInfo.Parse).Where(info => info != null)];
        //}

        #region Old code for Binance

        //private async Task InitProcessing(List<BnSecurity> securities)
        //{
        //    OnNewStatusMessage("Инструменты загружены.");


        //    OnNewStatusMessage("Начато первичное заполнение коллекций ScreenItems, Candles, OnLineCandles.");

        //    foreach (var sec in securities)
        //    {
        //        var item = new ScreenItem(sec);

        //        ScreenItems.Add(item);
        //        Candles.Add(sec.Symbol, null);
        //        OnlineCandles.Add(sec.Symbol, new List<BnCandle>());
        //    }

        //    OnNewStatusMessage("Первичное заполнение коллекций закончено.");

        //    OnNewStatusMessage("Выполняем подписку на получение свечей через web-socket.");

        //    await SubscribeToOnlineCandleUpdates(securities);

        //    OnNewStatusMessage("Выполняем подписка выполнена.");

        //    OnNewStatusMessage("Начата загрузка свечей и обновление хранилища.");

        //    var start = DateTime.Now;

        //    await LoadCandlesAndUpdateStorage(securities, RawCandleFldr, 3, start);

        //    if (isCalcStoredParams)
        //    {
        //        OnNewStatusMessage("Загружаем дневные свечи.");
        //        var dayCandles = await LoadDayCandlesAndUpdateStorage(securities, CandleDayFldr);
        //        OnNewStatusMessage("Загрузка дневных свечей закончена.");
        //        OnNewStatusMessage("Расчитываем базовые статистики для числа сделок.");
        //        Params.TradesStats = DayStatsCalc(dayCandles);
        //        Params.Save();
        //        OnNewStatusMessage("Сохраняем статистики в файл.");
        //    }
        //    else
        //    {
        //        Params.Load();
        //    }

        //    OnNewStatusMessage(string.Format("Загрузка свечей и обновление хранилища закончены. {0}", (DateTime.Now - start).ToString(@"hh\:mm\:ss")));

        //    OnNewStatusMessage("Сливаем временные и постоянные коллекции свечей.");

        //    await OnlineAndHistoryCandlesMerge();

        //    OnNewStatusMessage("Сканер готов к работе!!!");

        //    timer.Change(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(15));
        //}

        //private async Task SubscribeToOnlineCandleUpdates(List<BnSecurity> securities)
        //{

        //    PublClient.NewCandle += PublClient_NewCandle;
        //    var takeCnt = securities.Count / 3;
        //    int breaknow = 0;

        //    var takesecurities = securities.Take(takeCnt);

        //    while (true)
        //    {
        //        await PublClient.SubscribeToCandleUpdatesAsync(takesecurities.Select(x => x.Symbol).ToList(), new List<KlineInterval> { KlineInterval.OneMinute
        //        });

        //        breaknow++;

        //        if (breaknow == 1)
        //            takesecurities = securities.Skip(takeCnt).Take(takeCnt);
        //        else if (breaknow == 2)
        //            takesecurities = securities.Skip(takeCnt * 2);
        //        else
        //            break;

        //    }
        //}

        //private void OnTimerTick(object obj)
        //{
        //    if (updateMode != CandleUpdateModes.constant) return;

        //    if (Candles == null || !Candles.Any()) return;

        //    DateTime start = DateTime.Now;

        //    ParamCalculate();

        //    разность между текущими максимумом и минимумом;
        //    разность между предыдущей ценой закрытия и текущим максимумом;
        //    разность между предыдущей ценой закрытия и текущим минимумом.

        //    OnNewStatusMessage(string.Format("Время вычислений={0}", DateTime.Now - start));

        //    OnScreenUpdate();


        //}

        //private Dictionary<string, BasicStat> DayStatsCalc(Dictionary<string, List<BnCandle>> dcandles)
        //{
        //    Dictionary<string, BasicStat> stats = new Dictionary<string, BasicStat>();

        //    foreach (var kvp in dcandles)
        //    {
        //        var symbol = kvp.Key;
        //        var candles = kvp.Value;
        //        var n = candles.Count - 1; // размер выборки
        //        var avg = candles.Take(n).Average(c => c.TradeCount); // средняя 
        //        var max = candles.Take(n).Max(c => c.TradeCount);
        //        var min = candles.Take(n).Min(c => c.TradeCount);
        //        var sum2 = candles.Take(n).Sum(c => Math.Pow(c.TradeCount - avg, 2)); // сумма квадратов отклонений
        //        var sd = Math.Sqrt(sum2 / (n - 1));
        //        stats.Add(symbol, new BasicStat { Symbol = symbol, Count = n, Avg = avg, Max = max, Min = min, SD = sd });
        //    }

        //    return stats;

        //}

        //private void ParamCalculate()
        //{


        //    разность между текущими максимумом и минимумом;
        //    разность между предыдущей ценой закрытия и текущим максимумом;
        //    разность между предыдущей ценой закрытия и текущим минимумом.

        //    double btcTrades24 = 0; // индекс активности биткоина
        //    Dictionary<Periods, int> btcTrades = new Dictionary<Periods, int> { { Periods.small, 0 }, { Periods.mid, 0 }, { Periods.large, 0 } };


        //    foreach (var kvp in Candles)
        //    {
        //        var symbol = kvp.Key;
        //        var candles = kvp.Value;

        //        var screenItem = ScreenItems.FirstOrDefault(x => x.Symbol == symbol);

        //        screenItem.Candle = candles.Last();

        //        объем сделок в монетах за периоды
        //        Dictionary<Periods, double> vols = new Dictionary<Periods, double> { { Periods.small, 0 }, { Periods.mid, 0 }, { Periods.large, 0 }, { Periods.h24, 0 } };
        //        объем сделок в USD за периоды
        //        Dictionary<Periods, double> quoteVols = new Dictionary<Periods, double> { { Periods.small, 0 }, { Periods.mid, 0 }, { Periods.large, 0 }, { Periods.h24, 0 } };
        //        объем сделок за периоды
        //        Dictionary<Periods, int> trades = new Dictionary<Periods, int> { { Periods.small, 0 }, { Periods.mid, 0 }, { Periods.large, 0 }, { Periods.h24, 0 } };
        //        цена в конце периода
        //        Dictionary<Periods, double> prices = new Dictionary<Periods, double> { { Periods.small, 0 }, { Periods.mid, 0 }, { Periods.large, 0 }, { Periods.h24, 0 } };
        //        индекс активности за периоды
        //        Dictionary<Periods, double> activeIndexes = new Dictionary<Periods, double> { { Periods.small, 0 }, { Periods.mid, 0 }, { Periods.large, 0 }, { Periods.h24, 0 } };
        //        покупки тейкеров за периоды
        //        Dictionary<Periods, double> buyVolumes = new Dictionary<Periods, double> { { Periods.small, 0 }, { Periods.mid, 0 }, { Periods.large, 0 }, { Periods.h24, 0 } };

        //        int candCount = candles.Count;
        //        var dayCandleCnt = Math.Min(candles.Count, 60 * 24);
        //        var cnt = candCount - dayCandleCnt;

        //        последняя цена
        //        double lastPrice = 0;

        //        var avgTrCnt = candles.Skip(cnt).Average(c => c.TradeCount); // средняя за последние сутки 

        //        double d2Summ = 0; //сумма квадратов разностей

        //        for (var i = candCount - 1; i >= cnt; i--)
        //        {

        //            var candle = candles[i];

        //            foreach (var interval in Enum.GetValues<Periods>())
        //            {
        //                if (candCount - i < periods[interval])
        //                {
        //                    vols[interval] += candle.Volume;
        //                    quoteVols[interval] += candle.QuoteVolume;
        //                    buyVolumes[interval] += candle.TakerBuyBaseVolume;
        //                    trades[interval] += candle.TradeCount;
        //                    prices[interval] = candle.ClosePrice;
        //                }
        //            }

        //            d2Summ += Math.Pow((double)candle.TradeCount - avgTrCnt, 2);

        //            if (i == candCount - 1)
        //                lastPrice = candle.ClosePrice;

        //        }

        //        var sd = Math.Sqrt(d2Summ / (dayCandleCnt - 1));

        //        screenItem.ZTradeCount = (screenItem.Candle.TradeCount - avgTrCnt) / sd;

        //        foreach (var interval in Enum.GetValues<Periods>())
        //        {
        //            screenItem.BaseVolumes[interval] = vols[interval];
        //            screenItem.QuoteVolumes[interval] = quoteVols[interval];
        //            screenItem.BuyVolumes[interval] = buyVolumes[interval];
        //            screenItem.Trades[interval] = trades[interval];
        //            screenItem.PriceChanges[interval] = Math.Round(100 * ((lastPrice / prices[interval] - 1)), 2);

        //            if (kvp.Key == "BTCUSDT")
        //            {
        //                var cndls = candles;
        //                var k = 8;
        //            }

        //        }

        //        if (kvp.Key == "BTCUSDT")
        //        {
        //            btcTrades = trades;
        //        }

        //    }

        //    for (int i = 0; i < ScreenItems.Count; i++)
        //    {
        //        var item = ScreenItems[i];

        //        if (Params.TradesStats.ContainsKey(item.Symbol))
        //        {
        //            var bs = Params.TradesStats[item.Symbol];
        //            var z = (item.Trades[Periods.h24] - bs.Avg) / bs.SD;
        //            item.Z = z;
        //        }

        //        foreach (var interval in Enum.GetValues<Periods>())
        //        {
        //            item.ActiveIndexes[interval] = Math.Round((double)item.Trades[interval] / (double)btcTrades[interval], 2);
        //        }
        //    }
        //}

        //private void PublClient_NewCandle(string symbol, KlineInterval interval, BnCandle candle)
        //{

        //    if (updateMode == CandleUpdateModes.pause) return;

        //    if (updateMode == CandleUpdateModes.temporary)
        //    {
        //        if (OnlineCandles[symbol].Any())
        //        {
        //            if (OnlineCandles[symbol].Last().OpenTime == candle.OpenTime)
        //            {
        //                var inx = OnlineCandles[symbol].Count - 1;
        //                OnlineCandles[symbol][inx] = candle;
        //            }
        //            else
        //            {
        //                OnlineCandles[symbol].Add(candle);
        //            }
        //        }
        //        else
        //        {
        //            OnlineCandles[symbol].Add(candle);
        //        }
        //    }
        //    else if (updateMode == CandleUpdateModes.constant)
        //    {
        //        if (Candles[symbol].Any())
        //        {
        //            if (Candles[symbol].Last().OpenTime == candle.OpenTime)
        //            {
        //                var inx = Candles[symbol].Count - 1;
        //                Candles[symbol][inx] = candle;
        //            }
        //            else
        //            {
        //                Candles[symbol].Add(candle);
        //            }
        //        }
        //        else
        //        {
        //            Candles[symbol].Add(candle);
        //        }

        //    }


        //}



        //private async Task SaveCandles(List<BnSecurity> securities, string candleFldr)
        //{

        //    var secCnt = securities.Count;

        //    var takeCount = 5;

        //    int cnt = 0;

        //    foreach (var sec in securities)
        //    {
        //        var secFldr = Path.Combine(candleFldr, sec.Symbol);
        //        if (!Directory.Exists(secFldr))
        //            Directory.CreateDirectory(secFldr);

        //        DateTime firstDate = DateTime.MinValue;
        //        DateTime lastDate = DateTime.MinValue;
        //        DateTime startTime = DateTime.MinValue; // первая дата запроса

        //        var secDI = new DirectoryInfo(secFldr);
        //        var candleFiles = secDI.GetFiles();

        //        if (candleFiles.Any())
        //        {
        //            firstDate = DateTime.ParseExact(candleFiles.First().Name.Replace(".csv", ""), "yyyyMMdd", null);
        //            lastDate = DateTime.ParseExact(candleFiles.Last().Name.Replace(".csv", ""), "yyyyMMdd", null);
        //            var lines = File.ReadAllLines(candleFiles.Last().FullName).TakeLast(takeCount);
        //            var lastCandle = GetBnCandleFromLine(lines.Last(), lastDate);
        //            var hist_candles = lines.Select(l => bnadapt.Helpers.GetBnCandleFromLine(l, lastDate));
        //            startTime = hist_candles.Last().OpenTime.AddMinutes(1);
        //            Candles[sec.Symbol] = hist_candles.ToList();
        //        }
        //        else
        //        {
        //            firstDate = new DateTime(2024, 07, 1);
        //            startTime = firstDate;
        //        }

        //        var klines = await PublClient.GetSpotCandlesHistoryFromAsync(sec.Symbol, KlineInterval.OneMinute, startTime);

        //        var candles = klines.Select(k => k.ToBnCandle()).ToList();

        //        Candles[sec.Symbol].AddRange(candles);

        //        var groops = candles.GroupBy(c => c.OpenTime.Date);

        //        foreach (var groop in groops)
        //        {
        //            var date = groop.Key;
        //            var filePath = Path.Combine(secFldr, string.Format("{0}.csv", date.ToString("yyyyMMdd")));
        //            groop.CandlesSaveToFile(filePath, "", true);
        //        }

        //        cnt++;

        //        var per = 100 * ((double)cnt / (double)secCnt);

        //        OnNewStatusMessage(string.Format("{0}. {1} loaded. {2}%", cnt, sec.Symbol, Math.Round(per, 1)));

        //    }
        //}

        //private async Task LoadCandlesAndUpdateStorage(List<BnSecurity> securities, string candleFldr, int loadDays, DateTime start)
        //{

        //    var secCnt = securities.Count;

        //    int cnt = 0;

        //    var secHalfCnt = securities.Count / 2;

        //    var securities1 = securities.Take(secHalfCnt).ToList();
        //    var securities2 = securities.Skip(secHalfCnt).ToList();

        //    Task[] tasks = new Task[2];


        //    tasks[0] = Task.Run(() => SaveCandles(securities1, candleFldr, loadDays, start));
        //    tasks[1] = Task.Run(() => SaveCandles(securities2, candleFldr, loadDays, start));

        //    await Task.WhenAll(tasks);

        //    var t = 7;

        //    foreach (var sec in securities)
        //    {
        //        cnt++;
        //        await SaveCandlesForSecurity(sec, candleFldr, loadDays);
        //        var per = 100 * ((double)cnt / (double)secCnt);
        //        OnNewStatusMessage(string.Format("{0}. {1} loaded. {2}% -{3}-", cnt, sec.Symbol, Math.Round(per, 1), (DateTime.Now - start).ToString(@"hh\:mm\:ss")));
        //    }

        //}

        //private async Task<Dictionary<string, List<BnCandle>>> LoadDayCandlesAndUpdateStorage(List<BnSecurity> securities, string candleFldr)
        //{

        //    var dict = new Dictionary<string, List<BnCandle>>();

        //    foreach (var sec in securities)
        //    {
        //        var file = Path.Combine(candleFldr, string.Format("{0}.csv", sec.Symbol));
        //        List<BnCandle> candles = new List<BnCandle>();

        //        if (File.Exists(file))
        //        {
        //            candles = bnadapt.Helpers.GetDaylyCandlesFromFile(file);
        //            var onlineCandles = await PublClient.GetSpotCandlesHistoryAsync(sec.Symbol, KlineInterval.OneDay, candles.Last().OpenTime);
        //            if (onlineCandles.Any())
        //            {
        //                foreach (var kline in onlineCandles)
        //                {
        //                    var candle = candles.LastOrDefault(c => c.OpenTime == kline.OpenTime);

        //                    if (candle.OpenTime > DateTime.MinValue)
        //                    {
        //                        var idx = candles.LastIndexOf(candle);
        //                        candles[idx] = kline.ToBnCandle();
        //                    }
        //                    else
        //                    {
        //                        candles.Add(kline.ToBnCandle());
        //                    }
        //                }
        //                candles.SaveToDaylyFile(file);
        //            }
        //            else
        //            {
        //                var v = 0;
        //            }
        //        }
        //        else
        //        {
        //            var klines = await PublClient.GetSpotCandlesHistoryAsync(sec.Symbol, KlineInterval.OneDay);
        //            klines.SaveToDaylyFile(file);
        //            candles.AddRange(klines.Select(k => k.ToBnCandle()).ToList());
        //        }

        //        if (candles.Any())
        //            dict.Add(sec.Symbol, candles);

        //    }

        //    return dict;

        //}

        //private async Task SaveCandles(List<BnSecurity> securities, string candleFldr, int loadDays, DateTime start)
        //{
        //    var secCnt = securities.Count;
        //    int cnt = 0;

        //    foreach (var sec in securities)
        //    {
        //        cnt++;
        //        await SaveCandlesForSecurity(sec, candleFldr, loadDays);
        //        var per = 100 * ((double)cnt / (double)secCnt);
        //        OnNewStatusMessage(string.Format("{0}. loaded. {1}% -{2}-", sec.Symbol, Math.Round(per, 1), (DateTime.Now - start).ToString(@"hh\:mm\:ss")));
        //    }

        //}

        //private async Task SaveCandlesForSecurity(BnSecurity sec, string candleFldr, int loadDays, bool isprodaction = true)
        //{

        //    var secFldr = Path.Combine(candleFldr, sec.Symbol);

        //    DateTime startLoadDate = DateTime.UtcNow.AddDays(-loadDays).Date;

        //    if (!Directory.Exists(secFldr))
        //        Directory.CreateDirectory(secFldr);

        //    DateTime firstDate = DateTime.MinValue;
        //    DateTime lastDate = DateTime.MinValue;
        //    DateTime startTime = DateTime.MinValue; // первая дата запроса

        //    var secDI = new DirectoryInfo(secFldr);
        //    var candleFiles = secDI.GetFiles();

        //    var loadCandles = new List<BnCandle>();

        //    if (candleFiles.Any())
        //    {
        //        firstDate = DateTime.ParseExact(candleFiles.First().Name.Replace(".csv", ""), "yyyyMMdd", null);
        //        lastDate = DateTime.ParseExact(candleFiles.Last().Name.Replace(".csv", ""), "yyyyMMdd", null);
        //        startTime = lastDate;

        //        if (lastDate > startLoadDate)
        //        {
        //            var loadFiles = candleFiles.SkipWhile(f => bnadapt.Helpers.GetDateFromFile(f.Name).Date < startLoadDate);
        //            loadCandles = loadFiles.GetCandlesFromFileInfoList();
        //            startTime = loadCandles.Last().OpenTime.AddMinutes(1);
        //            Candles[sec.Symbol] = loadCandles;
        //        }


        //    }
        //    else
        //    {
        //        firstDate = new DateTime(2024, 07, 1);
        //        startTime = firstDate;
        //    }

        //    var candles = new List<BnCandle>();

        //    if (isprodaction)
        //    {
        //        var klines = await PublClient.GetSpotCandlesHistoryFromAsync(sec.Symbol, KlineInterval.OneMinute, startTime);
        //        candles = klines.Select(k => k.ToBnCandle()).ToList();

        //        var groops = candles.GroupBy(c => c.OpenTime.Date);

        //        foreach (var groop in groops)
        //        {
        //            var date = groop.Key;
        //            var filePath = Path.Combine(secFldr, string.Format("{0}.csv", date.ToString("yyyyMMdd")));
        //            groop.CandlesSaveToFile(filePath, "", true);
        //        }

        //        if (Candles[sec.Symbol] == null)
        //            Candles[sec.Symbol] = candles.SkipWhile(c => c.OpenTime.Date < startLoadDate).ToList();
        //        else
        //            Candles[sec.Symbol].AddRange(candles);

        //    }


        //}

        /// <summary>
        /// Сливаем исторические данные с онлайновыми, включаем флаг,
        /// </summary>
        /// <returns></returns>
        //private async Task OnlineAndHistoryCandlesMerge()
        //{
        //    updateMode = CandleUpdateModes.pause;

        //    await Task.Delay(20);

        //    foreach (var kvp in OnlineCandles)
        //    {
        //        if (kvp.Value != null && kvp.Value.Any())
        //        {
        //            var candlesForAdd = OnlineCandles[kvp.Key].Where(c => c.OpenTime > Candles[kvp.Key].Last().OpenTime);
        //            if (candlesForAdd.Any())
        //                Candles[kvp.Key].AddRange(candlesForAdd);
        //        }
        //    }

        //    updateMode = CandleUpdateModes.constant;

        //    await Task.Delay(20);
        //}

        #endregion

        #region Market Breadth

        //private void MarketBreadthCalc(List<BnSecurity> securities)
        //{
        //    ////var mbFldr = Path.Combine( //MarketBreadth

        //    //OnNewStatusMessage("Начата калькуляция индикатора MarketBreadth.");


        //    //var mbItems = new List<MarketBreadthItem>();
        //    //MarketBreadthItem mbitem = null;


        //    //try
        //    //{

        //    //    var BTCdi = new DirectoryInfo(Path.Combine(RawCandleFldr, "BTCUSDT"));

        //    //    //var files = BTCdi.GetFiles().ToArray();

        //    //    var times = BTCdi.GetFiles().Select(f => bnadapt.Helpers.GetDateFromFile(f.Name)).ToList();

        //    //    var dict = new Dictionary<string, List<BnCandle>>();

        //    //    int minCnt = 60 * 24; // 1440 

        //    //    bool isfirstday = true;

        //    //    foreach (var date in times)
        //    //    {

        //    //        foreach (var sec in securities)
        //    //        {
        //    //            var file = Path.Combine(RawCandleFldr, sec.Symbol, bnadapt.Helpers.GetFileNameFromDate(date));
        //    //            if (File.Exists(file))
        //    //            {
        //    //                var candles = bnadapt.Helpers.GetCandlesFromFile(file, date);
        //    //                if (dict.ContainsKey(sec.Symbol))
        //    //                {
        //    //                    dict[sec.Symbol].AddRange(candles);
        //    //                }
        //    //                else
        //    //                    dict.Add(sec.Symbol, candles);
        //    //            }
        //    //        }

        //    //        if (isfirstday)
        //    //        {
        //    //            isfirstday = false;
        //    //            continue;
        //    //        }

        //    //        int idx = minCnt;

        //    //        int endCnt = 0;

        //    //        if (dict["BTCUSDT"].Count <= minCnt * 2)
        //    //            endCnt = dict["BTCUSDT"].Count;
        //    //        else
        //    //            endCnt = minCnt * 2;

        //    //        for (var i = minCnt; i < endCnt; i++)
        //    //        {

        //    //            try
        //    //            {
        //    //                mbitem = new MarketBreadthItem() { Time = dict["BTCUSDT"][i].OpenTime };
        //    //            }
        //    //            catch (Exception ex)
        //    //            {
        //    //                var r = 9;
        //    //            }



        //    //            foreach (var kvp in dict)
        //    //            {
        //    //                if (kvp.Value.Count < minCnt * 2) continue;
        //    //                //var diff = 100 * (kvp.Value[idx].ClosePrice / kvp.Value[idx - minCnt].ClosePrice - 1);
        //    //                var diff = kvp.Value[i].ClosePrice - kvp.Value[i - minCnt].ClosePrice;
        //    //                mbitem.BullCnt += diff > 0 ? 1 : 0;
        //    //                mbitem.BearCnt += diff < 0 ? 1 : 0;
        //    //            }

        //    //            mbItems.Add(mbitem);

        //    //        }

        //    //        foreach (var kvp in dict)
        //    //        {
        //    //            if (kvp.Value.Count < minCnt * 2) continue;
        //    //            //var diff = 100 * (kvp.Value[idx].ClosePrice / kvp.Value[idx - minCnt].ClosePrice - 1);
        //    //            kvp.Value.RemoveAll(c => c.OpenTime.Date < date.Date);
        //    //        }

        //    //        var kk = 0;


        //    //    }

        //    //    var savefile = Path.Combine(MarketBreadthFldr, "MarketBreadth.csv");

        //    //    mbItems.SaveToFile(savefile);

        //    //    OnNewStatusMessage("Калькуляция индикатора MarketBreadth закончена.");

        //    //}
        //    //catch (Exception ex)
        //    //{
        //    //    var itms = mbItems;
        //    //    var itm = mbitem;
        //    //    logger.ToError(ex);
        //    //}



        //}

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

        #endregion

        #region CryptoIndex

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

        //private static async Task<Dictionary<string, List<BnCandle>>> GetRawCandlesFromFile(string candleFldr)
        //{
        //    var candles = new Dictionary<string, List<BnCandle>>();

        //    var smbsDi = new DirectoryInfo(candleFldr);

        //    List<Task<List<BnCandle>>> tasks = [];

        //    var subDirectories = smbsDi.GetDirectories();

        //    foreach (var smbDi in subDirectories)
        //    {
        //        tasks.Add(Task.Factory.StartNew(() => smbDi.GetFiles().GetCandlesFromFileInfoList()));
        //        //candles.Add(smbDi.Name, temp);
        //    }

        //    await Task.WhenAll(tasks);

        //    for (var i = 0; i < subDirectories.Length; i++)
        //    {
        //        var res = await tasks[i];

        //        candles.Add(subDirectories[i].Name, res);

        //    }

        //    //foreach (var fi in di.GetFiles())
        //    //{
        //    //    var symbol = fi.Name.Replace(".csv", "");
        //    //    var lines = File.ReadAllLines(fi.FullName);
        //    //    var cndls = lines.Select(l => GetBnCandleFromLine(l));
        //    //    candles.Add(symbol, cndls.ToList());
        //    //}



        //    return candles;

        //}

        //private Tuple<List<TimeSeriesItem>, List<IndexCalcItem>>? CalculateIndex(Dictionary<string, List<BnCandle>> candles, int extremeCount = 15)
        //{
        //    var items = new List<TimeSeriesItem>();

        //    Dictionary<string, IndexCalcItem> temp = [];

        //    int count = candles["BTCUSDT"].Count;

        //    try
        //    {

        //        for (int i = 0; i < count; i++)
        //        {
        //            var time = candles["BTCUSDT"][i].OpenTime;

        //            foreach (var kvp in candles)
        //            {
        //                var key = kvp.Key;
        //                var val = kvp.Value;

        //                if (i == 0)
        //                {
        //                    temp.Add(key, new IndexCalcItem()
        //                    {
        //                        Symbol = key,
        //                        FirstPrice = val[i].ClosePrice,
        //                        FirstTime = val[i].OpenTime
        //                    });

        //                    if (val[i].OpenTime == time)
        //                        temp[key].StartIndex = 0;
        //                    else
        //                        temp[key].StartIndex = (int)(val[i].OpenTime - time).TotalMinutes;
        //                }
        //                else if (temp[key].StartIndex <= i)
        //                {
        //                    try
        //                    {

        //                        int shift = temp[key].StartIndex;

        //                        if (val.Count > i - shift)
        //                        {
        //                            temp[key].NowTime = val[i - shift].OpenTime;
        //                            temp[key].NowPrice = val[i - shift].ClosePrice;
        //                            temp[key].Count++;
        //                            // изменение цены за последние 24 часа
        //                            if (i >= MinOfDay - 1 + shift)
        //                            {
        //                                temp[key].Prev24hPrice = val[i - (MinOfDay - 1 + shift)].ClosePrice;
        //                                temp[key].Active = true;
        //                            }
        //                        }
        //                        else
        //                        {
        //                            temp[key].Active = false;
        //                        }

        //                    }
        //                    catch (Exception ex)
        //                    {
        //                        logger.ToError(ex);
        //                    }

        //                }

        //            }

        //            if (i < MinOfDay - 1) continue;

        //            var changes24h = temp.Values.Where(v => v.Active).OrderBy(v => v.Change24h).ToArray();

        //            var lowParthMinus = changes24h.Skip(extremeCount).ToArray();

        //            var lowAndHighParthMinus = lowParthMinus.Take(lowParthMinus.Length - extremeCount).ToArray();

        //            var index = lowAndHighParthMinus.Average(k => k.FullChange);

        //            items.Add(new TimeSeriesItem { Time = time, Value = Math.Round(index, 2) });

        //        }

        //        var tpl = new Tuple<List<TimeSeriesItem>, List<IndexCalcItem>>(items, [.. temp.Values ]);

        //        return tpl;
        //    }
        //    catch (Exception ex)
        //    {
        //        logger.ToError(ex);
        //    }

        //    return null;

        //}

        //private void CandlesIndexValidate(Dictionary<string, List<BnCandle>> candles)
        //{


        //    foreach (var kvp in candles)
        //    {

        //        var cndls = candles[kvp.Key];

        //        for (var i = 0; i < cndls.Count - 1; i++)
        //        {
        //            if (cndls[i + 1].OpenTime - cndls[i].OpenTime == TimeSpan.FromMinutes(1))
        //            {
        //                //var k = 0;
        //            }
        //            else
        //            {
        //                //var hh = 9;
        //            }

        //        }

        //    }

        //   // var t = 9;


        //}

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
            var fi = new DirectoryInfo(Folders.CoinMarketCap).GetFiles().First();
            var caps = CoinCupInfo.LoadFromStorage(fi.FullName);

            indexSeries.Add(new TimeSeriesElement { Time = startTime, Value = startValue });

            var rebalTimes = GetRebalanceTimes(startTime);

            List<RelIndexCalcItem>? indexItems = null;
            DateTime reblTime = DateTime.MinValue;

            for (var t = 0; t < rebalTimes.Count - 1; t++) 
            {
                reblTime = rebalTimes[t];
                var nextReblTime = rebalTimes[t+1];

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

                var prevCandle = Candles[vs.Symbol].FirstOrDefault(c => c.OpenTime == reblTime.AddMinutes(-15));
                var candle = Candles[vs.Symbol].FirstOrDefault(c => c.OpenTime == reblTime);

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
                nextRebTime = Candles[indexItems[0].Symbol].Last().OpenTime;

            var ts = nextRebTime.GetValueOrDefault() - lastReblTime;

            var t = 1 + (int)(ts.TotalMinutes / 15);
            double index = lastIndex;

            Candle[][] cndls = new Candle[indexItems.Count][];

            for (var i = 0; i < indexItems.Count; i++)
            {
                var temp = Candles[indexItems[i].Symbol].Where(c => c.OpenTime >= lastReblTime && c.OpenTime <= nextRebTime.GetValueOrDefault());
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
            List<DateTime> times = [ start ];

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

        #endregion

        #region utilites

        /// <summary>
        /// Проверяем непрерывность (наличие пропусков) данных в хранилище
        /// </summary>
        public void CandelStorageMonitoring()
        {
            foreach(var kvp in Candles)
            {
                var symbol = kvp.Key;
                var candles = kvp.Value;
                var start = candles.First().OpenTime;
                var end = candles.Last().OpenTime;
                var time = start.AddMinutes(15);
                var calcCnt = 1 + ((int)(end - start).TotalMinutes / 15);  

                if(candles.Count != calcCnt)
                {
                   // var s = 0;
                }


                //List<DateTime> losdates = new List<DateTime>(); 

                //while (end >= time) 
                //{
                //    if(candles.All(c => c.OpenTime != time))
                //    {
                //        losdates.Add(time);
                //    }

                //    time = time.AddMinutes(15);
                //}

                //var f = 0;

            }
           // var l = 0;
        }

        /// <summary>
        /// Проверяем данные хранилища на наличие гэпов 
        /// </summary>
        public async Task CandelGapsMonitoring()
        {
            var exepts = Candles.Keys.SkipWhile(k => k != "CLANKERUSDT").ToList();
            exepts.RemoveAt(0);

            foreach (var kvp in Candles)
            {
                if (!exepts.Contains(kvp.Key)) continue;

                var symbol = kvp.Key;
                var candles = kvp.Value;
                var start = candles.First().OpenTime;
                var end = candles.Last().OpenTime;
                var time = start.AddMinutes(15);
                var calcCnt = 1 + ((int)(end - start).TotalMinutes / 15);
                int gaps = 0;
                List<Candle> corrCandles = []; 

                if (candles.Count != calcCnt)
                {
                    // var s = 0;
                }

                for(var i = 1; i < candles.Count; i ++)
                {
                    var prevCndl = candles[i - 1];
                    var nowCndl = candles[i];
                    var prevTime = prevCndl.OpenTime;
                    var nowTime = nowCndl.OpenTime;
                    var prevClose = prevCndl.Close;
                    var nowOpen = nowCndl.Open;

                    if (nowTime - prevTime == TimeSpan.FromMinutes(15))
                    {
                        var gap = nowCndl.Open - prevCndl.Close;
                        var gapP = Math.Abs(100 * (nowCndl.Open / prevCndl.Close - 1));

                        if (gapP > 0.09)
                        {
                            gaps++;
                            OnNewStatusMessage($"{symbol}. gap={gapP}. gaps={gaps}");
                            var temp = await BbClient.GetCandlesHistory(Category.LINEAR, symbol, MarketInterval.FifteenMinutes, prevTime, nowTime);

                            temp = [.. temp.OrderBy(c => c.OpenTime)];

                            var gapP2 = Math.Abs(100 * (temp.Last().Open / temp.First().Close - 1));

                            if (gapP2 > 0.01)
                            {
                                var s = 7;
                            }
                                corrCandles.AddRange(temp);

                            var d = 7;
                        }
                    }
                    else
                    {

                    }



                }

                if (corrCandles.Count > 0)
                {
                    foreach (var item in corrCandles)
                    {
                        var candleforchange = candles.FirstOrDefault(c => c.OpenTime == item.OpenTime);
                        var indx = candles.IndexOf(candleforchange);
                        candles[indx] = item;
                    }

                    File.AppendAllLines(Path.Combine(Folders.Candles[DefaultInstrType][DefaultInterval], "Correct", $"{symbol}.csv"), candles.Select(c => c.ToString()));
                }

            }
            // var l = 0;



        }

        #endregion

    }
}
