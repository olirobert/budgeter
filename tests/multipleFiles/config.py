from .accounts import *
from budgeter import *

Budget.SetProcessingDaysCount(AfterPaychecks(4))

bank.SoftMinimum('100')

bank.Forecast(AfterPaychecks(26))

bank.Pay('Job', BiWeekly('2025-7-16'), '1,000.00')

bank.Bill('Mortgage', Monthly(1), '150.00')
