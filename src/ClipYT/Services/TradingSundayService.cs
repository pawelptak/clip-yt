using ClipYT.Interfaces;

namespace ClipYT.Services
{
    public class TradingSundayService : ITradingSundayService
    {
        public bool? IsTradingSunday()
        {
            var today = DateTime.Now.Date;

            if (today.DayOfWeek != DayOfWeek.Sunday)
            {
                return null;
            }

            return IsTradingSunday(today);
        }

        private static bool IsTradingSunday(DateTime date)
        {
            return IsLastSundayOfMonth(date, 1)
                || IsLastSundayOfMonth(date, 4)
                || IsLastSundayOfMonth(date, 6)
                || IsLastSundayOfMonth(date, 8)
                || date.Date == GetEasterSunday(date.Year).AddDays(-7)
                || IsSundayBeforeChristmas(date);
        }

        private static bool IsLastSundayOfMonth(DateTime date, int month)
        {
            if (date.Month != month)
            {
                return false;
            }

            return date.AddDays(7).Month != month;
        }

        private static DateTime GetEasterSunday(int year)
        {
            var a = year % 19;
            var b = year / 100;
            var c = year % 100;
            var d = b / 4;
            var e = b % 4;
            var f = (b + 8) / 25;
            var g = (b - f + 1) / 3;
            var h = (19 * a + b - d - g + 15) % 30;
            var i = c / 4;
            var k = c % 4;
            var l = (32 + 2 * e + 2 * i - h - k) % 7;
            var m = (a + 11 * h + 22 * l) / 451;
            var month = (h + l - 7 * m + 114) / 31;
            var day = ((h + l - 7 * m + 114) % 31) + 1;

            return new DateTime(year, month, day);
        }

        private static bool IsSundayBeforeChristmas(DateTime date)
        {
            var christmas = new DateTime(date.Year, 12, 25);
            var daysUntilChristmas = (christmas - date.Date).Days;

            return date.DayOfWeek == DayOfWeek.Sunday && daysUntilChristmas is >= 1 and <= 14;
        }
    }
}
