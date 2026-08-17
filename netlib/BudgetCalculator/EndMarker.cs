namespace BudgetCalculator
{
    internal class EndMarker
    {
        public DateTime BeginRecurrentDate { get; private set; }

        private FileManipulator.Line m_linePointer;

        public FileManipulator.Line LinePointer => m_linePointer;

        public EndMarker(DateTime dt, FileManipulator.Line linePointer)
        {
            BeginRecurrentDate = dt;
            m_linePointer = linePointer;
        }

        public void UpdateDateInFile(DateTime dt)
        {
            string line = m_linePointer.String;

            if (line.Contains("Budget.End"))
            {
                line = line.ReplaceBy(@"Budget.End\(.*\)", $"Budget.End('{dt:d}')");
            }
            else
            {
                m_linePointer.ForceSpaceForEndLine();

                m_linePointer.ForceNewOperationsHeader();

                line = m_linePointer.String.Trim() + $"Budget.End('{dt:d}')" + Environment.NewLine;
            }

            m_linePointer.String = line;
        }
    }
}
