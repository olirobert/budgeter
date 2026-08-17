from budgeter import *

bank = Budget.AddAccount('National First Bank')

Budget.SetProcessingDaysCount(AfterPaychecks(4))

bank.SoftMinimum('100')

bank.Forecast(AfterPaychecks(26))

bank.Pay('Job', BiWeekly('2025-7-16'), '1,000.00')

bank.Bill('Mortgage', Monthly(1), '150.00')

bank.EntryBalance('1.23')

###

Budget.End('2026-01-31')


def execute(*args, **kwargs):
    Budget.Calculate('budget.html', *args, **kwargs)

if __name__ == "__main__":
    raise Exception("Must be run from tests")
