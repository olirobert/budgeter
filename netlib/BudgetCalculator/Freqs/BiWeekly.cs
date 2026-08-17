namespace BudgetCalculator.Freqs
{
    internal class BiWeekly : IFreq
    {
        private DateTime m_first;

        public BiWeekly(DateTime first)
        {
            m_first = first;

            // do a preliminary jump until near today
            DateTime dt = CDateTime.Today.AddDays(-1);
            while (m_first < dt)
            {
                m_first = m_first.AddDays(14);
            }
        }

        public DateTime GetNext(DateTime next)
        {
            DateTime result = m_first;

            while (result < next)
            {
                result = result.AddDays(14);
            }

            return result;
        }

        public override string ToString()
        {
            return $"BiWeekly at {m_first.DayOfWeek} (Begin: {m_first:d})";
        }
    }
}
