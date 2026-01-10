using Synapse.General;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Synapse.Crypto.Patterns
{
    public class DateSpinViewModel : BaseViewModel
    {

        public DateSpinViewModel() 
        {
           MinusTimeCommand = new DelegateCommand(OnMinusTime, CanMinusTime);
           PlusTimeCommand = new DelegateCommand(OnPlusTime, CanPlusTime);
        }

        public DateSpinViewModel(DateTime start, DateTime end, TimeSpan? step = null) : this()
        {
          Step = step ?? TimeSpan.FromDays(1);
          StartTime = start;
          EndTime = end;
        }

        public event Action<DateTime> TimeChanged = delegate { };

        private void OnTimeChanged(DateTime time)
        {
            TimeChanged?.Invoke(time);
        }


        #region properties

        private DateTime _time;
        public DateTime Time
        {
            get => _time;
            set
            {
                if (_time == value) return;
                if (value < StartTime || value > EndTime) return;
                _time = value;
                OnTimeChanged(value);
                NotifyPropertyChanged();
            }
        }

        private DateTime _startTime;
        public DateTime StartTime
        {
            get => _startTime;
            set
            {
                _startTime = value;
                NotifyPropertyChanged();
            }
        }

        private DateTime _endTime;
        public DateTime EndTime
        {
            get => _endTime;
            set
            {
                _endTime = value;
                NotifyPropertyChanged();
            }
        }

        private TimeSpan _step;
        public TimeSpan Step
        {
            get => _step;
            set
            {
                _step = value;
                NotifyPropertyChanged();
            }
        }

        #endregion

        #region commands

        public DelegateCommand MinusTimeCommand { get; private set; }

        private void OnMinusTime(object obj)
        {
            Time = Time - Step;
        }

        private bool CanMinusTime(object obj)
        {
            return StartTime < Time;
        }

        public DelegateCommand PlusTimeCommand { get; private set; }

        private void OnPlusTime(object obj)
        {
            Time = Time + Step;
        }

        private bool CanPlusTime(object obj)
        {
            return EndTime > Time;
        }

        #endregion

    }
}
