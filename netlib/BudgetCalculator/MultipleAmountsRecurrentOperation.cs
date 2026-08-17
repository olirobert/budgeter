namespace BudgetCalculator
{
    internal class MultipleAmountsRecurrentOperation : RecurrentOperation
    {
        public class Amount
        {
            protected decimal m_amount;

            public Amount(decimal amount)
            {
                m_amount = amount;
            }

            public virtual (bool, decimal) GetAmount(DateTime dt)
            {
                return (true, m_amount);
            }
        }

        public class YearlyBefore : Amount
        {
            private int m_month;
            private int m_day;

            public YearlyBefore(int month, int day, decimal amount)
                : base(amount)
            {
                m_month = month;
                m_day = day;
            }

            public override (bool, decimal) GetAmount(DateTime dt)
            {
                DateTime beforeDt = new DateTime (dt.Year, m_month, m_day);

                if (dt <= beforeDt)
                    return (true, m_amount);
                else
                    return (false, 0);
            }
        }

        private Amount[] m_amounts;

        public MultipleAmountsRecurrentOperation(Account account, string title, IFreq freq, Amount[] amounts, OpTypes opType)
            : base(account, title, freq, 0, opType)
        {
            m_amounts = amounts;
        }

        public override string ToString()
        {
            return $"'{m_title}' of multiple amounts at {m_freq}";
        }

        protected override decimal GetAmount(DateTime dt)
        {
            foreach (Amount amount in m_amounts)
            {
                (bool used, decimal a) = amount.GetAmount(dt);
                if (used)
                {
                    return a;
                }
            }

            throw new Exception("MultipleAmountsRecurrentOperation no default value");
        }
    }
}
