using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace Synapse.Crypto.Patterns
{

    [DisplayName("Retests")]
    [Description("Иннформация о ретестах")]
    public class RetestPattern : BreakdownPattern
    {

        private DateTime? _retestTime;
        /// <summary>
        /// Время ретеста
        /// </summary>
        [Category("Break/Retest Time")]
        [PropertyOrder(1)]
        [DisplayName("Ретест максимума")]
        [Description("Время ретеста максимума.")]
        [ReadOnly(true)]
        public DateTime? RetestTime
        {
            get => _retestTime;
            set
            {
                _retestTime = value;
                base.NotifyPropertyChanged();
            }
        }

    }
}
