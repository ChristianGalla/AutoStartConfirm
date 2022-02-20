using AutoStartConfirm.Connectors;
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
    public class CanBeRemovedConverter : IMultiValueConverter
    {
        private readonly IAutoStartService _autoStartService;

        //public CanBeRemovedConverter(IAutoStartService autoStartService)
        //{
        //    _autoStartService = autoStartService;
        //}

        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
			var autoStart = (AutoStartEntry)values[0];
			if (autoStart.CanBeRemoved.HasValue) {
				return autoStart.CanBeRemoved.Value;
			}
			Task.Run(() => {
				_autoStartService.LoadCanBeRemoved(autoStart);
			});
			return false;
		}

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
}
