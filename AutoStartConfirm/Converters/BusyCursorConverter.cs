using AutoStartConfirm.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace AutoStartConfirm.Converters {
    public class BusyCursorConverter : ConverterBase, IValueConverter {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            var runningActionCount = (int)value;
            return runningActionCount > 0 ? "AppStarting" : "Arrow";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
}
