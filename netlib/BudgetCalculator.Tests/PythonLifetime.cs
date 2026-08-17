using Python.Runtime;

[assembly: AssemblyFixture(typeof(BudgetCalculator.Tests.PythonLifetime))]

namespace BudgetCalculator.Tests
{
    public sealed class PythonLifetime : IDisposable
    {
        public PythonLifetime()
        {
            Runtime.PythonDLL = "python314.dll";
        }

        public void Dispose()
        {
            if (PythonEngine.IsInitialized)
            {
                PythonEngine.Shutdown();
            }
        }
    }
}
