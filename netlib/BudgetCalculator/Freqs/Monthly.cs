namespace BudgetCalculator.Freqs
{
    internal class Monthly : IFreq
    {
        private int m_when;

        public Monthly(int when)
        {
            // positive value indicate day of the month
            // negative value indicate day from end of month (-1 mean last day of month)
            m_when = when;
        }

        public DateTime GetNext(DateTime next)
        {
            DateTime result;

            if (m_when > 0)
            {
                result = new DateTime(next.Year, next.Month, m_when);

                if (m_when < next.Day)
                {
                    // next month
                    result = result.AddMonths(1);
                }
            }
            else
            {
                DateTime month = new DateTime(next.Year, next.Month, 1);
                while (true)
                {
                    result = month.AddMonths(1).AddDays(m_when);
                    if (result >= next)
                        // found
                        break;
                    else
                        // add a month
                        month = month.AddMonths(1);
                }
            }

            return result;
        }

        public override string ToString()
        {
            if (m_when > 0)
                return $"Monthly at {m_when}";
            else if (m_when == -1)
                return $"Monthly at end of the month";
            else
                return $"Monthly ???";
        }
    }
}
