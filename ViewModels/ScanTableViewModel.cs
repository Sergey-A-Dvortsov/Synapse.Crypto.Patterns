using Synapse.General;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using Synapse.Crypto.Bybit;

namespace Synapse.Crypto.Patterns
{
    public class ScanTableViewModel : BaseViewModel
    {
        private readonly AppRoot root = AppRoot.GetInstance();

        public ScanTableViewModel()
        {
            OpenChartCommand = new DelegateCommand(OnOpenChart, CanOpenChart);
            OpenSimulatorWndCommand = new DelegateCommand(OnOpenSimulatorWnd, CanOpenSimulatorWnd);
            OpenStepWndCommand = new DelegateCommand(OnOpenStepWnd, CanOpenStepWnd);
            CandleMarkupCommand = new DelegateCommand(OnCandleMarkup, CanCandleMarkup);
            HeikenAshiMarkupCommand = new DelegateCommand(OnHeikenAshiMarkup, CanHeikenAshiMarkup);
            ShowVolatilityCommand = new DelegateCommand(OnShowVolatility, CanShowVolatility);
            ScreenUpdateCommand = new DelegateCommand(OnScreenUpdate, CanScreenUpdate);
        }

        public ICollectionView ItemsView => CollectionViewSource.GetDefaultView(root.ScreenItems);

        #region properties

        private ScreenItem _selectedScreenItem;
        public ScreenItem SelectedScreenItem
        {
            get { return _selectedScreenItem; }
            set
            {
                _selectedScreenItem = value;
                NotifyPropertyChanged();
            }
        }

        public int LoadedBars
        {
            get { return root.LoadedBars; }
            set
            {
                root.LoadedBars = value;
                NotifyPropertyChanged();
            }
        }

        public TimeFrames TimeFrame
        {
            get { return root.TimeFrame; }
            set
            {
                root.TimeFrame = value;
                NotifyPropertyChanged();
            }
        }

        public BreakStyles BreakStyle
        {
            get { return root.BreakDownStyle; }
            set
            {
                root.BreakDownStyle = value;
                NotifyPropertyChanged();
            }
        }

        private bool _highLowLineShow;
        public bool HighLowLineShow
        {
            get { return _highLowLineShow; }
            set
            {
                _highLowLineShow = value;
                NotifyPropertyChanged();
            }
        }

        #endregion

        private void Root_ScreenUpdate()
        {
            ItemsView.Refresh();
        }

        public void OnLoaded(object sender, RoutedEventArgs e)
        {
            root.ScreenUpdate += Root_ScreenUpdate;
            ItemsView.Filter = o => 
            {
                if (o is not ScreenItem item)
                    return false;

                return true;
            };
        }

        public void OnUnloaded(object sender, RoutedEventArgs e)
        {
            root.ScreenUpdate -= Root_ScreenUpdate; ;
        }

        #region commands

        public DelegateCommand OpenChartCommand { private set; get; }

        private void OnOpenChart(object obj)
        {
            var item = obj as ScreenItem;
            var wnd = new ChartWnd(item, HighLowLineShow);
            wnd.Show();
        }

        private bool CanOpenChart(object obj)
        {
            return obj != null;
        }

        public DelegateCommand OpenSimulatorWndCommand { private set; get; }

        private void OnOpenSimulatorWnd(object obj)
        {
            var item = obj as ScreenItem;
            var wnd = new SimulatorWnd(item);
            wnd.Show();
        }

        private bool CanOpenSimulatorWnd(object obj)
        {
            return obj != null;
        }

        public DelegateCommand OpenStepWndCommand { private set; get; }

        private void OnOpenStepWnd(object obj)
        {
            var item = obj as ScreenItem;
            var wnd = new StepViewWnd(item);
            wnd.Show();
        }

        private bool CanOpenStepWnd(object obj)
        {
            return obj != null;
        }

        public DelegateCommand CandleMarkupCommand { private set; get; }

        private void OnCandleMarkup(object obj)
        {
            var item = obj as ScreenItem;
            var wnd = new MarkupWnd(item);
            wnd.Show();
        }

        private bool CanCandleMarkup(object obj)
        {
            return obj != null;
        }

        public DelegateCommand HeikenAshiMarkupCommand { private set; get; }

        private void OnHeikenAshiMarkup(object obj)
        {
            var item = obj as ScreenItem;
            var wnd = new MarkupWnd(item);
            wnd.Show();
        }

        private bool CanHeikenAshiMarkup(object obj)
        {
            return obj != null;
        }

        public DelegateCommand ShowVolatilityCommand { private set; get; }

        private void OnShowVolatility(object obj)
        {
            var wnd = new UniTableWnd("volatility");
            wnd.Show();
        }

        private bool CanShowVolatility(object obj)
        {
            return true;
        }

        public DelegateCommand ScreenUpdateCommand { private set; get; }

        private async void OnScreenUpdate(object obj)
        {
            //await root.LoadCandlesFromBurseAndUpdateStorage(false);
            root.UpdateScanTable(false);
        }

        private bool CanScreenUpdate(object obj)
        {
            return true;
        }

        #endregion

    }
}
