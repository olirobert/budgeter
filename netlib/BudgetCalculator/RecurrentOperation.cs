namespace BudgetCalculator
{
    internal class RecurrentOperation
    {
        protected Account m_account;
        protected string m_title;
        protected IFreq m_freq;
        protected decimal m_amount;
        protected OpTypes m_opType;

        public RecurrentOperation(Account account, string title, IFreq freq, decimal amount, OpTypes opType)
        {
            m_account = account;
            m_title = title;
            m_freq = freq;
            m_amount = amount;
            m_opType = opType;
        }

        public (Operation?, DateTime?) GetNext(DateTime nextDate, DateTime maxDate, DateTime newOpMaxDate, Account owner, params Account[] otherOwners)
        {
            DateTime dt = m_freq.GetNext(nextDate);
            if (dt > maxDate)
                return (null, null);

            List<Account> owners = new List<Account> (1 + otherOwners.Length);
            owners.Add(owner);
            owners.AddRange(otherOwners);

            NewOperation? newOp = null;
            if (dt <= newOpMaxDate)
                newOp = GetNewOperation(dt, owners.ToArray());

            Operation op = GetOperation(dt, newOp);
            if (dt <= newOpMaxDate && newOp != null)
                newOp.Source = op;

            return (op, dt.AddDays(1));
        }

        public override string ToString()
        {
            return $"'{m_title}' of {m_amount} at {m_freq}";
        }

        protected virtual decimal GetAmount(DateTime dt)
        {
            return m_amount;
        }

        protected virtual Operation GetOperation(DateTime dt, NewOperation? newOp)
        {
            decimal amount = GetAmount(dt);

            return new Operation(m_account, m_title, dt, amount, m_opType, newOp);
        }

        protected virtual NewOperation GetNewOperation(DateTime dt, Account[] owners)
        {
            return new NewOperation()
            {
                Title = m_title,
                When = dt,
                Accounts = owners,
                OpType = m_opType,
            };
        }
    }
}
