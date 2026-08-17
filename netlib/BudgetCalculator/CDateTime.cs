using Python.Runtime;
using System.Globalization;

namespace BudgetCalculator
{
    public static class CDateTime
    {
        private static Func<DateTime>? s_nowProvider;

        public static DateTime Today => (s_nowProvider?.Invoke() ?? DateTime.Now).Date;

        public static DateTime Now => s_nowProvider?.Invoke() ?? DateTime.Now;

        public static DateTime Parse(string text)
        {
            string textTrimmed = text.Trim();

            string[] formats =
            {
                "yyyy-M-d",
                "yyyy-MM-d",
                "yyyy-M-dd",
                "yyyy-MM-dd"
            };

            if (DateTime.TryParseExact(textTrimmed, formats,
                                       CultureInfo.InvariantCulture,
                                       DateTimeStyles.None,
                                       out var dt))
            {
                return dt;
            }

            throw new FormatException($"Invalid date value ('yyyy-MM-dd'): {text}");
        }

        public static DateTime Parse(PyString text)
        {
            string textTrimmed = "";
            using (Py.GIL())
            {
                textTrimmed = text.ToString().Trim();
            }
            return Parse(textTrimmed);
        }

        public static IDisposable Override(DateTime fixedNow)
        {
            Func<DateTime>? previous = s_nowProvider;
            s_nowProvider = () => fixedNow;
            return new Scope(previous);
        }

        public static IDisposable Override(PyString fixedNow)
        {
            string nowText = "";
            using (Py.GIL())
            {
                nowText = fixedNow.ToString().Trim();
            }

            DateTime now = Parse(nowText);
            return Override(now);
        }

        private sealed class Scope : IDisposable
        {
            private readonly Func<DateTime>? m_previous;

            public Scope(Func<DateTime>? previous)
            {
                m_previous = previous;
            }

            public void Dispose()
            {
                s_nowProvider = m_previous;
            }
        }
    }
}
