using Python.Runtime;
using System.Globalization;

namespace BudgetCalculator
{
    public class BudgetProcessor
    {
        static BudgetProcessor()
        {
            PythonEngine.Initialize();
        }

        private List<Account> m_accounts = new List<Account>();
        private List<IFreq> m_freqs = new List<IFreq>();
        private List<RecurrentOperation> m_recurrentOperation = new List<RecurrentOperation>();
        private List<DateTime> m_time = new List<DateTime>();
        private EndMarker? m_endMarker = null;
        private DateTime m_currentDayOperations = DateTime.MinValue;

        private int m_paychecksCount = 0;
        private List<IFreq> m_recurrentPaychecks = new List<IFreq>();
        private List<DateTime> m_paychecks = new List<DateTime>();

        private FileManipulator m_historyFile;

        private List<RecurrentTransferOperation> m_recurentTransferOperations = new List<RecurrentTransferOperation>();

        public BudgetProcessor(PyString historyFile)
        {
            string historyFilepath;

            using (Py.GIL())
            {
                historyFilepath = historyFile.ToString();
            }

            m_historyFile = new FileManipulator(historyFilepath);
        }

        public void SetProcessingDaysCount(PyInt count)
        {
            using (Py.GIL())
            {
                int countDays = count.ToInt32();
                if (countDays < 0)
                {
                    m_paychecksCount = -countDays;
                }
                else
                {
                    throw new Exception("SetProcessingDaysCount without AfterPaychecks ins't supported");
                }
            }
        }

        public void Update(PyString outputHistoryFile, PyString outputBudgetFile)
        {
            if (m_endMarker == null)
            {
                m_endMarker = new EndMarker(DateTime.MinValue, m_historyFile.RegisterLastLine());
            }

            DateTime beginRecurrentDate = m_endMarker.BeginRecurrentDate.AddDays(1);

            if (beginRecurrentDate < CDateTime.Today)
                beginRecurrentDate = CDateTime.Today;

            if (m_paychecksCount == 0)
                throw new Exception("Need at least one AfterPaychecks");

            DateTime budgetLastDate = GetLastDateAfterPaycheck(beginRecurrentDate, m_paychecksCount, false);

            DateTime processUntil = budgetLastDate;

            List<(Account, DateTime)> forecastAccounts = new List<(Account, DateTime)>();
            foreach (Account account in m_accounts)
            {
                if (account.ForecastDays == 0)
                    continue;

                DateTime lastProcessingDate = GetLastDateAfterPaycheck(beginRecurrentDate, -account.ForecastDays, true);

                forecastAccounts.Add((account, lastProcessingDate));

                if (lastProcessingDate > processUntil)
                    processUntil = lastProcessingDate;
            }

            List <DateTime> days = new List<DateTime>();

            foreach (RecurrentTransferOperation transferOperation in m_recurentTransferOperations)
            {
                DateTime dt = beginRecurrentDate;
                while (true)
                {
                    (Operation? op, DateTime? next) = transferOperation.GetNext(dt, processUntil, budgetLastDate, transferOperation.From, transferOperation.To);
                    if (!next.HasValue || op == null)
                        break;

                    TransferOperation trfOp = op as TransferOperation ?? throw new Exception("Not a TransferOperation");

                    if (op.When <= processUntil)
                    {
                        AddTransferOperationToAccounts(transferOperation.From, transferOperation.To, trfOp);
                    }

                    if (next.Value > processUntil)
                    {
                        break;
                    }

                    dt = next.Value;
                }
            }

            foreach (Account account in m_accounts)
            {
                var accountDays = account.Prepare(beginRecurrentDate, processUntil, budgetLastDate);

                foreach (DateTime d in accountDays)
                {
                    if (!days.Contains(d))
                        days.Add(d);
                }
            }

            days.Sort();

            foreach (Account account in m_accounts)
            {
                account.ExecuteAllOperations(days);
            }

            foreach (Account account in m_accounts)
            {
                account.CalculateBalance(days);
            }

            ApplyTransferOverNextPayOperations(days, processUntil);

            // calculate forecast
            foreach ((Account account, DateTime lastDate) in forecastAccounts)
            {
                account.CalculateForecast(days, lastDate);
            }

            // create budget days array
            List<DateTime> budgetDays = new List<DateTime>(days.ToArray());
            {
                int index = budgetDays.IndexOf(budgetLastDate);
                if (index != budgetDays.Count - 1)
                    budgetDays.RemoveRange(index + 1, budgetDays.Count - index - 1);
            }

            // populate positive and negative operations for display purpose
            foreach (Account account in m_accounts)
            {
                account.PopulatePosNegOperations(budgetDays);
            }

            string ouputHistoryFilePath;
            string outputFilePath;
            using (Py.GIL())
            {
                ouputHistoryFilePath = outputHistoryFile.ToString();
                outputFilePath = outputBudgetFile.ToString();
            }

            string outputDir = Path.GetDirectoryName(outputFilePath) ?? "";
            string timestampFile = Path.Combine(outputDir, "lastupdate.txt");

            // save html
            HtmlGenerator.Save(outputFilePath, m_accounts, budgetDays, forecastAccounts);
            File.WriteAllText(timestampFile, DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToLongTimeString());

            // update files;
            // end markers
            m_endMarker.UpdateDateInFile(budgetLastDate);

            // new operations
            InsertNewOperations(days);

            // save updated history file
            m_historyFile.Save(ouputHistoryFilePath);
        }

        public PyInt AddAccount(PyString name)
        {
            Account account = new Account();
            int index = m_accounts.AddIndex(account);

            using (Py.GIL())
            {
                account.Name = name.ToString();

                return new PyInt(index);
            }
        }

        public void MarkEnd(PyString dt, PyString file, PyInt lineno)
        {
            int fileLineNo;
            string fileName;
            using (Py.GIL())
            {
                fileLineNo = lineno.ToInt32();
                fileName = file.ToString();
            }

            DateTime date = CDateTime.Parse(dt);

            if (fileName.CompareTo(m_historyFile.Path, StringComparison.InvariantCultureIgnoreCase) != 0)
                throw new Exception("MarkEnd must only set in file set as 'history' file");

            FileManipulator.Line linePointer = m_historyFile.RegisterLine(fileLineNo - 1);

            m_endMarker = new EndMarker(date, linePointer);
        }

        public void MarkDay(PyString dt)
        {
            DateTime date = CDateTime.Parse(dt);

            m_time.AddIndex(date);

            m_currentDayOperations = date;
        }

        public void DayCompleted()
        {
            m_currentDayOperations = DateTime.MinValue;
        }


        // accounts

        public void SetAccountVariableName(PyInt accountHandle, PyString name)
        {
            using (Py.GIL())
            {
                int index = accountHandle.ToInt32();

                m_accounts[index].VariableName = name.ToString();
            }
        }

        public void SetAccountSoftMinimum(PyInt accountHandle, PyString softMin)
        {
            using (Py.GIL())
            {
                int index = accountHandle.ToInt32();

                m_accounts[index].SoftMinium = ParseDecimal(softMin);
            }
        }

        public void SetAccountEntryBalance(PyInt accountHandle, PyString balance)
        {
            using (Py.GIL())
            {
                int index = accountHandle.ToInt32();

                m_accounts[index].EntryBalance = ParseDecimal(balance);
            }
        }

        public void SetAccountForecastDaysCount(PyInt accountHandle, PyInt count)
        {
            using (Py.GIL())
            {
                int index = accountHandle.ToInt32();

                int countVal = count.ToInt32();

                if (countVal >= 0)
                    throw new Exception("Account.Forecast without AfterPaychecks ins't supported");

                m_accounts[index].ForecastDays = countVal;
            }
        }
        
        public void AddRecurrentBillOnAccount(PyInt accountHandle, PyString title, PyInt freqHandle, PyString amount)
        {
            using (Py.GIL())
            {
                int accountIndex = accountHandle.ToInt32();
                int freqIndex = freqHandle.ToInt32();

                Account account = m_accounts[accountIndex];
                IFreq freq = m_freqs[freqIndex];

                account.RecurrentOperations.Add(new RecurrentOperation(account, title.ToString(), freq, -ParseDecimal(amount), OpTypes.Payment));
            }
        }

        public void AddRecurrentPayOnAccount(PyInt accountHandle, PyString title, PyInt freqHandle, PyObject amount)
        {
            IFreq? freq = null;

            using (Py.GIL())
            {
                int accountIndex = accountHandle.ToInt32();
                int freqIndex = freqHandle.ToInt32();

                Account account = m_accounts[accountIndex];
                freq = m_freqs[freqIndex];

                if (amount.GetPythonType().Name == "MultipleAmounts")
                {
                    PyList args = amount.GetAttr("amounts").As<PyList>();

                    int count = (int)args.Length();

                    List<MultipleAmountsRecurrentOperation.Amount> amounts = new List<MultipleAmountsRecurrentOperation.Amount>(count);
                    for (int i = 0; i < count; i++)
                    {
                        PyObject arg = args.GetItem(i);

                        decimal amountDec = ParseDecimal(arg.GetAttr("amount").As<PyString>());

                        if (arg.GetPythonType().Name == "Before")
                        {
                            PyObject when = arg.GetAttr("when");
                            if (when.GetPythonType().Name == "DayOfYear")
                            {
                                PyString whenStr = when.GetAttr("when").As<PyString>();

                                (int month, int day) = ParseMonthDay(whenStr);

                                amounts.Add(new MultipleAmountsRecurrentOperation.YearlyBefore(month, day, amountDec));
                            }
                            else
                            {
                                throw new Exception("Before argument not supported");
                            }
                        }
                        else if (arg.GetPythonType().Name == "Then")
                        {
                            amounts.Add(new MultipleAmountsRecurrentOperation.Amount(amountDec));
                        }
                        else
                        {
                            throw new Exception("MultipleAmounts argument not supported");
                        }
                    }

                    account.RecurrentOperations.Add(new MultipleAmountsRecurrentOperation(account, title.ToString(), freq, amounts.ToArray(), OpTypes.Paycheck));
                }
                else
                {
                    PyString amountStr = amount.As<PyString>();

                    account.RecurrentOperations.Add(new RecurrentOperation(account, title.ToString(), freq, ParseDecimal(amountStr), OpTypes.Paycheck));
                }
            }

            if (m_paychecksCount > 0)
            {
                m_recurrentPaychecks.Add(freq);
            }
        }

        public void AddRecurrentTransferOnAccount(PyObject title, PyInt accountFromHandle, PyInt accountToHandle, PyInt freqHandle, PyObject amount)
        {
            using (Py.GIL())
            {
                int accountFromIndex = accountFromHandle.ToInt32();
                int accountToIndex = accountToHandle.ToInt32();
                int freqIndex = freqHandle.ToInt32();

                Account accountFrom = m_accounts[accountFromIndex];
                Account accountTo = m_accounts[accountToIndex];
                IFreq freq = m_freqs[freqIndex];

                if (amount.GetPythonType().Name == "OverUntilNextPay")
                {
                    PyString maxBalanceStr = amount.GetAttr("maxBalance").As<PyString>();
                    PyString roundStr = amount.GetAttr("round").As<PyString>();

                    string strTitleA;
                    string strTitleB;
                    if (title.GetPythonType().Name == "DualTitle")
                    {
                        strTitleA = title.GetAttr("posTitle").As<PyString>().ToString();
                        strTitleB = title.GetAttr("negTitle").As<PyString>().ToString();
                    }
                    else
                    {
                        strTitleA = title.As<PyString>().ToString();
                        strTitleB = strTitleA;
                    }

                    m_recurentTransferOperations.Add(new RecurrentOverNextPayTransfertOperation(accountFrom, accountTo, strTitleA, strTitleB, freq, ParseDecimal(maxBalanceStr), ParseDecimal(roundStr)));
                }
                else
                {
                    PyString amountStr = amount.As<PyString>();
                    string titleText = title.ToString() ?? "[UNKNOWN TITLE]";
                    m_recurentTransferOperations.Add(new RecurrentTransferOperation(accountFrom, accountTo, titleText, freq, -ParseDecimal(amountStr))); // use negative amount to have operation created in from account
                }
            }
        }

        public void AddPaycheckOnAccount(PyInt accountHandle, PyString title, PyString amount)
        {
            if (m_currentDayOperations == DateTime.MinValue)
                throw new Exception("Pay operation must be inside a Budget.Day statement");

            using (Py.GIL())
            {
                int accountIndex = accountHandle.ToInt32();

                Account account = m_accounts[accountIndex];

                Operation op = new Operation(account, title.ToString(), m_currentDayOperations, ParseDecimal(amount), OpTypes.Paycheck, null);

                account.AddOperation(op);
            }

            if (m_paychecksCount > 0)
            {
                m_paychecks.Add(m_currentDayOperations);
            }
        }

        public void AddPaymentOnAccount(PyInt accountHandle, PyString title, PyString amount)
        {
            if (m_currentDayOperations == DateTime.MinValue)
                throw new Exception("Pay operation must be inside a Budget.Day statement");

            using (Py.GIL())
            {
                int accountIndex = accountHandle.ToInt32();

                Account account = m_accounts[accountIndex];

                Operation op = new Operation(account, title.ToString(), m_currentDayOperations, -ParseDecimal(amount), OpTypes.Payment, null);

                account.AddOperation(op);
            }
        }

        public void AddTransferOnAccount(PyObject title, PyInt accountFromHandle, PyInt accountToHandle, PyObject amount)
        {
            if (m_currentDayOperations == DateTime.MinValue)
                throw new Exception("Pay operation must be inside a Budget.Day statement");

            if (title == null)
                throw new Exception("Missing title");

            using (Py.GIL())
            {
                int accountFromIndex = accountFromHandle.ToInt32();
                int accountToIndex = accountToHandle.ToInt32();

                Account accountFrom = m_accounts[accountFromIndex];
                Account accountTo = m_accounts[accountToIndex];

                TransferOperation op;
                if (amount.GetPythonType().Name == "OverUntilNextPay")
                {
                    PyString maxBalanceStr = amount.GetAttr("maxBalance").As<PyString>();
                    PyString roundStr = amount.GetAttr("round").As<PyString>();

                    string strTitleA;
                    string strTitleB;
                    if (title.GetPythonType().Name == "DualTitle")
                    {
                        strTitleA = title.GetAttr("posTitle").As<PyString>().ToString();
                        strTitleB = title.GetAttr("negTitle").As<PyString>().ToString();
                    }
                    else
                    {
                        strTitleA = title.As<PyString>().ToString();
                        strTitleB = strTitleA;
                    }

                    op = new OverNextPayOperation(accountFrom, accountTo, strTitleA, strTitleB, m_currentDayOperations, ParseDecimal(maxBalanceStr), ParseDecimal(roundStr), null);
                }
                else
                {
                    PyString amountStr = amount.As<PyString>();
                    string titleText = title.ToString() ?? "[UNKNOWN TITLE]";
                    op = new TransferOperation(accountFrom, accountTo, titleText, m_currentDayOperations, -ParseDecimal(amountStr), null); // use negative amount to have operation created in from account
                }


                AddTransferOperationToAccounts(accountFrom, accountTo, op);
            }
        }


        // freqs

        public PyInt CreateMonthlyFreq(PyObject when)
        {
            IFreq? freq = null;

            using (Py.GIL())
            {
                if (PyInt.IsIntType(when))
                {
                    int day = when.As<PyInt>().ToInt32();
                    freq = new Freqs.Monthly(day);
                }
                else if (PyString.IsStringType(when))
                {
                    string moment = when.As<PyString>().ToString();
                    if (moment == "last")
                        freq = new Freqs.Monthly(-1);
                }
            }

            if (freq == null)
                throw new Exception("Invalid monthly setting");

            return new PyInt(m_freqs.AddIndex(freq));
        }

        public PyInt CreateBiWeeklyFreq(PyString start)
        {
            DateTime startDt = CDateTime.Parse(start);
            IFreq freq = new Freqs.BiWeekly(startDt);
            if (freq == null)
                throw new Exception("Invalid biweekly setting");

            return new PyInt(m_freqs.AddIndex(freq));
        }

        public PyInt CreateYearlyFreq(PyString start)
        {
            string startStr;
            using (Py.GIL())
            {
                startStr = start.ToString().Trim();
            }

            DateTime startDt;
            string[] formats =
            {
                "M-d",
                "MM-d",
                "M-dd",
                "MM-dd"
            };
            if (DateTime.TryParseExact(startStr, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out startDt))
            {
                startDt = CDateTime.Parse($"{DateTime.Now.Year}-{startStr}");
            }
            else
            {
                startDt = CDateTime.Parse(startStr);
            }

            IFreq? freq = new Freqs.Yearly(startDt);
            if (freq == null)
                throw new Exception("Invalid yearly setting");

            return new PyInt(m_freqs.AddIndex(freq));
        }

        // functions

        private static decimal ParseDecimal(PyString str)
        {
            string text = str.ToString().Trim();
            text = text.Replace("$", "");
            text = text.Replace(" ", "");

            if (text.Contains('.'))
                text = text.Replace(",", "");
            else
                text = text.Replace(",", ".");

            return decimal.Parse(text, CultureInfo.InvariantCulture);
        }

        private static (int month, int day) ParseMonthDay(PyString str)
        {
            string[] values = str.ToString().Split('-');

            int month = int.Parse(values[0]);
            int day = int.Parse(values[1]);

            return (month, day);
        }

        private void InsertNewOperations(List<DateTime> days)
        {
            Dictionary<DateTime, List<NewOperation>> dayNewOperations = new Dictionary<DateTime, List<NewOperation>>();
            foreach (Account account in m_accounts)
            {
                foreach (var day in days)
                {
                    var dayOperation = account.GetDay(day);
                    if (dayOperation.Operations.Count == 0)
                        continue;

                    if (!dayNewOperations.TryGetValue(day, out List<NewOperation>? list))
                    {
                        list = new List<NewOperation>();
                        dayNewOperations.Add(day, list);
                    }

                    foreach (var op in dayOperation.Operations)
                    {
                        var newOp = op.NewOperation;

                        if (newOp != null)
                        {
                            if (!list.Contains(newOp))
                                list.Add(newOp);
                        }
                    }
                }
            }

            List<DateTime> newDays = new List<DateTime>(dayNewOperations.Keys.ToArray());
            newDays.Sort();

            List<string> newLines = new List<string>();

            foreach (var day in newDays)
            {
                List<NewOperation> operations = dayNewOperations[day];

                if (operations.Count == 0)
                    continue;
                
                newLines.Add($"Budget.Day('{day.ToString("yyyy-MM-dd")}')(");

                int count = operations.Count;
                foreach (NewOperation operation in operations)
                {
                    object amountStr = operation.AmountAsOperationParameter;

                    string cmd = "";
                    if (operation.OpType == OpTypes.Transfer)
                    {
                        string title;
                        OverNextPayNewOperation? overNextPayOp = operation as OverNextPayNewOperation;
                        if (overNextPayOp != null)
                        {
                            title = $"DualTitle('{overNextPayOp.Title}', '{overNextPayOp.OtherTitle}')";
                        }
                        else
                        {
                            title = $"'{operation.Title}'";
                        }

                        cmd = $"Budget.Transfer({title}, {operation.Accounts[0].VariableName}, {operation.Accounts[1].VariableName}, {amountStr})";
                    }
                    else
                    {
                        string opName = "";
                        switch (operation.OpType)
                        {
                            case OpTypes.Payment:
                                opName = "Payment";
                                break;
                            case OpTypes.Paycheck:
                                opName = "Paycheck";
                                break;
                            default:
                                throw new NotImplementedException("OpType " + operation.OpType.ToString());
                        }

                        cmd = $"{operation.Accounts[0].VariableName}.{opName}('{operation.Title}', {amountStr})";
                    }

                    string line = $"    {cmd}";

                    count--;

                    if (count != 0)
                        line += ",";

                    newLines.Add(line);
                }

                newLines.Add(")");
                newLines.Add("");
            }

            if (newLines.Count > 0 && m_endMarker != null)
            {
                FileManipulator.Line endLinePointer = m_endMarker.LinePointer;

                endLinePointer.InsertLinesBefore(newLines.ToArray(), Environment.NewLine);
            }
        }

        private void AddTransferOperationToAccounts(Account from, Account to, TransferOperation op)
        {
            Operation opInv = op.Invert();

            from.AddOperation(op);
            to.AddOperation(opInv);

            op.InvertedOperation = opInv;
        }

        private void ApplyTransferOverNextPayOperations(List<DateTime> days, DateTime processUntil)
        {
            List<OverNextPayOperation> operations = new List<OverNextPayOperation>();

            foreach (Account account in m_accounts)
            {
                foreach (var day in days)
                {
                    var dayOperation = account.GetDay(day);
                    if (dayOperation.Operations.Count == 0)
                        continue;

                    foreach (var op in dayOperation.Operations)
                    {
                        var overOp = op as OverNextPayOperation;
                        if (overOp == null)
                            continue;

                        operations.Add(overOp);
                    }
                }
            }

            operations.Sort((x, y) => { return Comparer<DateTime>.Default.Compare(x.When, y.When); });

            foreach (OverNextPayOperation overOp in operations)
            {
                DateTime dt = overOp.When;

                decimal min = overOp.Account.GetDay(dt).Balance;
                dt = dt.AddDays(1);

                while (dt < processUntil)
                {
                    var dayOps = overOp.Account.GetDay(dt);
                    decimal b = dayOps.Balance;

                    bool payCheck = false;
                    foreach (var o in dayOps.Operations)
                    {
                        if (o.OperationType == OpTypes.Paycheck)
                        {
                            payCheck = true;
                            break;
                        }
                    }
                    if (payCheck)
                        break;

                    min = Math.Min(min, b);

                    dt = dt.AddDays(1);
                }

                overOp.UpdateAmount(min, processUntil);
            }
        }

        private DateTime GetLastDateAfterPaycheck(DateTime beginRecurrentDate, int paychecksCount, bool completeLastMonth)
        {
            bool added = false;

            foreach (IFreq freq in m_recurrentPaychecks)
            {
                DateTime dt = beginRecurrentDate;
                for (int i = 0; i < paychecksCount + 1; i++)
                {
                    DateTime next = freq.GetNext(dt);
                    if (!m_paychecks.Contains(next))
                    {
                        m_paychecks.Add(next);
                        added = true;
                    }
                    dt = next.AddDays(1);
                }

                if (completeLastMonth)
                {
                    int month = dt.Month;
                    while (true)
                    {
                        DateTime next = freq.GetNext(dt);
                        if (!m_paychecks.Contains(next))
                        {
                            m_paychecks.Add(next);
                            added = true;
                        }
                        if (next.Month != month)
                            break;

                        dt = next.AddDays(1);
                    }
                }
            }

            if (added)
            {
                m_paychecks.Sort();
            }

            DateTime date = m_paychecks[paychecksCount].AddDays(-1);

            if (completeLastMonth)
            {
                date = new DateTime(date.Year, date.Month, 1).AddMonths(1).AddDays(-1);
            }

            return date;
        }
    }
}
