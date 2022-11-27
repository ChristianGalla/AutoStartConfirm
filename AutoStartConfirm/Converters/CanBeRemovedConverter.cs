using AutoStartConfirm.Models;
using System;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows.Data;

namespace AutoStartConfirm.Converters
{
    public class CanBeRemovedConverter : ConverterBase, IMultiValueConverter {
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
			var autoStart = (AutoStartEntry)values[0];
			if (autoStart.CanBeRemoved.HasValue) {
				return autoStart.CanBeRemoved.Value;
			}
			Task.Run(() => {
				AutoStartService.LoadCanBeRemoved(autoStart);
			});
			return false;
		}

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
}
