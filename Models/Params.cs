using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Synapse.General;

namespace Synapse.Crypto.Patterns
{
    // Copyright(c) [2026], [Sergey Dvortsov]
    /// <summary>
    /// Базовая статистика
    /// </summary>
    public class BasicStat
    {

        public BasicStat() { }  

        public BasicStat(string line)
        {
            Load(line);
        }

        public string Symbol { get; set; }

        /// <summary>
        /// Число элементов в выборке
        /// </summary>
        public int Count { get; set; }
        /// <summary>
        /// Среднее
        /// </summary>
        public double Avg { get; set; }

        /// <summary>
        /// Максимальное значение
        /// </summary>
        public double Max { get; set; }

        /// <summary>
        /// Минимальное значение
        /// </summary>
        public double Min { get; set; }

        /// <summary>
        /// Стандартное отклонение
        /// </summary>
        public double SD { get; set; }


        public void Load(string line)
        {
            //var arr = line.Split(";");
            //Symbol = arr[0];
            //Count = int.Parse(arr[1]);
            //Avg = double.Parse(arr[2]);
            //Max = double.Parse(arr[3]);
            //Min = double.Parse(arr[4]);
            //SD = double.Parse(arr[5]);
        }

        public void Load()
        {
            //Symbol = storage.GetValue<string>("Symbol");
            //Count = storage.GetValue<int>("Count");
            //Avg = storage.GetValue<double>("Avg");
            //SD = storage.GetValue<double>("SD");
        }

        public void Save()
        {
            //storage.SetValue("Symbol", Symbol);
            //storage.SetValue("Count", Count);
            //storage.SetValue("Avg", Avg);
            //storage.SetValue("SD", SD);
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append(Symbol + ";");
            sb.Append(Count + ";");
            sb.Append(Avg + ";");
            sb.Append(Max + ";");
            sb.Append(Min + ";");
            sb.Append(SD);
            return sb.ToString();
        }
    }

    public class StoredParameters
    {

        public StoredParameters(string rootfldr)
        {
            TradesStats = new Dictionary<string, BasicStat>();
            RootFldr = rootfldr;
        }

        public Dictionary<string, BasicStat> TradesStats { get; set; }

        public string RootFldr { private set; get; }

        public string TradesStatsFile { get => "DayTradesStats.csv"; }

        public void Save()
        {

            var file = Path.Combine(RootFldr, "Params", TradesStatsFile);
            TradesStats.Values.Where(v => v.Count > 1).SaveToFile(file);
        }

        public void Load()
        {

            var file = Path.Combine(RootFldr, "Params", TradesStatsFile);
            var lines = File.ReadAllLines(file);
            IEnumerable<BasicStat> stats = lines.Select(l => new BasicStat(l));

            foreach (BasicStat stat in stats)
            {
                TradesStats.Add(stat.Symbol, stat);
            }
            
        }

        //public void Load()
        //{
        //    //storage.SetValue("TradesStats", TradesStats.Select(k => k.Value.Save()).ToArray());

        //    //var tradesStatStorage = storage.GetValue<SettingsStorage[]>("TradesStats");

        //    //foreach (var settings in tradesStatStorage)
        //    //{
        //    //    var stat = new BasicStat();
        //    //    stat.Load(settings);
        //    //    TradesStats.Add(stat.Symbol, stat);
        //    //}

        //}

        //public void Save()
        //{
        //    //storage.SetValue("TradesStats", TradesStats.Select(k => k.Value.Save()).ToArray());
        //}
    }


}
