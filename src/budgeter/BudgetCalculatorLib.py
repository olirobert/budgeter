import os

from pythonnet import load

load("coreclr")

import clr

_dllPath = os.path.join(
    os.path.dirname(os.path.abspath(__file__)),
    r"BudgetCalculator\BudgetCalculator.dll",
)

clr.AddReference(_dllPath)

from BudgetCalculator import BudgetProcessor

_proxy = None


class BudgetCalculatorLib:

    @staticmethod
    def get_proxy():
        global _proxy
        return _proxy

    @staticmethod
    def initialize(historyFile: str):
        global _proxy
        _proxy = BudgetProcessor(historyFile)
        return _proxy
