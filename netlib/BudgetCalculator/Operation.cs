namespace BudgetCalculator
{
    public class Operation
    {
        protected Account m_account;
        protected string m_title;
        protected DateTime m_when;
        protected decimal m_amount;
        private OpTypes m_opType;
        private NewOperation? m_newOperation;

        internal Operation(Account account, string title, DateTime when, decimal amount, OpTypes opType, NewOperation? newOp)
        {
            m_account = account;
            m_title = title;
            m_when = when;
            m_amount = amount;
            m_opType = opType;
            m_newOperation = newOp;
        }

        public Operation Invert()
        {
            return new Operation(m_account, m_title, m_when, -m_amount, m_opType, m_newOperation);
        }

        public Account Account { get => m_account; }

        public virtual string Title { get => m_title; }

        public DateTime When { get => m_when; }

        public decimal Amount { get => m_amount; set => m_amount = value; }

        public void SetTitle(string text)
        {
            m_title = text;
        }

        internal OpTypes OperationType { get => m_opType; }

        internal NewOperation? NewOperation { get => m_newOperation; }

        public class Sorter : IComparer<Operation>
        {
            int IComparer<Operation>.Compare(Operation? x, Operation? y)
            {
                if (x == null) return 1;
                else if (y == null) return -1;

                int c = Comparer<DateTime>.Default.Compare(x.m_when, y.m_when); // sorting first by date when operations are before today
                if (c == 0)
                {
                    int x_optype = GetOpTypeOrderValue(x.m_opType);
                    int y_optype = GetOpTypeOrderValue(y.m_opType);
                    c = Comparer<int>.Default.Compare(x_optype, y_optype);
                    if (c == 0)
                    {
                        return Comparer<string>.Default.Compare(x.m_title, y.m_title);
                    }
                    else
                    {
                        return c;
                    }
                }
                else
                {
                    return c;
                }
            }

            private int GetOpTypeOrderValue(OpTypes opType)
            {
                switch (opType)
                {
                    case OpTypes.Paycheck: return 0;
                    case OpTypes.Transfer: return 1;
                    default: return 2;
                }
            }
        }

    }
}
