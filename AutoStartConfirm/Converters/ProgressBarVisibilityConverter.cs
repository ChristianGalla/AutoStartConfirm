using System;
using System.Globalization;

namespace AutoStartConfirm.Converters
{
    public class ProgressBarVisibilityConverter : ConverterBase /*, IValueConverter */ {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            var runningActionCount = (int)value;
            return runningActionCount > 0 ? "Visible" : "Hidden";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
}
