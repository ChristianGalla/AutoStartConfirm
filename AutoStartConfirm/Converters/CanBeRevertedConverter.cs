using AutoStartConfirm.Models;
using System;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows.Data;

namespace AutoStartConfirm.Converters
{
    public class CanBeRevertedConverter : ConverterBase, IMultiValueConverter {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) {
			var autoStart = (AutoStartEntry)values[0];
			switch (autoStart.Change) {
				case Change.Added:
					if (autoStart.CanBeRemoved.HasValue) {
						return autoStart.CanBeRemoved.Value;
					}
					Task.Run(() => {
						AutoStartService.LoadCanBeRemoved(autoStart);
					});
					break;
				case Change.Removed:
					if (autoStart.CanBeAdded.HasValue) {
						return autoStart.CanBeAdded.Value;
					}
					Task.Run(() => {
						AutoStartService.LoadCanBeAdded(autoStart);
					});
					break;
				case Change.Enabled:
					if (autoStart.CanBeDisabled.HasValue) {
						return autoStart.CanBeDisabled.Value;
					}
					Task.Run(() => {
						AutoStartService.LoadCanBeDisabled(autoStart);
					});
					break;
				case Change.Disabled:
					if (autoStart.CanBeEnabled.HasValue) {
						return autoStart.CanBeEnabled.Value;
					}
					Task.Run(() => {
						AutoStartService.LoadCanBeEnabled(autoStart);
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
