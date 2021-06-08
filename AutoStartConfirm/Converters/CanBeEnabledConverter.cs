using AutoStartConfirm.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace AutoStartConfirm.Converters {
    class CanBeEnabledConverter : IValueConverter {
		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
			var autoStart = (AutoStartEntry)value;
			if (autoStart.CanBeEnabled.HasValue) {
				return autoStart.CanBeEnabled.Value;
			}
			Task.Run(() => {
				App.GetInstance().AutoStartService.LoadCanBeEnabled(autoStart);
			});
			return false;
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
			throw new NotSupportedException();
		}
	}
}
