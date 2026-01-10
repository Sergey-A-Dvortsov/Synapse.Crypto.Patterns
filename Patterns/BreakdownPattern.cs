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

    [CategoryOrder("High/Low levels", 0)]
    [CategoryOrder("Times", 1)]
    [CategoryOrder("Дополнительно", 3)]
    [DisplayName("Breakdown")]
    [Description("Информация о пробоях")]
    public abstract class BreakdownPattern : INotifyPropertyChanged
    {

            private BreakStyles _breakDownStyle;
            /// <summary>
            /// Стиль пробоя экстремума
            /// </summary>
            public BreakStyles BreakDownStyle
            {
                get => _breakDownStyle;
                set
                {
                    _breakDownStyle = value;
                    NotifyPropertyChanged();
                }
            }

            private BreakDownSide _breakSide;
            /// <summary>
            /// Направление пробоя
            /// </summary>
            public BreakDownSide BreakSide
            {
                get => _breakSide;
                set
                {
                    _breakSide = value;
                    NotifyPropertyChanged();
                }
            }

            private DateTime? _breakTime;
            /// <summary>
            /// Время пробоя
            /// </summary>
            [Category("Times")]
            [PropertyOrder(0)]
            [DisplayName("Пробитие уровня")]
            [Description("Время пробития уровня.")]
            [ReadOnly(true)]
            public DateTime? BreakTime
            {
                get => _breakTime;
                set
                {
                    _breakTime = value;
                    NotifyPropertyChanged();
                }
            }

            #region NotifyPropertyChanged

            public event PropertyChangedEventHandler? PropertyChanged;

            public void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }

            #endregion

        }
    }
