using AutoStartConfirm.Models;
using Microsoft.UI.Xaml.Data;
using System;
using System.Globalization;

namespace AutoStartConfirm.Converters
{
    public class CanBeConfirmedConverter : ConverterBase
    {

        public override object Convert(object value, Type targetType, object parameter, string language)
        {
            var item = (AutoStartEntry)value;
            var status = item.ConfirmStatus;
            return status == ConfirmStatus.New;
        }

        public override object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
