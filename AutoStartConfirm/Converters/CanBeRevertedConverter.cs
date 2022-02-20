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
    public class CanBeRevertedConverter : IMultiValueConverter
	{
		private readonly IAutoStartService _autoStartService;

		//public CanBeRevertedConverter(IAutoStartService autoStartService)
		//{
		//	_autoStartService = autoStartService;
		//}

		public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) {
			var autoStart = (AutoStartEntry)values[0];
			switch (autoStart.Change) {
				case Change.Added:
					if (autoStart.CanBeRemoved.HasValue) {
						return autoStart.CanBeRemoved.Value;
					}
					Task.Run(() => {
						_autoStartService.LoadCanBeRemoved(autoStart);
					});
					break;
				case Change.Removed:
					if (autoStart.CanBeAdded.HasValue) {
						return autoStart.CanBeAdded.Value;
					}
					Task.Run(() => {
						_autoStartService.LoadCanBeAdded(autoStart);
					});
					break;
				case Change.Enabled:
					if (autoStart.CanBeDisabled.HasValue) {
						return autoStart.CanBeDisabled.Value;
					}
					Task.Run(() => {
						_autoStartService.LoadCanBeDisabled(autoStart);
					});
					break;
				case Change.Disabled:
					if (autoStart.CanBeEnabled.HasValue) {
						return autoStart.CanBeEnabled.Value;
					}
					Task.Run(() => {
						_autoStartService.LoadCanBeEnabled(autoStart);
					});
					break;
			}
			return false;
		}

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
}
