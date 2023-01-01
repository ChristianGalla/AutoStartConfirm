using Microsoft.UI.Xaml.Data;
using System;
using System.Globalization;

namespace AutoStartConfirm.Converters
{
    public class BusyCursorConverter : ConverterBase, IValueConverter {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            var runningActionCount = (int)value;
            return runningActionCount > 0 ? "AppStarting" : "Arrow";
        }

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
