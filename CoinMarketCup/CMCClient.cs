using MathNet.Numerics.Distributions;
using Newtonsoft.Json;
using NLog;
using Synapse.General;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Shapes;
using System.IO;
using System.Collections.Generic;

namespace Synapse.Crypto.Patterns.CoinMarketCup
{
    /// <summary>
    /// Взаимодействует с API CoinMarketCup
    /// </summary>
    public class CMCClient
    {
        //private static string API_KEY = "b54bcf4d-1bca-4e8e-9a24-22ff2c3d462c";

        private static CMCClient? _client;

        private readonly Logger logger = LogManager.GetCurrentClassLogger();

        private readonly static string api_key = Properties.Settings.Default.CMCKey;
        //"3d60c087-c1bc-487e-8deb-c602bf4375d9";

        private readonly static HttpClient httpClient = new();

        private readonly List<string> stableCoins = ["USDT", "USDC", "DAI", "TUSDT"];

        private readonly AppRoot root = AppRoot.GetInstance();

        private CMCClient() {}

        public static CMCClient GetInstance()
        {
            if (_client == null)
            {
                lock (typeof(CMCClient))
                {
                    _client = new CMCClient();
                }
            }
            return _client;
        }

        public List<CoinCupInfo> CoinCaps { get; private set; } = [];

        public List<CoinCupInfo> GetCoinCaps()
        {
            return CoinCaps;
        }

        /// <summary>
        /// Получает информацию о рыночной капитализации монет, торгуемых в виде вечных фьючерсов на Bybit.
        /// Если есть соответствующий файл в хранилище и дата его создания равна текущей дате, то данные получают 
        /// из локального хранилища, в противном случае данные получают с сервера CoinMarketCap и выполняют запись 
        /// в локальном хранилище.
        /// </summary>
        /// <param name="fromStorage">получить их локального хранилища</param>
        public async Task LoadCoinCapInfo(List<CoinCupInfo> coinCaps)
        {

            var filepath = System.IO.Path.Combine(root.Folders.CoinMarketCap, $"CoinMarketCup{DateTime.UtcNow:yyMM01}.csv");

            bool fromStorage = File.Exists(filepath);

            CoinCaps.Clear();

            if (fromStorage)
            {
                CoinCaps.AddRange(CoinCupInfo.LoadFromStorage(filepath));
            }
            else 
            {
                var cmcinfo = await GetCoinCapInfo();

                foreach (var sec in root.Swaps)
                {
                    var coininfo = cmcinfo?.FirstOrDefault(ci => ci.symbol.Equals(sec.BaseCoin, StringComparison.CurrentCultureIgnoreCase));
                    if (coininfo == null) continue;
                    CoinCaps.Add(coininfo.ToCoinCupInfo());
                }

                CoinCaps.SaveToFile(filepath);

            }
        }

        /// <summary>
        /// Получает данные, связанные с рыночной капитализацией c сервиса CoinMarketCap (top 5000 coins).
        /// </summary>
        /// <returns></returns>
        public async Task<CMCCoinInfo[]?> GetCoinCapInfo()
        {
            try
            {
                var response = await MakeCapInfoRequest();

                if (response != null && response.IsSuccessStatusCode)
                {
                    string content = response.Content.ReadAsStringAsync().Result;
                    var result = JsonConvert.DeserializeObject<CMCResponse>(content);
                    if (result != null && result.Status.error_code == 0)
                    {
                        return result.Data;
                    }
                }
                else
                {
                    throw new Exception($"Error. status={response?.StatusCode.ToString()}");
                }

            }
            catch (WebException ex)
            {
               logger.ToError(ex);
            }
            catch (Exception ex)
            {
                logger.ToError(ex);
            }
            return null;
        }

        private async Task<HttpResponseMessage> MakeCapInfoRequest()
        {

            var ub = new UriBuilder(@"https://pro-api.coinmarketcap.com/v1/cryptocurrency/listings/latest");
            var queryString = HttpUtility.ParseQueryString(string.Empty);
            queryString["start"] = "1";
            queryString["limit"] = "5000";
            queryString["convert"] = "USD";
            ub.Query = queryString.ToString();

            var request = new HttpRequestMessage(HttpMethod.Get, ub.Uri);
            request.Headers.Add("X-CMC_PRO_API_KEY", api_key);
            request.Headers.Add("Accepts", "application/json");

            return await httpClient.SendAsync(request);

        }

    }
}
