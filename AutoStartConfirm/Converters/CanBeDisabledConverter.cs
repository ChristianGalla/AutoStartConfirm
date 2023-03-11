using AutoStartConfirm.Models;
using Microsoft.UI.Xaml.Data;
using System;
using System.Globalization;
using System.Threading.Tasks;

namespace AutoStartConfirm.Converters
{
    public class CanBeDisabledConverter : ConverterBase
    {

        public override object Convert(object value, Type targetType, object parameter, string language)
        {
            var autoStart = (AutoStartEntry)value;
            if (autoStart.CanBeDisabled.HasValue)
            {
                return autoStart.CanBeDisabled.Value;
            }
            if (autoStart.CanBeDisabledLoader == null)
            {
                AutoStartService.LoadCanBeDisabled(autoStart);
            }
            return false;
        }

        public override object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
