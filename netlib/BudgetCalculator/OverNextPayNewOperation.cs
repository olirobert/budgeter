namespace BudgetCalculator
{
    internal class OverNextPayNewOperation : NewOperation
    {
        public string OtherTitle = "";
    
        public override string AmountAsOperationParameter
        {
            get
            {
                OverNextPayOperation op = Source as OverNextPayOperation ?? throw new Exception("Isn't a OverNextPayOperation");

                string str = $"OverUntilNextPay('{op.MaxBalance}'";

                if (op.Round > 0)
                    str += $", round='{op.Round}'";

                str += ")";
                return str;
            }
        }
    }
}
