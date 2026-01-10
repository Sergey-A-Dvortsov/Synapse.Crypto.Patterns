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

    // Пробойная свеча
    // 1. Пробито тенью
    // 1.1. Пробившая свеча имеет одинаковый цвет с направлением пробоя...
    // Например, пробой снизу вверх, а свеча зеленая 
    // 1.2. Пробившая свеча имеет противоположный цвет направлению пробоя...
    // Например, пробой снизу вверх, а свеча красная
    // 1.3. Пробившая свеча имеет тип "hummer" вне зависимости от цвета
    // 1.4. Пробившая свеча имеет тип "doji" или "волчок" , при этом тень противоположная направлению
    // пробоя маленькая или отсутствует.
    // 1.5. "Doji" или "волчок" c приблизительно одинаковыми тенями.
    // 2. Пробито телом, цена закрытия за уровнем пробоя

    // Вторая свеча (следует за пробойной) должна иметь цвет противоположный направлению пробоя
    // 3.1 Цена цена открытия в зоне пробоя, закрытие ушло из зоны пробоя, цвет противоположный направлению пробоя (используется в случае 2.)
    // 3.1 Цена цена открытия и закрытия вне зоны пробоя, закрытие ушло из зоны пробоя
    // 3.2 Цена закрытия не вернулась за уровень пробоя

    // Третья свеча (используется в случае 3.2) должна иметь цвет противоположный направлению пробоя
    // 4. Цена закрытия вернулась за уровень пробоя 

    public enum FalseBreakDownStyle
    {
        /// <summary>
        /// Пробито тенью, цвет свечи соответсвует направлению пробоя
        /// </summary>
        ShadowInLine,
        /// <summary>
        /// Пробито тенью, цвет свечи противоположный направлению пробоя
        /// </summary>
        ShadowOpossite,
        /// <summary>
        /// Пробито тенью, тип свечи hummer вне зависимости от ее цвета
        /// </summary>
        ShadowHummer,
        /// <summary>
        /// Пробито тенью, тип свечи doji или волчок вне зависимости от цвета. Тень противоположная направлению пробоя маленькая или отсутствует.
        /// </summary>
        ShadowDoji,
        /// <summary>
        /// Пробито тенью, тип свечи doji или волчок вне зависимости от цвета. Тени приблизительно равны
        /// </summary>
        ShadowFormalDoji,
        /// <summary>
        /// Пробито телом, цена закрытия за уровнем пробоя
        /// </summary>
        BodyClose
    }

    public enum SecondBreakDownStyle
    {


    }

    [CategoryOrder("High/Low levels", 0)]
    [CategoryOrder("Break/Retest Time", 1)]
    [CategoryOrder("Дополнительно", 3)]
    [DisplayName("False breakdowns")]
    [Description("Иннформация о ложных пробоях")]
    public class FalseBreakdownPattern : BreakdownPattern
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
                NotifyPropertyChanged();
            }
        }

    }

}
