using Synapse.General;
using Synapse.Crypto.Bybit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Navigation;
using Synapse.Crypto.Trading;

namespace Synapse.Crypto.Patterns
{
    public class CandleViewModel : BaseViewModel
    {

        public CandleViewModel()
        {
        }

        private Candle _candle;
        public Candle Candle 
        {
            get => _candle;  
            set 
            { 
                _candle = value; 
                NotifyPropertyChanged();
            } 
        }

    }
}
