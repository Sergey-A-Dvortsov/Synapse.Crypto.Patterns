using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Synapse.Crypto.Patterns
{
        public class BaseViewModel : INotifyPropertyChanged
        {

            public event PropertyChangedEventHandler PropertyChanged;

            public void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }


        }

}
