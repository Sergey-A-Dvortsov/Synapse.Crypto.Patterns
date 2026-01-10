using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Synapse.Crypto.Patterns
{
    /// <summary>
    /// Вспомогательный класс для расчета Relative Crypto Index
    /// </summary>
    public class RelIndexCalcItem
    {
        /// <summary>
        /// Инструмент
        /// </summary>
        public string Symbol { set; get; }

        /// <summary>
        /// Цена в момент ребалансировки или начала калькуляции индекса
        /// </summary>
        public double RebalPrice { set; get; }

        /// <summary>
        /// Цена предыдущего периода
        /// </summary>
        public double PrevPrice { set; get; }

        /// <summary>
        /// Текущая цена
        /// </summary>
        public double NowPrice { set; get; }

        /// <summary>
        /// Относительное изменение цены, скорректированное по волатильности.
        /// Инструменты с большей волатильностью будет иметь меньший вес и наоборот.
        /// Делается для устранения "перекоса" индекса в сторону инструментов с большей волатильностью
        /// </summary>
        public double RelativePriceChange 
        { 
            get => СorrectedVolat * (NowPrice / PrevPrice - 1); 
        }

        //(Pt/Pt_1-1) * cv)

        /// <summary>
        /// Циркулирующее предложение
        /// </summary>
        public double CircSupply { set; get; }

        /// <summary>
        /// Капитализация
        /// </summary>
        public double Capitalization
        {
            get => RebalPrice * CircSupply;    
        }

        /// <summary>
        /// Волатильность
        /// </summary>
        public double Volat {  set; get; }

        /// <summary>
        /// Приведенное значение волатильности, относительно средней волатильности всех компонентов индекса
        /// = AvgVolat / Volat
        /// </summary>
        public double СorrectedVolat { set; get; }


    }
}
