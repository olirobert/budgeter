from budgeter import Budget

def execute(*args, **kwargs):
    Budget.CalculateBudgetMultipleFiles('accounts.py', \
                                        'config.py', \
                                        'history.py', \
                                        'budget.html', 
                                        *args, **kwargs)

if __name__ == "__main__":
    raise Exception("Must be run from tests")
