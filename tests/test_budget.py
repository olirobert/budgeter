import unittest
import os
import sys
import importlib.util
import importlib.machinery
from budgeter import Budget

def OverrideNow(now: str):
    import clr
    from BudgetCalculator import CDateTime

    return CDateTime.Override(now)



def prepareTestFolders(files: str):
    testDir = os.path.dirname(os.path.abspath(__file__))
    testSrcDir = os.path.join(testDir, files)
    outputDir = os.path.join(testDir, files, '.test_output')

    if not os.path.isdir(outputDir):
        os.makedirs(outputDir)

    return (testSrcDir, outputDir)



def loadModule(path: str, name: str):
    dir = os.path.dirname(path)
    module_name = "module_" + name

    pkg_spec = importlib.machinery.ModuleSpec(module_name, loader=None, is_package=True)
    pkg_spec.submodule_search_locations = [dir]
    pkg_module = importlib.util.module_from_spec(pkg_spec)
    pkg_module.__path__ = [dir]
    sys.modules[name] = pkg_module

    spec = importlib.util.spec_from_file_location(module_name, path)
    module = importlib.util.module_from_spec(spec)
    sys.modules[module_name] = module
    spec.loader.exec_module(module)
    return module



def ValidateFile(validFile: str, outputFile: str):
    with open(validFile, "rb") as f:
        expected = f.read()
    with open(outputFile, "rb") as f:
        actual = f.read()

    if expected != actual:
        raise AssertionError(f"'{os.path.basename(outputFile)}' contents differ from it valid version")



def ValidateOutput(files: str, historyFile: str):
    testDir = os.path.dirname(os.path.abspath(__file__))
    testSrcDir = os.path.join(testDir, files)
    outputDir = os.path.join(testDir, files, '.test_output')

    ValidateFile(os.path.join(testSrcDir, 'budget.html.valid'), os.path.join(outputDir, 'budget.html'))
    ValidateFile(os.path.join(testSrcDir, historyFile + '.valid'), os.path.join(outputDir, historyFile))



class TestBudget(unittest.TestCase):
    def test_multiple_files(self):
        try:
            tempNow = OverrideNow('2026-02-01')

            (testSrcDir, outputDir) = prepareTestFolders('multipleFiles')

            budget_module = loadModule(os.path.join(testSrcDir, "budget.py"), "multipleFiles")

            budget_module.execute(outputdir=outputDir)

            ValidateOutput('multipleFiles', 'history.py')

        finally:
            tempNow.Dispose()
            Budget.Cleanup()


    def test_single_file(self):
        try:
            tempNow = OverrideNow('2026-02-01')

            (testSrcDir, outputDir) = prepareTestFolders('singleFile')

            budget_module = loadModule(os.path.join(testSrcDir, "budget.py"), "singleFile")

            budget_module.execute(outputdir=outputDir)

            ValidateOutput('singleFile', 'budget.py')

        finally:
            tempNow.Dispose()
            Budget.Cleanup()


if __name__ == "__main__":
    unittest.main()
