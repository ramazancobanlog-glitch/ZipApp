using System.Globalization;

namespace login.Helpers
{
    public class TurkishLiraFormatting
    {
        public static readonly CultureInfo TRY = new CultureInfo("tr-TR");
        
        static TurkishLiraFormatting()
        {
            TRY.NumberFormat.CurrencySymbol = "₺";
            TRY.NumberFormat.CurrencyDecimalSeparator = ",";
            TRY.NumberFormat.CurrencyGroupSeparator = ".";
        }
        
        public static string Format(decimal value)
        {
            return string.Format(TRY, "{0:C}", value);
        }
    }
}