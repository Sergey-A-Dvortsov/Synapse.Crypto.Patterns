using ScottPlot;
using Synapse.General;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace Synapse.Crypto.Patterns
{
    public class UniTableViewModel : BaseViewModel
    {
        private AppRoot root = AppRoot.GetInstance();

        public UniTableViewModel(string mode) 
        {
            switch (mode)
            {
                case "volatility" :
                    {
                        ItemsView = CollectionViewSource.GetDefaultView(root.VolatilityParams);
                        break;
                    }
                default:
                    break;
            }
            
        }

        private ICollectionView _itemsView;
        public ICollectionView ItemsView 
        { 
            get => _itemsView;
            set 
            {
                _itemsView = value;
                NotifyPropertyChanged();
            }
        }

        private object _selectedItem;
        public object SelectedItem
        {
            get => _selectedItem;
            set
            {
                _selectedItem = value;
                NotifyPropertyChanged();
            }
        }


        public void OnLoaded(object sender, RoutedEventArgs e)
        {
        }

        public void OnUnloaded(object sender, RoutedEventArgs e)
        {
        }

        public void OnClosing(object sender, CancelEventArgs e)
        {
        }

    }
}
