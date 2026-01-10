using System;
using System.Windows.Data;
using System.Globalization;
using System.Windows.Media;
using System.Windows;

namespace Synapse.Crypto.Patterns
{
    // Copyright(c) [2026], [Sergey Dvortsov]
    /// <summary>
    /// Возвращает цвет в зависимости от знака числа 
    /// </summary>
    public class SignColorConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {

                if(value == null)
                    return DependencyProperty.UnsetValue;

                var result = double.TryParse(value.ToString(), out double number);

                if (result && number < 0)
                    return Brushes.Red;
                    
            }
            catch (Exception)
            {
                throw;
            }

            return DependencyProperty.UnsetValue;

        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }

    }

    public class PositionSignColorConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {

                if (value == null)
                    return DependencyProperty.UnsetValue;

                var result = double.TryParse(value.ToString(), out double number);

                if (result)
                {
                    if(number < 0)
                        return Brushes.DarkRed;
                    else if (number > 0)
                        return Brushes.DarkGreen;
                }
            }
            catch (Exception)
            {
                throw;
            }

            return DependencyProperty.UnsetValue;

        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }

    }
}
