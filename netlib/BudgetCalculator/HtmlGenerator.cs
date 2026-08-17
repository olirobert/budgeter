using Scriban.Runtime;
using System.Reflection;
using static BudgetCalculator.Account;

namespace BudgetCalculator
{
    public class HtmlGenerator
    {
        internal static void Save(string filepath, List<Account> accounts, List<DateTime> budgetDays, List<(Account, DateTime)> forecastAccounts)
        {
            string dllDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "";

            string content = File.ReadAllText(Path.Combine(dllDir, "budget_result.template.htm"));

            var templateParser = Scriban.Template.Parse(content);

            HtmlGenerator generator = new HtmlGenerator(accounts, budgetDays, forecastAccounts);

            var scriptObject = new ScriptObject();
            scriptObject.Import(typeof(HtmlGenerator));
            scriptObject.Import(generator, null, generator.MemberRenamerDelegate);

            var context = new Scriban.TemplateContext();
            context.MemberRenamer = generator.MemberRenamerDelegate;
            context.MemberFilter = null;
            context.PushGlobal(scriptObject);
            context.LoopLimit = 100000;

            content = templateParser.Render(context);

            content = HtmlSpaceRemover.RemoveSpaces(content);

            File.WriteAllText(filepath, content);
        }

        private static string c_spanning = "$$$";

        public class Cell
        {
            public bool Visible = true;
            public int ColSpan = 1;
            public int RowSpan = 1;

            public string? Content = null;

            public int Size = 1;
            public bool SoftHighlight = false;
            public bool HardHighlight = false;
        }

        public class GridCells
        {
            private List<List<Cell?>> m_cells;

            public List<List<Cell?>> Rows { get => m_cells; }

            public GridCells(int cols, int rows)
            {
                m_cells = new List<List<Cell?>>(rows);

                for (int i = 0; i < rows; i++)
                {
                    List<Cell?> row = new List<Cell?>(cols);

                    for (int j = 0; j < cols; j++)
                    {
                        row.Add(new Cell());
                    }

                    m_cells.Add(row);
                }
            }

            public string? this[int x, int y]
            {
                get
                {
                    Cell? c = m_cells[y][x];
                    if (c != null)
                        return c.Content;
                    else
                        return null;
                }
                set
                {
                    Cell? c = m_cells[y][x];
                    if (c == null)
                    {
                        c = new Cell();
                        m_cells[y][x] = c;
                    }
                    c.Content = value;
                }
            }

            public void SetCellRowSpan(int x, int y, int rowspan)
            {
                Cell? c = m_cells[y][x] ?? throw new Exception("Empty cell");
                c.RowSpan = rowspan;

                for (int i = 1; i < rowspan; i++)
                {
                    List<Cell?> row = m_cells[y + i];
                    row[x] = null;
                }
            }

            public void SetCellColSpan(int x, int y, int colspan)
            {
                Cell? c = m_cells[y][x] ?? throw new Exception("Empty cell");
                c.ColSpan = colspan;

                List<Cell?> row = m_cells[y];
                for (int i = 1; i < colspan; i++)
                {
                    row[x + i] = null;
                }
            }

            public void SetCellHidden(int x, int y)
            {
                Cell? c = m_cells[y][x] ?? throw new Exception("Empty cell"); ;
                c.Visible = false;
            }

            public void SetCellSize(int x, int y, int size)
            {
                Cell? c = m_cells[y][x];
                if (c != null)
                {
                    c.Size = size;
                }
            }

            public enum Highlight
            {
                None,
                Soft,
                Hard,
            }
            public void SetCellHighlight(int x, int y, Highlight h)
            {
                Cell? c = m_cells[y][x] ?? throw new Exception("Empty cell");
                switch (h)
                {
                    case Highlight.Soft:
                        c.SoftHighlight = true;
                        c.HardHighlight = false;
                        break;
                    case Highlight.Hard:
                        c.SoftHighlight = false;
                        c.HardHighlight = true;
                        break;
                }
            }

            public void SplitRow(int y, int count)
            {
                List<Cell?> origRow = m_cells[y];

                int colCount = origRow.Count;

                for (int i = 0; i < count - 1; i++)
                {
                    List<Cell?> row = new List<Cell?>();
                    for (int j = 0; j < colCount; j++)
                    {
                        row.Add(new Cell()
                        {
                            ColSpan = 1,
                            RowSpan = 1,
                            Visible = true,
                            Content = c_spanning
                        });
                    }

                    if (y == m_cells.Count - 1)
                        m_cells.Add(row);
                    else
                        m_cells.Insert(y + 1, row);
                }
            }
        }

        private List<GridCells> m_grids = new List<GridCells>();

        public List<GridCells> Grids { get => m_grids; }

        private HtmlGenerator(List<Account> accounts, List<DateTime> budgetDays, List<(Account, DateTime)> forecastAccounts)
        {
            BuildBudgetCells(accounts, budgetDays);

            BuildForecast(forecastAccounts);
        }

        private void BuildBudgetCells(List<Account> accounts, List<DateTime> budgetDays)
        {
            bool haveBeforeToday = budgetDays[0] == DateTime.MinValue;

            int rowsCount = 2 + budgetDays.Count;
            int colsCount = 2 + (accounts.Count * 5);

            GridCells cells = new GridCells(colsCount, rowsCount);
            m_grids.Add(cells);

            // header
            cells.SetCellHidden(0, 0);
            cells.SetCellColSpan(0, 0, 2);

            int col = 2;
            foreach (Account account in accounts)
            {
                cells.SetCellColSpan(col, 0, 5);
                cells[col, 0] = account.Name;
                col += 5;
            }

            // initial balance
            cells.SetCellColSpan(0, 1, 2);
            cells[0, 1] = "Initial balance";

            col = 2;
            foreach (Account account in accounts)
            {
                cells[col + 0, 1] = Dollard(account.EntryBalance);

                cells.SetCellColSpan(col + 1, 1, 4);

                col += 5;
            }

            // set dates
            int row = 2;
            foreach (DateTime day in budgetDays)
            {
                if (day == DateTime.MinValue)
                {
                    cells[0, row] = "Before today";
                    cells.SetCellColSpan(0, row, 2);
                }
                else
                {
                    cells[0, row] = day.Month.ToString();
                    cells[1, row] = day.Day.ToString();
                }
                row++;
            }

            row = rowsCount - 1;

            //int count = 0;
            int last = haveBeforeToday ? 1 : 0;
            for (int i = budgetDays.Count - 1; i >= last; i--, row--)
            {
                DateTime dt = budgetDays[i];

                if (dt.Day != 1 && i != last)
                    cells[0, row] = c_spanning;
            }

            // operations
            row = 2;
            foreach (DateTime day in budgetDays)
            {
                // first determine how many lines required
                int usedRows = 1; // minium is 1 row
                foreach (Account account in accounts)
                {
                    DayPaiements dayPaiements = account.GetDay(day);
                    usedRows = Math.Max(usedRows, dayPaiements.PositiveOperations.Count);
                    usedRows = Math.Max(usedRows, dayPaiements.NegativeOperations.Count);
                }

                if (usedRows > 1)
                {
                    cells.SplitRow(row, usedRows);

                    if (day == DateTime.MinValue)
                    {
                        for (int i = 1; i < usedRows; i++)
                        {
                            cells.SetCellColSpan(0, row + i, 2);
                        }
                    }
                }

                col = 2;
                foreach (Account account in accounts)
                {
                    DayPaiements dayPaiements = account.GetDay(day);

                    cells[col + 0, row] = Dollard(dayPaiements.Balance);
                    CheckForHighlight(cells, account, col + 0, row, dayPaiements.Balance);

                    int i = 0;
                    // positive operations
                    foreach (var op in dayPaiements.PositiveOperations)
                    {
                        cells[col + 1, row + i] = op.Title;
                        cells[col + 2, row + i] = Dollard(op.Amount, true);
                        i++;
                    }
                    for (int j = i; j < usedRows; j++)
                    {
                        cells[col + 1, row + j] = j == i ? null : c_spanning;
                        cells.SetCellColSpan(col + 1, row + j, 2);
                    }

                    i = 0;
                    // negative operations
                    foreach (var op in dayPaiements.NegativeOperations)
                    {
                        cells[col + 3, row + i] = op.Title;
                        cells[col + 4, row + i] = Dollard(op.Amount, true);
                        i++;
                    }
                    for (int j = i; j < usedRows; j++)
                    {
                        cells[col + 3, row + j] = j == i ? null : c_spanning;
                        cells.SetCellColSpan(col + 3, row + j, 2);
                    }

                    col += 5;
                }

                row += usedRows;
            }

            // processing spanning
            for (int x = 0; x < colsCount; x++)
            {
                int count = 0;
                for (int y = cells.Rows.Count - 1; y >= 2; y--)
                {
                    List<Cell?> r = cells.Rows[y];

                    Cell? cell = r[x];
                    if (cell == null)
                        continue;

                    string content = cell.Content ?? string.Empty;
                    if (content == c_spanning)
                    {
                        count++;
                    }
                    else
                    {
                        cells.SetCellRowSpan(x, y, count + 1);
                        count = 0;
                    }
                }
            }

            // calculate maximum sizes
            int amountLen = 1;
            int titleLen = 1;
            foreach (Account account in accounts)
            {
                SetMaxLen(ref amountLen, account.EndBalance);
                SetMaxLen(ref amountLen, account.EntryBalance);
                foreach (DayPaiements dp in account.Days)
                {
                    SetMaxLen(ref amountLen, dp.Balance);
                    foreach (Operation op in dp.Operations)
                    {
                        SetMaxLen(ref amountLen, op.Amount, true);
                        SetMaxLen(ref titleLen, op.Title);
                    }
                }
            }

            // adjust column widths
            Cell? c = cells.Rows[1][0];
            if (c != null)
            {
                string content = c.Content ?? string.Empty;
                cells.SetCellSize(0, 1, content.Length);
                for (int y = 1; y < cells.Rows.Count; y++)
                {
                    for (int i = 0; i < accounts.Count; i++)
                    {
                        cells.SetCellSize(2 + (i * 5) + 0, y, amountLen);
                        cells.SetCellSize(2 + (i * 5) + 2, y, amountLen);
                        cells.SetCellSize(2 + (i * 5) + 4, y, amountLen);

                        cells.SetCellSize(2 + (i * 5) + 1, y, titleLen);
                        cells.SetCellSize(2 + (i * 5) + 3, y, titleLen);
                    }
                }
            }
            else
            {
                c = null;
            }
        }

        private void BuildForecast(List<(Account, DateTime)> forecastAccounts)
        {
            if (forecastAccounts.Count == 0)
                return;

            // forecast pre calculation
            DateTime lastForecastDate = CDateTime.Today;
            foreach ((_, DateTime lastDate) in forecastAccounts)
            {
                if (lastDate > lastForecastDate)
                    lastForecastDate = lastDate;
            }
            DateTime month = new DateTime(CDateTime.Today.Year, CDateTime.Today.Month, 1);
            int forecastRows = 0;
            while (true)
            {
                forecastRows++;

                if (month.Year == lastForecastDate.Year && month.Month == lastForecastDate.Month)
                    break;

                month = month.AddMonths(1);
            }

            int rowsCount = 1 + forecastRows;
            int colsCount = 2 + (forecastAccounts.Count * 2);

            GridCells cells = new GridCells(colsCount, rowsCount);
            m_grids.Add(cells);

            // header
            cells.SetCellHidden(0, 0);
            cells.SetCellColSpan(0, 0, 2);

            // dates
            month = new DateTime(CDateTime.Today.Year, CDateTime.Today.Month, 1);
            for (int i = 0; i < forecastRows; i++)
            {
                if (i == 0)
                {
                    cells.SetCellColSpan(0, 1 + i, 2);
                    cells[0, 1 + i] = "Current month";
                }
                else
                {
                    cells[0, 1 + i] = month.Year.ToString();
                    cells[1, 1 + i] = month.Month.ToString();
                }

                month = month.AddMonths(1);
            }

            int col = 2;
            int accIndex = 0;
            foreach ((Account account, DateTime lastDate) in forecastAccounts)
            {
                cells.SetCellColSpan(col, 0, 2);
                cells[col, 0] = account.Name;

                DateTime lastMonth = new DateTime(lastDate.Year, lastDate.Month, 1);

                month = new DateTime(CDateTime.Today.Year, CDateTime.Today.Month, 1);
                for (int row = 0; row < forecastRows; row++)
                {
                    (decimal min, decimal max) = account.Forecast[month];

                    cells[col + 0, 1 + row] = Dollard(min);
                    CheckForHighlight(cells, account, col + 0, 1 + row, min);

                    cells[col + 1, 1 + row] = Dollard(max);
                    CheckForHighlight(cells, account, col + 1, 1 + row, min);

                    if (month.Year == lastDate.Year && month.Month == lastDate.Month)
                        break;

                    month = month.AddMonths(1);
                }

                col += 2;
                accIndex++;
            }
        }

        private static string Dollard(decimal dec, bool abs = false)
        {
            decimal d = abs ? Math.Abs(dec) : dec;

            string s = d.ToString("c");
            return s;
        }

        private static void SetMaxLen(ref int len, decimal d, bool abs = false)
        {
            string s = Dollard(d, abs);
            SetMaxLen(ref len, s);
        }

        private static void SetMaxLen(ref int len, string s)
        {
            len = Math.Max(len, s.Length);
        }

        private static void CheckForHighlight(GridCells cells, Account account, int x, int y, decimal balance)
        {
            if (balance < 0)
                cells.SetCellHighlight(x, y, GridCells.Highlight.Hard);
            else if (balance < account.SoftMinium)
                cells.SetCellHighlight(x, y, GridCells.Highlight.Soft);
        }

        private string MemberRenamerDelegate(MemberInfo member)
        {
            return member.Name;
        }
    }
}
