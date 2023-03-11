using AutoStartConfirm.Models;
using Microsoft.UI.Xaml.Data;
using System;
using System.Globalization;
using System.Threading.Tasks;

namespace AutoStartConfirm.Converters
{
    public class CanBeRevertedConverter : ConverterBase
    {
        public override object Convert(object value, Type targetType, object parameter, string language)
        {
            var autoStart = (AutoStartEntry)value;
            switch (autoStart.Change)
            {
                case Change.Added:
                    if (autoStart.CanBeRemoved.HasValue)
                    {
                        return autoStart.CanBeRemoved.Value;
                    }
                    if (autoStart.CanBeRemovedLoader == null)
                    {
                        AutoStartService.LoadCanBeRemoved(autoStart);
                    }
                    break;
                case Change.Removed:
                    if (autoStart.CanBeAdded.HasValue)
                    {
                        return autoStart.CanBeAdded.Value;
                    }
                    if (autoStart.CanBeAddedLoader == null)
                    {
                        AutoStartService.LoadCanBeAdded(autoStart);
                    }
                    break;
                case Change.Enabled:
                    if (autoStart.CanBeDisabled.HasValue)
                    {
                        return autoStart.CanBeDisabled.Value;
                    }
                    if (autoStart.CanBeDisabledLoader == null)
                    {
                        AutoStartService.LoadCanBeDisabled(autoStart);
                    }
                    break;
                case Change.Disabled:
                    if (autoStart.CanBeEnabled.HasValue)
                    {
                        return autoStart.CanBeEnabled.Value;
                    }
                    if (autoStart.CanBeEnabledLoader == null)
                    {
                        AutoStartService.LoadCanBeEnabled(autoStart);
                    }
                    break;
            }
            return false;
        }

        public override object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
