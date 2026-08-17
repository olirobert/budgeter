using System.Text;

namespace BudgetCalculator
{
    internal class FileManipulator
    {
        public class Line
        {
            private FileManipulator m_owner;
            private int m_lineNo;

            public Line(FileManipulator owner, int lineNo)
            {
                m_owner = owner;
                m_lineNo = lineNo;
            }

            public string String
            {
                get
                {
                    return m_owner.m_lines[m_lineNo];
                }

                set
                {
                    string oldLine = m_owner.m_lines[m_lineNo];
                    if (oldLine != value)
                    {
                        m_owner.m_lines[m_lineNo] = value;
                        m_owner.m_dirty = true;
                    }
                }
            }

            public void InsertLinesBefore(string[] newLines, string lineSep)
            {
                m_owner.InsertLinesBefore(m_lineNo, newLines, lineSep);
            }

            public void ForceSpaceForEndLine()
            {
                int count = 0;
                for (int i = m_lineNo; i > 0; i--)
                {
                    string line = m_owner.m_lines[i].Trim(' ');
                    string lineNoCR = line.Replace("\n", "").Replace("\r", "");
                    if (lineNoCR.Length > 0)
                    {
                        break;
                    }
                    else if (line.Length > 0)
                    {
                        count++;
                    }
                }
                if (count >= 1)
                    return;

                m_owner.m_lines[m_lineNo] = m_owner.m_lines[m_lineNo].Trim() + Environment.NewLine;
                m_owner.InsertLinesBefore(m_lineNo + 1, new string[] { "" }, "");
                m_lineNo++;
            }

            public void ForceNewOperationsHeader()
            {
                bool headerFound = false;
                bool haveSpace = false;
                for (int i = m_lineNo; i > 0; i--)
                {
                    string line = m_owner.m_lines[i].Trim();
                    if (line.Length > 0)
                    {
                        if (line.StartsWith("###"))
                        {
                            headerFound = true;
                            haveSpace = i < m_lineNo - 1;
                        }
                        break;
                    }
                }

                if (!headerFound)
                {
                    m_owner.m_lines[m_lineNo] = "###" + Environment.NewLine;
                    m_owner.InsertLinesBefore(m_lineNo + 1, new string[] { "" }, "");
                    m_lineNo++;
                }

                if (!haveSpace)
                    ForceSpaceForEndLine();
            }

            public void AdjustLineNo(int from, int count)
            {
                if (m_lineNo >= from)
                    m_lineNo += count;
            }
        }

        private string m_path;
        private List<string> m_lines = new List<string>();
        private List<Line> m_linePointer = new List<Line>();

        private bool m_dirty = false;

        public string Path => m_path;

        public FileManipulator(string path)
        {
            m_path = path;

            string content = File.ReadAllText(path);

            int pos = 0;
            int oldPos = 0;
            while (pos != -1)
            {
                pos = content.IndexOf('\n', oldPos);
                if (pos == -1)
                    break;

                m_lines.Add(content.Substring(oldPos, pos - oldPos + 1));

                oldPos = pos + 1;
            }
            m_lines.Add(content.Substring(oldPos));
        }

        public void Save(string newFilePath)
        {
            if (!m_dirty && m_path == newFilePath)
                return;

            StringBuilder output = new StringBuilder();
            foreach (string line in m_lines)
            {
                output.Append(line);
            }

            File.WriteAllText(newFilePath, output.ToString());
        }

        public Line RegisterLine(int lineNo)
        {
            Line l = new Line(this, lineNo);

            m_linePointer.Add(l);

            return l;
        }

        public Line RegisterLastLine()
        {
            int index = m_lines.Count - 1;
            return RegisterLine(index);
        }

        private void InsertLinesBefore(int index, string[] newLines, string lineSep)
        {
            for (int i = newLines.Length - 1; i >= 0; i--)
            {
                if (index < m_lines.Count)
                    m_lines.Insert(index, $"{newLines[i]}{lineSep}");
                else
                    m_lines.Add($"{newLines[i]}{lineSep}");
            }

            foreach (Line l in m_linePointer)
            {
                l.AdjustLineNo(index, newLines.Length);
            }

            m_dirty = true;
        }
    }
}
