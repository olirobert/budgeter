namespace BudgetCalculator
{
    internal class OverNextPayOperation : TransferOperation
    {
        private decimal m_maxBalance;
        private decimal m_round;

        private string m_otherTitle;

        public decimal MaxBalance { get => m_maxBalance; }
        public decimal Round { get => m_round; }

        public OverNextPayOperation(Account accountFrom, Account accountTo, string titleA, string titleB, DateTime when, decimal maxBalance, decimal round, NewOperation? newOp)
            : base(accountFrom, accountTo, titleA, when, 0, newOp)
        {
            m_maxBalance = maxBalance;
            m_round = round;

            m_otherTitle = titleB;
        }

        public override string Title => (m_amount >= 0) ? m_otherTitle : m_title;

        public void UpdateAmount(decimal minBalance, DateTime processUntil)
        {
            if (InvertedOperation == null)
                throw new Exception("OverNextPayOperation not properly setup");
            if (m_round == 0)
                throw new Exception("Don't support no rounding for OverNextPayOperation");

            decimal delta = 0;

            if (minBalance + m_round >= m_maxBalance)
            {
                delta = minBalance - m_maxBalance;

                decimal rounding = delta % m_round;
                delta -= rounding;
            }
            else if (m_maxBalance - m_round >= minBalance)
            {
                delta = m_maxBalance - minBalance;

                decimal rounding = delta % m_round;
                delta = -(delta - rounding + m_round);
            }

            if (delta == 0)
                return;

            m_amount -= delta;
            InvertedOperation.Amount += delta;

            if (delta < 0)
            {
                InvertedOperation.SetTitle(m_otherTitle);
            }

            Account accountFrom = m_account;
            Account accountTo = m_otherAccount;

            accountFrom.EndBalance -= delta;
            accountTo.EndBalance += delta;

            DateTime dt = m_when;
            while (dt <= processUntil)
            {
                var dayOps = accountFrom.GetDay(dt);
                dayOps.Balance -= delta;
 
                dayOps = accountTo.GetDay(dt);
                dayOps.Balance += delta;

                dt = dt.AddDays(1);
            }
        }
    }
}
