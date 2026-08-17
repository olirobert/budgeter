//#define USE_TEMP_FOLDER

using Python.Runtime;
using System.Globalization;
using System.Text;

namespace BudgetCalculator.Tests
{
    public class BudgetProcessorTests
    {
        private static string NormalizeLineEndings(string s)
        {
            return s.Replace("\r\n", "\n").Replace("\r", "\n");
        }

        private static void AssertEqualIgnoreLineEndings(string expected, string actual)
        {
            Assert.Equal(NormalizeLineEndings(expected), NormalizeLineEndings(actual));
        }

        private static string GetTempFolder()
        {
#if USE_TEMP_FOLDER
            string tempFolder = "D:\\temp\\BudgetCalculatorTests";
#else
            string tempFolder = Path.Combine(Path.GetTempPath(), "BudgetCalculatorTests_" + Guid.NewGuid().ToString("N"));
#endif
            Directory.CreateDirectory(tempFolder);
            return tempFolder;
        }

        private PyObject OverUntilNextPay(string maxBalance, string? round = null)
        {
            const string code = @"
class OverUntilNextPay:
    def __init__(self, maxBalance: str, round: str):
        self.maxBalance = maxBalance
        self.round = round
";

            PyModule module = PyModule.FromString("budgetModule", code);

            dynamic overUntilNextPay = module.GetAttr("OverUntilNextPay");
            if (round != null)
            {
                return overUntilNextPay(new PyString(maxBalance), new PyString(round));
            }
            else
            {
                return overUntilNextPay(new PyString(maxBalance));
            }
        }

        private PyObject DualTitle(string posTitle, string negTitle)
        {
            const string code = @"
class DualTitle:
    def __init__(self, pos: str, neg: str):
        self.posTitle = pos
        self.negTitle = neg
";

            PyModule module = PyModule.FromString("budgetModule", code);

            dynamic dualTitle = module.GetAttr("DualTitle");
            return dualTitle(new PyString(posTitle), new PyString(negTitle));
        }

        private class Amount
        {
            public string AmountStr;
            public string? DayOfYear = null;

            public Amount(string amount)
            {
                AmountStr = amount;
            }

            public Amount(string amount, string dayOfYear)
            {
                AmountStr = amount;
                DayOfYear = dayOfYear;
            }
        }

        private PyObject MultipleAmounts(params Amount[] amounts)
        {
            const string codeA = @"
class DayOfYear:
    def __init__(self, when: str):
        self.when = when


class Before:
    def __init__(self, when, amount: str):
        self.when = when
        self.amount = amount

class Then:
    def __init__(self, amount: str):
        self.amount = amount

class MultipleAmounts:
    def __init__(self, *args):
        self.amounts = list(args)

def CreateMultipleAmounts():
    return MultipleAmounts(";

            const string codeB = @")";

            StringBuilder code = new StringBuilder();
            code.Append(codeA);

            bool first = true;
            foreach (var amount in amounts)
            {
                if (first)
                    first = false;
                else
                    code.Append(",");

                if (amount.DayOfYear != null)
                {
                    code.Append($"Before(DayOfYear('{amount.DayOfYear}'), '{amount.AmountStr}')");
                }
                else
                {
                    code.Append($"Then('{amount.AmountStr}')");
                }
            }

            code.Append(codeB);

            PyModule module = PyModule.FromString("budgetModule", code.ToString());

            PyObject method = module.GetAttr("CreateMultipleAmounts");
            PyObject ma = method.Invoke();
            return ma;
        }

        private static void SetCulture(string cultureName)
        {
            CultureInfo culture = new CultureInfo(cultureName);
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;
        }

        private static void SetCulture()
        {
            SetCulture("en-CA");
        }

        [Fact]
        public void Freqs()
        {
            SetCulture();

            string tempFolder = GetTempFolder();
            string historyFile = Path.Combine(tempFolder, "history.py");
            string outputBudgetFile = Path.Combine(tempFolder, "budget.html");

            try
            {
                PythonEngine.Initialize();

                string[] historyBefore = File.ReadAllLines("FreqsFiles\\history.py.before");
                File.WriteAllLines(historyFile, historyBefore);

                using (CDateTime.Override(new DateTime(2026, 2, 1, 9, 0, 0)))
                {
                    BudgetProcessor processor;

                    using (Py.GIL())
                    {
                        processor = new BudgetProcessor(new PyString(historyFile));

                        var account = processor.AddAccount(new PyString("National First Bank"));
                        processor.SetAccountVariableName(account, new PyString("bank"));
                        processor.SetAccountEntryBalance(account, new PyString("100.00"));

                        processor.SetProcessingDaysCount(new PyInt(-3)); // AfterPaychecks

                        var jobFreq = processor.CreateBiWeeklyFreq(new PyString("2023-9-27"));
                        processor.AddRecurrentPayOnAccount(account, new PyString("Job"), jobFreq, new PyString("1000.00"));

                        var monthlyFreq = processor.CreateMonthlyFreq(new PyInt("2"));
                        processor.AddRecurrentBillOnAccount(account, new PyString("House mortgage"), monthlyFreq, new PyString("100.00"));

                        var endOfMonthFreq = processor.CreateMonthlyFreq(new PyInt("-1"));
                        processor.AddRecurrentBillOnAccount(account, new PyString("Bank fees"), endOfMonthFreq, new PyString("1.00"));

                        var annualFreq = processor.CreateYearlyFreq(new PyString("2024-3-1"));
                        processor.AddRecurrentBillOnAccount(account, new PyString("Assurances"), annualFreq, new PyString("200.00"));

                        processor.MarkEnd(new PyString("2026-02-01"), new PyString(historyFile), new PyInt(historyBefore.Length));

                        processor.Update(new PyString(historyFile), new PyString(outputBudgetFile));
                    }

                    string historyActual = File.ReadAllText(historyFile);
                    string historyExpected = File.ReadAllText("FreqsFiles\\history.py.after");
                    AssertEqualIgnoreLineEndings(historyExpected, historyActual);

                    string htmlActual = File.ReadAllText(outputBudgetFile);
                    string htmlExpected = File.ReadAllText("FreqsFiles\\budget.html.after");
                    AssertEqualIgnoreLineEndings(htmlExpected, htmlActual);
                }
            }
            finally
            {
#if !USE_TEMP_FOLDER
                Directory.Delete(tempFolder, recursive: true);
#endif
            }
        }

        [Fact]
        public void Transfers()
        {
            SetCulture();

            string tempFolder = GetTempFolder();
            string historyFile = Path.Combine(tempFolder, "history.py");
            string outputBudgetFile = Path.Combine(tempFolder, "budget.html");

            try
            {
                PythonEngine.Initialize();

                string[] historyBefore = File.ReadAllLines("TransfersFiles\\history.py.before");
                File.WriteAllLines(historyFile, historyBefore);

                using (CDateTime.Override(new DateTime(2026, 2, 1, 9, 0, 0)))
                {
                    BudgetProcessor processor;

                    using (Py.GIL())
                    {
                        processor = new BudgetProcessor(new PyString(historyFile));

                        var account1 = processor.AddAccount(new PyString("National First Bank"));
                        processor.SetAccountVariableName(account1, new PyString("bank"));
                        processor.SetAccountEntryBalance(account1, new PyString("1000.00"));

                        var account2 = processor.AddAccount(new PyString("Saving account A"));
                        processor.SetAccountVariableName(account2, new PyString("saving1"));
                        processor.SetAccountEntryBalance(account2, new PyString("0.00"));

                        var account3 = processor.AddAccount(new PyString("Saving account B"));
                        processor.SetAccountVariableName(account3, new PyString("saving2"));
                        processor.SetAccountEntryBalance(account3, new PyString("0.00"));

                        processor.SetProcessingDaysCount(new PyInt(-3)); // AfterPaychecks

                        var jobFreq = processor.CreateBiWeeklyFreq(new PyString("2023-9-27"));
                        processor.AddRecurrentPayOnAccount(account1, new PyString("Job"), jobFreq, new PyString("1000.00"));
                        // don't support no rounding for OverUntilNextPay
                        var nextDayFreq = processor.CreateBiWeeklyFreq(new PyString("2023-9-28"));
                        processor.AddRecurrentTransferOnAccount(new PyString("Saving"), account1, account3, nextDayFreq, OverUntilNextPay("1500.00", "10.00"));

                        var monthly2Freq = processor.CreateMonthlyFreq(new PyInt("2"));
                        processor.AddRecurrentTransferOnAccount(new PyString("Saving"), account1, account2, monthly2Freq, new PyString("150.00"));

                        processor.MarkEnd(new PyString("2026-02-01"), new PyString(historyFile), new PyInt(historyBefore.Length));

                        processor.Update(new PyString(historyFile), new PyString(outputBudgetFile));
                    }

                    string historyActual = File.ReadAllText(historyFile);
                    string historyExpected = File.ReadAllText("TransfersFiles\\history.py.after");
                    AssertEqualIgnoreLineEndings(historyExpected, historyActual);

                    string htmlActual = File.ReadAllText(outputBudgetFile);
                    string htmlExpected = File.ReadAllText("TransfersFiles\\budget.html.after");
                    AssertEqualIgnoreLineEndings(htmlExpected, htmlActual);
                }
            }
            finally
            {
#if !USE_TEMP_FOLDER
                Directory.Delete(tempFolder, recursive: true);
#endif
            }
        }

        [Fact]
        public void TransferDualTile()
        {
            SetCulture();

            string tempFolder = GetTempFolder();
            string historyFile = Path.Combine(tempFolder, "history.py");
            string outputBudgetFile = Path.Combine(tempFolder, "budget.html");

            try
            {
                PythonEngine.Initialize();

                string[] historyBefore = File.ReadAllLines("TransferDualTitleFiles\\history.py.before");
                File.WriteAllLines(historyFile, historyBefore);

                using (CDateTime.Override(new DateTime(2026, 2, 1, 9, 0, 0)))
                {
                    BudgetProcessor processor;

                    using (Py.GIL())
                    {
                        processor = new BudgetProcessor(new PyString(historyFile));

                        var account1 = processor.AddAccount(new PyString("National First Bank"));
                        processor.SetAccountVariableName(account1, new PyString("bank"));
                        processor.SetAccountEntryBalance(account1, new PyString("1000.00"));

                        var account2 = processor.AddAccount(new PyString("Saving account"));
                        processor.SetAccountVariableName(account2, new PyString("saving"));
                        processor.SetAccountEntryBalance(account2, new PyString("0.00"));

                        processor.SetProcessingDaysCount(new PyInt(-3)); // AfterPaychecks

                        var jobFreq = processor.CreateBiWeeklyFreq(new PyString("2023-9-27"));
                        processor.AddRecurrentPayOnAccount(account1, new PyString("Job"), jobFreq, new PyString("1000.00"));
                        // don't support no rounding for OverUntilNextPay
                        var nextDayFreq = processor.CreateBiWeeklyFreq(new PyString("2023-9-28"));
                        processor.AddRecurrentTransferOnAccount(DualTitle("Saving", "Overdraft"), account1, account2, nextDayFreq, OverUntilNextPay("1500.00", "10.00"));

                        var firstFreq = processor.CreateYearlyFreq(new PyString("2-15"));
                        processor.AddRecurrentBillOnAccount(account1, new PyString("Payemnt 1"), firstFreq, new PyString("100.00"));

                        var secondFreq = processor.CreateYearlyFreq(new PyString("3-15"));
                        processor.AddRecurrentBillOnAccount(account1, new PyString("Payemnt 2"), secondFreq, new PyString("2000.00"));

                        processor.MarkEnd(new PyString("2026-02-01"), new PyString(historyFile), new PyInt(historyBefore.Length));

                        processor.Update(new PyString(historyFile), new PyString(outputBudgetFile));
                    }

                    string historyActual = File.ReadAllText(historyFile);
                    string historyExpected = File.ReadAllText("TransferDualTitleFiles\\history.py.after");
                    AssertEqualIgnoreLineEndings(historyExpected, historyActual);

                    string htmlActual = File.ReadAllText(outputBudgetFile);
                    string htmlExpected = File.ReadAllText("TransferDualTitleFiles\\budget.html.after");
                    AssertEqualIgnoreLineEndings(htmlExpected, htmlActual);
                }
            }
            finally
            {
#if !USE_TEMP_FOLDER
                Directory.Delete(tempFolder, recursive: true);
#endif
            }
        }

        [Fact]
        public void PayMultipleAmounts()
        {
            SetCulture();

            string tempFolder = GetTempFolder();
            string historyFile = Path.Combine(tempFolder, "history.py");
            string outputBudgetFile = Path.Combine(tempFolder, "budget.html");

            try
            {
                PythonEngine.Initialize();

                string[] historyBefore = File.ReadAllLines("PayMultipleAmountsFiles\\history.py.before");
                File.WriteAllLines(historyFile, historyBefore);

                using (CDateTime.Override(new DateTime(2026, 2, 1, 9, 0, 0)))
                {
                    BudgetProcessor processor;

                    using (Py.GIL())
                    {
                        processor = new BudgetProcessor(new PyString(historyFile));

                        var account = processor.AddAccount(new PyString("National First Bank"));
                        processor.SetAccountVariableName(account, new PyString("bank"));
                        processor.SetAccountEntryBalance(account, new PyString("1000.00"));

                        processor.SetProcessingDaysCount(new PyInt(-3)); // AfterPaychecks

                        var jobFreq = processor.CreateBiWeeklyFreq(new PyString("2023-9-27"));
                        processor.AddRecurrentPayOnAccount(account, new PyString("Job"), jobFreq, MultipleAmounts(new Amount("1000.00", "3-1"), new Amount("1500.00")));

                        processor.MarkEnd(new PyString("2026-02-01"), new PyString(historyFile), new PyInt(historyBefore.Length));

                        processor.Update(new PyString(historyFile), new PyString(outputBudgetFile));
                    }

                    string historyActual = File.ReadAllText(historyFile);
                    string historyExpected = File.ReadAllText("PayMultipleAmountsFiles\\history.py.after");
                    AssertEqualIgnoreLineEndings(historyExpected, historyActual);

                    string htmlActual = File.ReadAllText(outputBudgetFile);
                    string htmlExpected = File.ReadAllText("PayMultipleAmountsFiles\\budget.html.after");
                    AssertEqualIgnoreLineEndings(htmlExpected, htmlActual);
                }
            }
            finally
            {
#if !USE_TEMP_FOLDER
                Directory.Delete(tempFolder, recursive: true);
#endif
            }
        }

        [Fact]
        public void History()
        {
            SetCulture();

            string tempFolder = GetTempFolder();
            string historyFile = Path.Combine(tempFolder, "history.py");
            string outputBudgetFile = Path.Combine(tempFolder, "budget.html");

            try
            {
                PythonEngine.Initialize();

                string[] historyBefore = File.ReadAllLines("HistoryFiles\\history.py.before");
                File.WriteAllLines(historyFile, historyBefore);

                using (CDateTime.Override(new DateTime(2026, 2, 1, 9, 0, 0)))
                {
                    BudgetProcessor processor;

                    using (Py.GIL())
                    {
                        processor = new BudgetProcessor(new PyString(historyFile));

                        var account1 = processor.AddAccount(new PyString("National First Bank"));
                        processor.SetAccountVariableName(account1, new PyString("bank"));
                        processor.SetAccountEntryBalance(account1, new PyString("1000.00"));

                        var account2 = processor.AddAccount(new PyString("Saving account"));
                        processor.SetAccountVariableName(account2, new PyString("saving"));
                        processor.SetAccountEntryBalance(account2, new PyString("0.00"));

                        processor.SetProcessingDaysCount(new PyInt(-3)); // AfterPaychecks

                        var jobFreq = processor.CreateBiWeeklyFreq(new PyString("2023-9-27"));
                        processor.AddRecurrentPayOnAccount(account1, new PyString("Job"), jobFreq, new PyString("1000.00"));

                        processor.MarkDay(new PyString("2026-02-15"));
                        processor.AddPaycheckOnAccount(account1, new PyString("Old Jog"), new PyString("500.00"));
                        processor.AddPaymentOnAccount(account1, new PyString("Unknown bill"), new PyString("100.00"));
                        processor.AddTransferOnAccount(new PyString("Saving"), account1, account2, new PyString("1000.00"));

                        processor.MarkEnd(new PyString("2026-02-01"), new PyString(historyFile), new PyInt(historyBefore.Length));

                        processor.Update(new PyString(historyFile), new PyString(outputBudgetFile));
                    }

                    string historyActual = File.ReadAllText(historyFile);
                    string historyExpected = File.ReadAllText("HistoryFiles\\history.py.after");
                    AssertEqualIgnoreLineEndings(historyExpected, historyActual);

                    string htmlActual = File.ReadAllText(outputBudgetFile);
                    string htmlExpected = File.ReadAllText("HistoryFiles\\budget.html.after");
                    AssertEqualIgnoreLineEndings(htmlExpected, htmlActual);
                }
            }
            finally
            {
#if !USE_TEMP_FOLDER
                Directory.Delete(tempFolder, recursive: true);
#endif
            }
        }

        [Fact]
        public void Forecast()
        {
            SetCulture();

            string tempFolder = GetTempFolder();
            string historyFile = Path.Combine(tempFolder, "history.py");
            string outputBudgetFile = Path.Combine(tempFolder, "budget.html");

            try
            {
                PythonEngine.Initialize();

                string[] historyBefore = File.ReadAllLines("ForecastFiles\\history.py.before");
                File.WriteAllLines(historyFile, historyBefore);

                using (CDateTime.Override(new DateTime(2026, 2, 1, 9, 0, 0)))
                {
                    BudgetProcessor processor;

                    using (Py.GIL())
                    {
                        processor = new BudgetProcessor(new PyString(historyFile));

                        var account = processor.AddAccount(new PyString("National First Bank"));
                        processor.SetAccountVariableName(account, new PyString("bank"));
                        processor.SetAccountEntryBalance(account, new PyString("100.00"));
                        processor.SetAccountForecastDaysCount(account, new PyInt(-26)); // AfterPaychecks

                        processor.SetProcessingDaysCount(new PyInt(-3)); // AfterPaychecks

                        var jobFreq = processor.CreateBiWeeklyFreq(new PyString("2023-9-27"));
                        processor.AddRecurrentPayOnAccount(account, new PyString("Job"), jobFreq, new PyString("1000.00"));

                        var monthlyFreq = processor.CreateMonthlyFreq(new PyInt("2"));
                        processor.AddRecurrentBillOnAccount(account, new PyString("House mortgage"), monthlyFreq, new PyString("100.00"));

                        var endOfMonthFreq = processor.CreateMonthlyFreq(new PyInt("-1"));
                        processor.AddRecurrentBillOnAccount(account, new PyString("Bank fees"), endOfMonthFreq, new PyString("1.00"));

                        var annualFreq = processor.CreateYearlyFreq(new PyString("2024-3-1"));
                        processor.AddRecurrentBillOnAccount(account, new PyString("Assurances"), annualFreq, new PyString("200.00"));

                        processor.MarkEnd(new PyString("2026-02-01"), new PyString(historyFile), new PyInt(historyBefore.Length));

                        processor.Update(new PyString(historyFile), new PyString(outputBudgetFile));
                    }

                    string historyActual = File.ReadAllText(historyFile);
                    string historyExpected = File.ReadAllText("ForecastFiles\\history.py.after");
                    AssertEqualIgnoreLineEndings(historyExpected, historyActual);

                    string htmlActual = File.ReadAllText(outputBudgetFile);
                    string htmlExpected = File.ReadAllText("ForecastFiles\\budget.html.after");
                    AssertEqualIgnoreLineEndings(htmlExpected, htmlActual);
                }
            }
            finally
            {
#if !USE_TEMP_FOLDER
                Directory.Delete(tempFolder, recursive: true);
#endif
            }
        }

        [Fact]
        public void MinimumBalance()
        {
            SetCulture();

            string tempFolder = GetTempFolder();
            string historyFile = Path.Combine(tempFolder, "history.py");
            string outputBudgetFile = Path.Combine(tempFolder, "budget.html");

            try
            {
                PythonEngine.Initialize();

                string[] historyBefore = File.ReadAllLines("MinimumBalanceFiles\\history.py.before");
                File.WriteAllLines(historyFile, historyBefore);

                using (CDateTime.Override(new DateTime(2026, 2, 1, 9, 0, 0)))
                {
                    BudgetProcessor processor;

                    using (Py.GIL())
                    {
                        processor = new BudgetProcessor(new PyString(historyFile));

                        var account = processor.AddAccount(new PyString("National First Bank"));
                        processor.SetAccountVariableName(account, new PyString("bank"));
                        processor.SetAccountEntryBalance(account, new PyString("1000.00"));
                        processor.SetAccountSoftMinimum(account, new PyString("500.00"));

                        processor.SetProcessingDaysCount(new PyInt(-3)); // AfterPaychecks

                        var jobFreq = processor.CreateBiWeeklyFreq(new PyString("2023-9-27"));
                        processor.AddRecurrentPayOnAccount(account, new PyString("Job"), jobFreq, new PyString("1000.00"));

                        processor.MarkDay(new PyString("2026-02-15"));
                        processor.AddPaymentOnAccount(account, new PyString("bill 1"), new PyString("1600.00"));

                        processor.MarkDay(new PyString("2026-02-18"));
                        processor.AddPaymentOnAccount(account, new PyString("bill 1"), new PyString("1000.00"));

                        processor.MarkEnd(new PyString("2026-02-01"), new PyString(historyFile), new PyInt(historyBefore.Length));

                        processor.Update(new PyString(historyFile), new PyString(outputBudgetFile));
                    }

                    string historyActual = File.ReadAllText(historyFile);
                    string historyExpected = File.ReadAllText("MinimumBalanceFiles\\history.py.after");
                    AssertEqualIgnoreLineEndings(historyExpected, historyActual);

                    string htmlActual = File.ReadAllText(outputBudgetFile);
                    string htmlExpected = File.ReadAllText("MinimumBalanceFiles\\budget.html.after");
                    AssertEqualIgnoreLineEndings(htmlExpected, htmlActual);
                }
            }
            finally
            {
#if !USE_TEMP_FOLDER
                Directory.Delete(tempFolder, recursive: true);
#endif
            }
        }
    }
}
