using AutoStartConfirm.AutoStarts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace AutoStartConfirm.Converters {
    class CanBeConfirmedConverter : IValueConverter {
		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
			var status = (ConfirmStatus)value;
			return status == ConfirmStatus.New;
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
			var canConfirm = (bool)value;
			if (canConfirm) {
				return ConfirmStatus.New;
			} else {
				return ConfirmStatus.Confirmed;
			}
		}
	}
}
