using System.Globalization;

namespace Group_2
{
    public class SelectedEqualsItemConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
            => values != null && values.Length >= 2 && ReferenceEquals(values[0], values[1]);

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}