from budgeter import Budget

if __name__ == "__main__":
    Budget.CalculateBudgetMultipleFiles('accounts.py', \
                                        'config.py', \
                                        'history.py', \
                                        'budget.html')
