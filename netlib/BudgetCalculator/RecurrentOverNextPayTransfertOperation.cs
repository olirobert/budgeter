namespace BudgetCalculator
{
    internal class RecurrentOverNextPayTransfertOperation : RecurrentTransferOperation
    {
        private decimal m_maxBalance;
        private decimal m_round;

        private string m_otherTitle;

        public RecurrentOverNextPayTransfertOperation(Account from, Account to, string titleA, string titleB, IFreq freq, decimal maxBalance, decimal round)
                : base(from, to, titleA, freq, 0)
        {
            m_maxBalance = maxBalance;
            m_round = round;

            m_otherTitle = titleB;
        }

        protected override Operation GetOperation(DateTime dt, NewOperation? newOp)
        {
            return new OverNextPayOperation(m_account, m_otherAccount, m_title, m_otherTitle, dt, m_maxBalance, m_round, newOp);
        }

        protected override NewOperation GetNewOperation(DateTime dt, Account[] owners)
        {
            return new OverNextPayNewOperation()
            {
                Title = m_title,
                OtherTitle = m_otherTitle,
                When = dt,
                Accounts = owners,
                OpType = m_opType,
            };
        }

    }
}
