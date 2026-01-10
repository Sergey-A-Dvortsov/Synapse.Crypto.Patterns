using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Synapse.Crypto.Patterns
{
    public class MarketBreadthItem
    {
        public DateTime Time { set; get; }
        public int BullCnt { set; get; }
        public int BearCnt { set; get; }

        public override string ToString()
        {
            return string.Format("{0};{1};{2}", Time, BullCnt, BearCnt);
        }
    }
}
