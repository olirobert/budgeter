namespace BudgetCalculator
{
    public class Account
    {
        public class DayPaiements
        {
            public DateTime Day;
            public List<Operation> Operations = new List<Operation>();

            public List<Operation> PositiveOperations = new List<Operation>();
            public List<Operation> NegativeOperations = new List<Operation>();

            public decimal Balance = 0;
        }

        public string Name = "";
        public decimal EntryBalance;
        public decimal SoftMinium = 0;
        public int ForecastDays = 0;

        internal string VariableName = "";

        internal List<RecurrentOperation> RecurrentOperations = new List<RecurrentOperation>();

        private decimal m_balance;

        private Dictionary<DateTime, DayPaiements> m_days = new Dictionary<DateTime, DayPaiements>();

        private Dictionary<DateTime, (decimal, decimal)> m_forecast = new Dictionary<DateTime, (decimal, decimal)>();

        public decimal EndBalance { get => m_balance; set => m_balance = value; }

        public DayPaiements[] Days { get => m_days.Values.ToArray(); }

        public Dictionary<DateTime, (decimal, decimal)> Forecast { get => m_forecast; }

        internal List<DateTime> Prepare(DateTime beginRecurrentDate, DateTime processUntil, DateTime budgetlastDate)
        {
            DateTime dt = CDateTime.Today;
            while (dt <= processUntil)
            {
                if (!m_days.ContainsKey(dt))
                    m_days.Add(dt, new DayPaiements());
                dt = dt.AddDays(1);
            }

            foreach (RecurrentOperation recOp in RecurrentOperations)
            {
                dt = beginRecurrentDate;
                while (true)
                {
                    (Operation? op, DateTime? next) = recOp.GetNext(dt, processUntil, budgetlastDate, this);
                    if (!next.HasValue || op == null)
                        break;

                    if (op.When <= processUntil)
                    {
                        AddOperation(op);
                    }

                    if (next.Value > processUntil)
                    {
                        break;
                    }

                    dt = next.Value;
                }
            }

            List<DateTime> days = new List<DateTime>();
            List<DateTime> beforeToday = new List<DateTime>();
            foreach (var day in m_days)
            {
                DayPaiements dayPaiements = day.Value;

                if (day.Key >= CDateTime.Today)
                {
                    dayPaiements.Operations.Sort(new Operation.Sorter());
                    days.Add(day.Key);
                }
                else
                {
                    beforeToday.Add(day.Key);
                }
            }
            if (beforeToday.Count > 0)
            {
                DayPaiements dayPaiements = new DayPaiements()
                {
                    Day = DateTime.MinValue
                };
                m_days.Add(dayPaiements.Day, dayPaiements);

                foreach (var d in beforeToday)
                {
                    DayPaiements day = m_days[d];

                    dayPaiements.Operations.AddRange(day.Operations);
                }

                dayPaiements.Operations.Sort(new Operation.Sorter());
                days.Add(dayPaiements.Day);
            }

            return days;
        }

        public void ExecuteAllOperations(List<DateTime> days)
        {
            foreach (DateTime dt in days)
            {
                if (!m_days.TryGetValue(dt, out var day))
                {
                    continue;
                }

                foreach (var p in day.Operations)
                {
                    day.Balance += p.Amount;
                }
            }
        }

        public void CalculateBalance(List<DateTime> days)
        {
            m_balance = EntryBalance;

            foreach (DateTime dt in days)
            {
                if (!m_days.TryGetValue(dt, out var day))
                {
                    day = new DayPaiements()
                    {
                        Day = dt
                    };
                    m_days.Add(dt, day);
                }

                day.Balance += m_balance;

                m_balance = day.Balance;
            }
        }

        public void PopulatePosNegOperations(List<DateTime> days)
        {
            foreach (DateTime dt in days)
            {
                if (!m_days.TryGetValue(dt, out var day))
                {
                    continue;
                }

                day.PositiveOperations.Clear();
                day.NegativeOperations.Clear();

                foreach (var p in day.Operations)
                {
                    if (p.Amount > 0)
                        day.PositiveOperations.Add(p);
                    else
                        day.NegativeOperations.Add(p);
                }

                day.PositiveOperations.Sort(new Operation.Sorter());
                day.NegativeOperations.Sort(new Operation.Sorter());
            }
        }

        public void AddOperation(Operation op)
        {
            if (!m_days.TryGetValue(op.When, out DayPaiements? dayPaiements))
            {
                dayPaiements = new DayPaiements();
                m_days.Add(op.When, dayPaiements);
            }
            dayPaiements.Operations.Add(op);
        }

        public DayPaiements GetDay(DateTime day)
        {
            if (!m_days.TryGetValue(day, out DayPaiements? dayPaiements))
                //return null;
                throw new InvalidOperationException("Invalid day");

            return dayPaiements;
        }

        public void CalculateForecast(List<DateTime> days, DateTime lastDate)
        {
            foreach (DateTime dt in days)
            {
                if (dt < CDateTime.Today && dt != DateTime.MinValue)
                {
                    continue;
                }

                DateTime month = new DateTime(dt.Year, dt.Month, 1);

                DayPaiements day = m_days[dt];

                if (m_forecast.TryGetValue(month, out (decimal, decimal) values))
                {
                    decimal min = Math.Min(values.Item1, day.Balance);
                    decimal max = Math.Max(values.Item2, day.Balance);

                    m_forecast[month] = (min, max);
                }
                else
                {
                    values = (day.Balance, day.Balance);
                    m_forecast.Add(month, values);
                }

                if (dt == lastDate)
                    break;
            }
        }
    }
}
