namespace BudgetCalculator
{
    internal class RecurrentTransferOperation : RecurrentOperation
    {
        protected Account m_otherAccount;

        public Account From => m_account;
        public Account To => m_otherAccount;

        public RecurrentTransferOperation(Account from, Account to, string title, IFreq freq, decimal amount)
            : base(from, title, freq, amount, OpTypes.Transfer)
        {
            m_otherAccount = to;
        }

        protected override Operation GetOperation(DateTime dt, NewOperation? newOp)
        {
            return new TransferOperation(m_account, m_otherAccount, m_title, dt, m_amount, newOp);
        }
    }
}
