using Microsoft.UI.Xaml.Data;
using System;
using System.Globalization;

namespace AutoStartConfirm.Converters
{
    public class ProgressBarVisibilityConverter : ConverterBase
    {
        public override object Convert(object value, Type targetType, object parameter, string language)
        {
            var runningActionCount = (int)value;
            return runningActionCount > 0 ? "Visible" : "Hidden";
        }

        public override object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
