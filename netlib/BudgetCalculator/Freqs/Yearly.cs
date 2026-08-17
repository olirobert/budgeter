namespace BudgetCalculator.Freqs
{
    internal class Yearly : IFreq
    {
        private DateTime m_first;

        public Yearly(DateTime first)
        {
            m_first = first;

            // do a preliminary jump until near today
            DateTime dt = CDateTime.Today.AddDays(-1);
            while (m_first < dt)
            {
                m_first = m_first.AddYears(1);
            }
        }

        public DateTime GetNext(DateTime next)
        {
            DateTime result = m_first;

            while (result < next)
            {
                result = result.AddYears(1);
            }

            return result;
        }

        public override string ToString()
        {
            return $"Yearly at {m_first.Month} / {m_first.Day} (Begin: {m_first:d})";
        }
    }
}
