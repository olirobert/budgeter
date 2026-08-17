# budgeter

budgeter is a Python library for planning your personal budget.
Its purpose is to determine whether you have enough money, between two paychecks, to pay the upcoming bills.
Then, as a second step, to determine how much surplus is available before the next paycheck.

It produces a calendar of operations as well as a forecast of the account balances.

> **Note:** This project is a work in progress and may not work in all environments. It has been designed and tested using the `en-CA` culture.

Dates are entered as `yyyy-mm-dd` strings. Amounts are entered as strings, e.g. `1000.00`; several other formats are also supported, see [`docs/budget.md`](docs/budget.md).

## Design

At run time, the following information is collected:
- the accounts
- the recurring paychecks, expenses and transfers
- the upcoming operations and the manually entered operations

At the end of the script, the budget generation is executed.
This execution produces 2 files:
- Creates the HTML page containing the calendar of the upcoming days as well as the forecast
- Creates a `lastupdate.txt` file

At run time, the library also updates the history file to include the upcoming operations.

### Upcoming operations in the history

This feature is an important part of this library. It lets you adjust the upcoming operations individually, for a better representation of the amounts to come and, if used, to better determine the shortfall or the surplus between the next paychecks. This is particularly useful for amounts that vary from one time to the next, for example the credit card amount.

It works starting from the line `Budget.End('yyyy-mm-dd')`, which tells the system the date up to which it should assume the recurring operations have already been written out as upcoming operations. That way, during the calculation, it will only include the recurring operations after this reference date.

At the end of the processing, this library determines whether the reference date must be adjusted to reflect the configured number of paychecks: the new date will be the day before the N+1<sup>th</sup> paycheck, either already written in the history file or derived from the recurring operations.

Example:
- For a bi-weekly paycheck starting on 01/01
- The calendar must include 3 paychecks
- The paychecks of 01/01 and 01/15 are already written in the history

While processing this budget, the missing 3rd paycheck is determined to be 01/29.
The 4th paycheck being 02/12, we determine that the budget must cover up to the day before this 4th paycheck, that is 02/11.

The script will then add the upcoming operations directly into the history file, based on the recurring operations, up to this new reference date.
These new entries are inserted just before the `Budget.End` line.
Then the `Budget.End` command is modified to indicate the new reference date.

Note: it is possible to use `Budget.End()`. With this variant, the script uses yesterday's date as the reference date. This variant allows a first run without having to pre-fill the history file with upcoming operations.

### Output

#### HTML page

This page contains:
- A table containing, for each day computed in the budget, the balance as well as all the operations of each account.
- A table containing, for each month computed in the forecast, the minimum and maximum balance of each account.

#### lastupdate.txt

This file contains the date and time of the generation. This file lets VS Code detect a change even when none of the budget files changed.

### 4 files vs 1 file

The initial design of this library was to use a standalone VS Code as the graphical interface. The "script" was then split into 3 Python files, plus a fourth one as the execution entry point.
Under the hood, this entry point ran the 3 Python files to configure the budget and produce an HTML page.
This HTML page was displayed in VS Code through the `Live Preview` extension.

The ability to have the budget in a single script was added afterwards, for convenience.

## Usage

See the [`examples`](examples) directory for a working budget, in both the single-file and 4-files layouts described above.

A convenient way to use budgeter is to open the example folder in VS Code and open `budget.html` with the `Live Preview` extension. Running `budget.py` regenerates the HTML page, and Live Preview refreshes it automatically, giving you a UI to edit the budget and see the result.

See [`docs/budget.md`](docs/budget.md) for the full list of classes, frequencies and `Budget` methods.

## Prerequisites

Requires Python >= 3.8 and the .NET runtime (loaded through `pythonnet`'s `coreclr` host).

## Install

```bash
pip install .
```

## Running the tests

```bash
python -m unittest tests.test_budget
```

## Third-party licenses

This project depends on `pythonnet` and `Scriban`. See [`THIRD-PARTY-NOTICES.md`](THIRD-PARTY-NOTICES.md) for their license texts.
