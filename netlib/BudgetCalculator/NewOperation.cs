namespace BudgetCalculator
{
    internal class NewOperation
    {
        public DateTime When;
        public string Title = "";
        public Account[] Accounts = [];
        public OpTypes OpType;
        public Operation? Source = null;

        public virtual string AmountAsOperationParameter
        {
            get
            {
                if (Source == null)
                    throw new InvalidOperationException("NewOperation not properly setup");
                return $"'{Math.Abs(Source.Amount)}'";
            }
        }
    }

    internal enum OpTypes
    {
        Payment,
        Paycheck,
        Transfer,
    }
}
