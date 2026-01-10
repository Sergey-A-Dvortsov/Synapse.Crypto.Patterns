using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace Synapse.Crypto.Patterns
{
    /// <summary>
    /// Логика взаимодействия для StepViewWnd.xaml
    /// </summary>
    public partial class StepViewWnd : Window
    {
        public StepViewWnd(ScreenItem item)
        {
            InitializeComponent();
            DataContext = new StepViewModel(Plot, item);
        }
    }
}
