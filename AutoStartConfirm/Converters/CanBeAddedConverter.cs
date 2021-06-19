using AutoStartConfirm.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace AutoStartConfirm.Converters {
    class CanBeAddedConverter : ConverterBase, IValueConverter {

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
            var autoStart = (AutoStartEntry)value;
            if (autoStart.CanBeAdded.HasValue) {
                return autoStart.CanBeAdded.Value;
            }
            Task.Run(() => {
                AutoStartService.LoadCanBeAdded(autoStart);
            });
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
            throw new NotSupportedException();
        }
    }
}
