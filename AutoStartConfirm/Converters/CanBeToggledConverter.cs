using AutoStartConfirm.Models;
using Microsoft.UI.Xaml.Data;
using System;
using System.Globalization;
using System.Threading.Tasks;

namespace AutoStartConfirm.Converters
{
    public class CanBeToggledConverter : ConverterBase
    {
        public override object Convert(object value, Type targetType, object parameter, string language)
        {
            var autoStart = (AutoStartEntry)value;
            if (autoStart.IsEnabled.Value)
            {
                if (!autoStart.CanBeDisabled.HasValue)
                {
                    AutoStartService.LoadCanBeDisabled(autoStart);
                }
                else
                {
                    return autoStart.CanBeDisabled.Value;
                }
            }
            else
            {
                if (!autoStart.CanBeEnabled.HasValue)
                {
                    AutoStartService.LoadCanBeEnabled(autoStart);
                }
                else
                {
                    return autoStart.CanBeEnabled.Value;
                }
            }
            return false;
        }

        public override object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
