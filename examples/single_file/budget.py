from budgeter import *

bank = Budget.AddAccount('National First Bank')
saving = Budget.AddAccount('Saving account')


Budget.SetProcessingDaysCount(AfterPaychecks(4))

bank.SoftMinimum('1000.00')

saving.Forecast(AfterPaychecks(26))

payFreq = BiWeekly('2026-7-16')
bank.Pay('Job', payFreq, '1,000.00')
Budget.TransferBetween(DualTitle('Saving', 'Overdraft'), bank, saving, payFreq, OverUntilNextPay('1100', round='100'))

bank.Bill('Mortgage', Monthly(1), '150,00')

bank.Bill('Insurance', Monthly(15), '50,00')

bank.Bill('City Taxes', Yearly('2023-2-10'), '596.87')


bank.EntryBalance('256.00')
saving.EntryBalance('1,881.29')

###

Budget.End()


if __name__ == "__main__":
    Budget.Calculate('budget.html')
