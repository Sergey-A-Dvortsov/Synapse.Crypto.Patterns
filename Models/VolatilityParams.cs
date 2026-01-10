using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Synapse.Crypto.Patterns
{
    // Калькуляция параметров волатильности инструмента
    // 1. Используем данные за последний год
    // 2. В качестве расчетной единицы, берем относительный (%) размах (high - low) дневной свечи
    // 3. Вычисляем параметры описательной статистики: среднюю, максимум, минимум, стандартное отклонение 

    /// <summary>
    /// Параметры волатильности инструмента
    /// </summary>
    public struct VolatilityParams : INotifyPropertyChanged
    {
        private string _symbol;
        public string Symbol 
        { 
            get => _symbol;
            set 
            { 
                _symbol = value; 
                NotifyPropertyChanged();
            } 
        }

        private double _average;
        public double Average
        {
            get => _average;
            set
            {
                _average = value;
                NotifyPropertyChanged();
            }
        }

        private double _max;
        public double Max
        {
            get => _max;
            set
            {
                _max = value;
                NotifyPropertyChanged();
            }
        }

        private double _min;
        public double Min
        {
            get => _min;
            set
            {
                _min = value;
                NotifyPropertyChanged();
            }
        }

        private double _sd;
        public double SD
        {
            get => _sd;
            set
            {
                _sd = value;
                NotifyPropertyChanged();
            }
        }


        public override string ToString()
        {
            return $"{Symbol};{Average};{Max};{Min};{SD}"; 
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