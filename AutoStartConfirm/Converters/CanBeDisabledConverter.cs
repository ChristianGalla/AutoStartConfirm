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
    public class CanBeDisabledConverter : IMultiValueConverter
    {
        private readonly IAutoStartService _autoStartService;

        //public CanBeDisabledConverter(IAutoStartService autoStartService)
        //{
        //    _autoStartService = autoStartService;
        //}

        public object Convert(object[] value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
			var autoStart = (AutoStartEntry)value[0];
			if (autoStart.CanBeDisabled.HasValue) {
				return autoStart.CanBeDisabled.Value;
			}
			Task.Run(() => {
				_autoStartService.LoadCanBeDisabled(autoStart);
			});
			return false;
		}

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
}
