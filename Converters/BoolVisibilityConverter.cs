using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace Synapse.Crypto.Patterns
{
    public class BoolVisibilityConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if ((bool)value)
                return parameter == null ? Visibility.Visible : Visibility.Collapsed;
            else
                return parameter == null ? Visibility.Collapsed : Visibility.Visible;

        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if ((Visibility)value == Visibility.Visible)
            {
                if (parameter == null) return true;
            }
            else if ((Visibility)value == Visibility.Collapsed)
            {
                if (parameter != null) return true;
            }

            return false;

        }

    }
}
