import inspect
import os
import sys
import importlib.util
import importlib.machinery
import datetime
from .BudgetCalculatorLib import BudgetCalculatorLib

budgetCalculatorProxy = BudgetCalculatorLib.get_proxy()


def _GetLineInfo(methodName: str):
    stack = inspect.stack()

    previousFrame = stack[1]
    if previousFrame.function != methodName:
        return None

    callerFrame = stack[2]
    return (callerFrame.filename, callerFrame.lineno)



def _CreatePackage(dir: str, name: str):
    pkg_spec = importlib.machinery.ModuleSpec(name, loader=None, is_package=True)
    pkg_spec.submodule_search_locations = [dir]
    pkg_module = importlib.util.module_from_spec(pkg_spec)
    pkg_module.__path__ = [dir]
    sys.modules[name] = pkg_module



def _RunScript(path: str, name: str, pkg_name: str):
    module_name = f"{pkg_name}.{name}"

    spec = importlib.util.spec_from_file_location(module_name, path)
    script_module = importlib.util.module_from_spec(spec)
    sys.modules[module_name] = script_module
    spec.loader.exec_module(script_module)

    return script_module



class Frequence:
    def __init__(self):
        self._handle = -1



class Monthly(Frequence):
    def __init__(self, when):
        global budgetCalculatorProxy
        self._handle = budgetCalculatorProxy.CreateMonthlyFreq(when)



class BiWeekly(Frequence):
    def __init__(self, start):
        global budgetCalculatorProxy
        self._handle = budgetCalculatorProxy.CreateBiWeeklyFreq(start)



class Yearly(Frequence):
    def __init__(self, start):
        global budgetCalculatorProxy
        self._handle = budgetCalculatorProxy.CreateYearlyFreq(start)



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



class DualTitle:
    def __init__(self, pos: str, neg: str):
        self.posTitle = pos
        self.negTitle = neg



class Account:
    def __init__(self, handle):
        self._handle = handle


    def SetVariableName(self, name: str):
        global budgetCalculatorProxy
        budgetCalculatorProxy.SetAccountVariableName(self._handle, name)


    def SoftMinimum(self, softMin: str):
        global budgetCalculatorProxy
        budgetCalculatorProxy.SetAccountSoftMinimum(self._handle, softMin)
        

    def EntryBalance(self, balance: str):
        global budgetCalculatorProxy
        budgetCalculatorProxy.SetAccountEntryBalance(self._handle, balance)

    
    def Forecast(self, count):
        global budgetCalculatorProxy
        if isinstance(count, AfterPaychecks):
            paychecks = count
            budgetCalculatorProxy.SetAccountForecastDaysCount(self._handle, paychecks.count * -1)
        else:
            budgetCalculatorProxy.SetAccountForecastDaysCount(self._handle, count)        


    def Bill(self, title: str, freq: Frequence, amount: str):
        global budgetCalculatorProxy
        budgetCalculatorProxy.AddRecurrentBillOnAccount(self._handle, title, freq._handle, amount)


    def Pay(self, title: str, freq: Frequence, amount):
        global budgetCalculatorProxy
        budgetCalculatorProxy.AddRecurrentPayOnAccount(self._handle, title, freq._handle, amount)


    def Payment(self, title: str, amount: str):
        global budgetCalculatorProxy
        budgetCalculatorProxy.AddPaymentOnAccount(self._handle, title, amount)


    def Paycheck(self, title: str, amount: str):
        global budgetCalculatorProxy
        budgetCalculatorProxy.AddPaycheckOnAccount(self._handle, title, amount)



class DayOperations:
    def __call__(self, *args):
        global budgetCalculatorProxy
        budgetCalculatorProxy.DayCompleted()



class OverUntilNextPay:
    def __init__(self, maxBalance: str, **kwargs):
        self.maxBalance = maxBalance
        self.round = '0'

        for key, value in kwargs.items():
            if key == 'round':
                self.round = value



class AfterPaychecks:
    def __init__(self, count: int):
        self.count = count



def _CalculateBudget(accounts, budgetDir, historyFilename, outputBudgetFilename, *args, **kwargs):
    global budgetCalculatorProxy

    for name in dir(accounts):
        attr = getattr(accounts, name)
        if isinstance(attr, Account):
            attr.SetVariableName(name)

    outputDir = budgetDir
    for key, value in kwargs.items():
        if key == 'outputdir':
            outputDir = value

    outputHistoryFile = os.path.join(outputDir, historyFilename)
    outputBudgetFile = os.path.join(outputDir, outputBudgetFilename)

    budgetCalculatorProxy.Update(outputHistoryFile, outputBudgetFile)



def _CheckInitialized():
    global budgetCalculatorProxy
    if budgetCalculatorProxy is not None:
        return
    
    frame = inspect.stack()[2]
    filename = frame.filename

    budgetCalculatorProxy = BudgetCalculatorLib.initialize(filename)



class Budget:

    @staticmethod
    def Cleanup():
        global budgetCalculatorProxy
        budgetCalculatorProxy = None


    @staticmethod
    def CalculateBudgetMultipleFiles(accountsFilename: str, configFilename: str, historyFilename: str, outputBudgetFilename: str, *args, **kwargs):
        global budgetCalculatorProxy

        frame = inspect.stack()[1]
        (budgetDir, _) = os.path.split(frame.filename)
        budgetDir = os.path.abspath(budgetDir)

        accountsFile = os.path.join(budgetDir, accountsFilename)
        configFile = os.path.join(budgetDir, configFilename)
        historyFile = os.path.join(budgetDir, historyFilename)

        budgetCalculatorProxy = BudgetCalculatorLib.initialize(historyFile)

        packageName = "budgetPkg"
        _CreatePackage(budgetDir, packageName)

        accounts = _RunScript(accountsFile, 'accounts', packageName)

        _RunScript(configFile, 'config', packageName)

        _RunScript(historyFile, 'history', packageName)

        _CalculateBudget(accounts, budgetDir, historyFilename, outputBudgetFilename, *args, **kwargs)


    @staticmethod
    def Calculate(outputBudgetFilename: str, *args, **kwargs):
        frame = inspect.stack()[1]
        accounts = inspect.getmodule(frame[0])

        (dir, filename) = os.path.split(frame.filename)
        _CalculateBudget(accounts, dir, filename, outputBudgetFilename, *args, **kwargs)


    @staticmethod
    def AddAccount(name: str) -> Account:
        _CheckInitialized()
        global budgetCalculatorProxy

        handle = budgetCalculatorProxy.AddAccount(name)
        account = Account(handle)
        return account


    @staticmethod
    def Day(dt: str):
        _CheckInitialized()
        global budgetCalculatorProxy
        budgetCalculatorProxy.MarkDay(dt)
        return DayOperations()


    @staticmethod
    def End(dt: str = ''):
        _CheckInitialized()
        global budgetCalculatorProxy

        if dt == '':
            today = datetime.date.today()
            yest = today - datetime.timedelta(days = 1)
            endDt = yest.__str__()
        else:
            endDt = dt

        file, lineno = _GetLineInfo('End')
        budgetCalculatorProxy.MarkEnd(endDt, file, lineno)


    @staticmethod
    def SetProcessingDaysCount(count):
        _CheckInitialized()
        global budgetCalculatorProxy
        if isinstance(count, AfterPaychecks):
            paychecks = count
            budgetCalculatorProxy.SetProcessingDaysCount(paychecks.count * -1)
        else:
            budgetCalculatorProxy.SetProcessingDaysCount(count)


    @staticmethod
    def TransferBetween(title, accountFrom: 'Account', accountTo: 'Account', freq: Frequence, amount):
        _CheckInitialized()
        global budgetCalculatorProxy
        budgetCalculatorProxy.AddRecurrentTransferOnAccount(title, accountFrom._handle, accountTo._handle, freq._handle, amount)


    @staticmethod
    def Transfer(title, accountFrom: 'Account', accountTo: 'Account', amount):
        _CheckInitialized()
        global budgetCalculatorProxy
        budgetCalculatorProxy.AddTransferOnAccount(title, accountFrom._handle, accountTo._handle, amount)
