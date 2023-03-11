using AutoStartConfirm.Models;
using Microsoft.UI.Xaml.Data;
using System;
using System.Globalization;
using System.Threading.Tasks;

namespace AutoStartConfirm.Converters
{
    public class CanBeAddedConverter : ConverterBase
    {

        public override object Convert(object value, Type targetType, object parameter, string language)
        {
            var autoStart = (AutoStartEntry)value;
            if (autoStart.CanBeAdded.HasValue)
            {
                return autoStart.CanBeAdded.Value;
            }
            if (autoStart.CanBeAddedLoader == null)
            {
                AutoStartService.LoadCanBeAdded(autoStart);
            }
            return false;
        }

        public override object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
