namespace BudgetCalculator
{
    internal class TransferOperation : Operation
    {
        public Operation? InvertedOperation = null;

        protected Account m_otherAccount;

        public TransferOperation(Account accountFrom, Account accountTo, string title, DateTime when, decimal amount, NewOperation? newOp)
            : base(accountFrom, title, when, amount, OpTypes.Transfer, newOp)
        {
            m_otherAccount = accountTo;
        }
    }
}
