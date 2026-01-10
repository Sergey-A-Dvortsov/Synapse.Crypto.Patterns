using Synapse.General;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Synapse.Crypto.Patterns
{
    public class MainViewmodel : BaseViewModel
    {

        private AppRoot root;

        public MainViewmodel()
        {
            root = AppRoot.GetInstance();
            ScanTableViewModel = new ScanTableViewModel();
            ExitCommand = new DelegateCommand(OnExit, CanExit);
            OpenChartCommand = new DelegateCommand(OnOpenChart, CanOpenChart);
            OpenTestChartCommand = new DelegateCommand(OnOpenTestChart, CanOpenTestChart);
            FullCalculateCommand = new DelegateCommand(OnFullCalculate, CanFullCalculate);
            LoadCryptoIndexCommand = new DelegateCommand(OnLoadCryptoIndex, CanLoadCryptoIndex);
        }

        public ScanTableViewModel ScanTableViewModel { get; private set; }


        private string _statusMessage;
        public string StatusMessage
        {
            get { return _statusMessage; }
            set
            {
                _statusMessage = value;
                NotifyPropertyChanged();
            }
        }

        private bool _isClose = false;
        public bool IsClose
        {
            get { return _isClose; }
            set
            {
                _isClose = value;
                NotifyPropertyChanged();
            }
        }

        #region command
     
        public DelegateCommand ExitCommand { private set; get; }

        private void OnExit(object obj)
        {

        }

        private bool CanExit(object obj)
        {
            return true;
        }

        public DelegateCommand OpenChartCommand { private set; get; }

        private void OnOpenChart(object obj)
        {
            var wnd = new ChartWnd();
            wnd.Show();
        }

        private bool CanOpenChart(object obj)
        {
            return true;
        }

        public DelegateCommand OpenTestChartCommand { private set; get; }

        private void OnOpenTestChart(object obj)
        {
            var wnd = new TestPlotWnd();
            wnd.Show();
        }

        private bool CanOpenTestChart(object obj)
        {
            return true;
        }


        public DelegateCommand FullCalculateCommand { private set; get; }

        private async void OnFullCalculate(object obj)
        {
            //var indexes = await root.FullCalculateCryptoIndex();
        }

        private bool CanFullCalculate(object obj)
        {
            return true;
        }

        public DelegateCommand LoadCryptoIndexCommand { private set; get; }

        private void OnLoadCryptoIndex(object obj)
        {
            //var indexes = root.LoadCryptoIndex();
            //root.Indexes = indexes;
        }

        private bool CanLoadCryptoIndex(object obj)
        {
            return true;
        }

        #endregion command

        public void OnLoaded(object sender, RoutedEventArgs e)
        {
            root.NewStatusMessage += NewStatusMessageDone;
            root.InitAsync();
            //NotifyPropertyChanged("Accname");
        }

        public void OnUnloaded(object sender, RoutedEventArgs e)
        {
            root.NewStatusMessage -= NewStatusMessageDone;
        }

        public void OnClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            root.Shutdown().Wait();
            App.Current.Shutdown();
        }

        private void NewStatusMessageDone(string message)
        {
            StatusMessage = message;
        }

    }
}
