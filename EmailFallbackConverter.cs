using System.Globalization;

namespace Group_2
{
    public class EmailFallbackConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var email = value as string;
            if (string.IsNullOrWhiteSpace(email))
                return "Sähköposti ei ole ilmoitettu";
            return email;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => value;
    }
}