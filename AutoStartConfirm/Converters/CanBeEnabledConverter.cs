using AutoStartConfirm.Models;
using Microsoft.UI.Xaml.Data;
using System;
using System.Globalization;
using System.Threading.Tasks;

namespace AutoStartConfirm.Converters
{
    public class CanBeEnabledConverter : ConverterBase
    {
        public override object Convert(object value, Type targetType, object parameter, string language)
        {
            var autoStart = (AutoStartEntry)value;
            if (autoStart.CanBeEnabled.HasValue)
            {
                return autoStart.CanBeEnabled.Value;
            }
            if (autoStart.CanBeEnabledLoader == null)
            {
                AutoStartService.LoadCanBeEnabled(autoStart);
            }
            return false;
        }

        public override object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
