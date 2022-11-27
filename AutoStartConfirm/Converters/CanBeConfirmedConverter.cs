using AutoStartConfirm.Models;
using System;
using System.Globalization;
using System.Windows.Data;

namespace AutoStartConfirm.Converters
{
    public class CanBeConfirmedConverter : ConverterBase, IMultiValueConverter {

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) {
			var item = (AutoStartEntry)values[0];
			var status = item.ConfirmStatus;
			return status == ConfirmStatus.New;
		}

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) {
			throw new NotImplementedException();
		}
	}
}
