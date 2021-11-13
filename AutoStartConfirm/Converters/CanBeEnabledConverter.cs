﻿using AutoStartConfirm.Connectors;
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
    public class CanBeEnabledConverter : ConverterBase, IMultiValueConverter {

		public object Convert(object[] value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
			var autoStart = (AutoStartEntry)value[0];
            if (autoStart.CanBeEnabled.HasValue) {
				return autoStart.CanBeEnabled.Value;
			}
			Task.Run(() => {
				AutoStartService.LoadCanBeEnabled(autoStart);
			});
			return false;
		}

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
}
