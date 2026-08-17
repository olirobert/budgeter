# Library reference

Everything you need is exported from the top-level `budgeter` package:

```python
from budgeter import *
```

which brings in `Budget`, `Account`, the frequency classes (`Monthly`,
`BiWeekly`, `Yearly`), and the helper wrappers (`DayOfYear`, `Before`, `Then`,
`MultipleAmounts`, `DualTitle`, `OverUntilNextPay`, `AfterPaychecks`).

Amounts are strings so the engine controls parsing
and rounding — do not pass Python `float`. Dates are `'YYYY-MM-DD'` strings.

Those are amount formats that are parsable:
- 1 000,00
- 1000.00
- 1,000.00
- -1 000,00
- -1000.00
- -1,000.00

---

## `Budget`

`Budget` is a static entry point — you never instantiate it. It holds a single
process-wide calculator proxy.

### Setup

- **`Budget.AddAccount(name: str) -> Account`**
  Register a new account. The returned `Account` is used to attach balances,
  recurring flows and one-off transactions.

- **`Budget.SetProcessingDaysCount(count)`**
  How many days into the future the engine simulates day-by-day. Pass either
  an `int` (raw day count) or `AfterPaychecks(n)` to anchor on the *n*-th
  upcoming paycheck.

### History markers

- **`Budget.Day(dt: str)`**
  Mark the calendar day currently being processed. Returns a `DayOperations`
  callable — call it with `()` to close the day.

- **`Budget.End(dt: str = '')`**
  Mark the end of the recorded history. Everything after this point is
  projected, not observed. If `dt` is omitted, defaults to yesterday.

### Running the calculation

- **`Budget.CalculateBudgetMultipleFiles(budgetFilename, accountsFilename, configFilename, historyFilename, outputBudgetFilename, *, outputdir=None)`**
  Four-file mode driver.

- **`Budget.Calculate(outputBudgetFilename, *, outputdir=None)`**
  Single-file mode driver.

- **`Budget.Cleanup()`**
  Drop the current calculator proxy so the next call re-initializes. Useful in
  test suites that run multiple budgets in one process.

### Transfers

- **`Budget.Transfer(title, accountFrom, accountTo, amount)`**
  One-off transfer between two accounts.

- **`Budget.TransferBetween(title, accountFrom, accountTo, freq, amount)`**
  Recurring transfer on a `Frequence`.

---

## `Account`

Returned by `Budget.AddAccount(...)`. All methods are chainable via the account
variable — you configure the account once, then reference it wherever needed.

### Balance & metadata

- **`EntryBalance(balance: str)`** — starting balance on day 0 of the history.
- **`SoftMinimum(softMin: str)`** — threshold below which cells are visually
  flagged (soft-warning) in the output.
- **`Forecast(count)`** — how far to project this account into the future.
  Accepts an `int` (months) or `AfterPaychecks(n)`.
- **`SetVariableName(name)`** — internal; the four-file driver calls this for
  you so the report labels match your Python variable names.

### Recurring flows

- **`Pay(title, freq: Frequence, amount)`** — recurring income (e.g. salary).
- **`Bill(title, freq: Frequence, amount: str)`** — recurring expense.

### One-off operations

- **`Payment(title, amount: str)`** — one-off expense recorded on the current
  `Budget.Day(...)`.
- **`Paycheck(title, amount: str)`** — one-off income on the current day.

---

## Frequency classes

Used with `Pay`, `Bill`, and `Budget.TransferBetween`.

- **`Monthly(when)`** — every month on day `when` (int, or a `DayOfYear`).
- **`BiWeekly(start: str)`** — every 14 days from `start` (`'YYYY-MM-DD'`).
- **`Yearly(start)`** — once a year starting at `start`.

## Amount / date helpers

- **`DayOfYear(when: str)`** — a specific month+day marker (e.g. for a yearly
  bill).
- **`Before(when, amount: str)`** — an amount that applies **before** a given
  date.
- **`Then(amount: str)`** — the follow-up amount used together with `Before`
  to express "X until date, then Y after".
- **`MultipleAmounts(*args)`** — bundle several amount variations.
- **`DualTitle(pos, neg)`** — different display titles for positive vs.
  negative values of the same line.
- **`OverUntilNextPay(maxBalance: str, *, round='0')`** — cap that spills
  overflow to the next paycheck cycle; optional rounding step.
- **`AfterPaychecks(count: int)`** — used with `SetProcessingDaysCount` or
  `Forecast` to express "*n* paychecks from now" instead of a raw day count.

---

## Output

Both `Calculate` and `CalculateBudgetMultipleFiles` accept:

- The output filename (positional).
- **`outputdir=<path>`** (keyword) — directory to write the report into. When
  omitted, files land next to the budget script. The engine also rewrites the
  history file into `outputdir` in four-file mode.
