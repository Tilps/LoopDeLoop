using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using System.Linq;
using System.IO;

namespace LoopDeLoop
{
    public partial class HelpForm : Form
    {
        public HelpForm()
        {
            InitializeComponent();
        }

        private void HelpForm_Load(object sender, EventArgs e)
        {
            textHelp.Select(0, 0);
        }

        Thread runner;

        private void button1_Click(object sender, EventArgs e)
        {
            if (runner == null)
            {
                label1.Text = "Running Self Test";
                textHelp.Clear();
                runner = new Thread(SelfTest);
                runner.IsBackground = true;
                runner.Start();
            }
            else
            {
                try
                {
                    lock (invokeLock)
                    {
                        runner.Abort();
                    }
                }
                catch
                {
                }
                runner = null;
                label1.Text = "Self Test Stopped.";
            }
        }

        private void SelfTest()
        {
            while (true)
            {
                SelfTest(2, 2, MeshType.Square, true);
                SelfTest(4, 4, MeshType.Square, true);
                SelfTest(2, 2, MeshType.Triangle, true);
                SelfTest(2, 2, MeshType.Octagon, true);
                SelfTest(10, 10, MeshType.Square, false);
                SelfTest(5, 5, MeshType.Triangle, false);
                SelfTest(5, 5, MeshType.Octagon, false);
                SelfTest(20, 14, MeshType.Square, false);
            }
        }
        int failCount = 0;

        private void SelfTest(int height, int width, MeshType meshType, bool extensive)
        {
            Output(string.Format("SelfTesting {0}, {1}, {2}, {3}", height, width, meshType, extensive));
            List<string> codes = new List<string>{"S", "SC", "SCO", "SCOM", "SC+OM", "SC+EOM", "SC+EOMP", "SC+EOMP+", "FC+EOMP+"};
            List<string> extraCodes = new List<string>{"SI", "SIC", "SIO", "SM", "SIM", "SOM", "SP", "SP+", "SC+EP", "SE", "SEP", "SC+E", "SCE", "SCP"};
            try
            {
                int solvedCount=0;
                for (int i = 0; i < (extensive ? 100 : 5); i++)
                {
                    Mesh m = new Mesh(width, height, meshType);
                    m.SetRatingCodeOptions("F");
                    m.GenerateBoringFraction = 0.5/height;
                    m.GenerateLengthFraction = 0.7;
                    m.Generate();
                    bool solved = false;
                    int lastDepth = int.MaxValue;
                    foreach (string code_in in codes)
                    {
                        bool solvedByCode = false;
                        for (int j = 0; j < (extensive ? height * height : 1); j++ )
                        {
                            string code = code_in;
                            if (extensive)
                                code += (j-1).ToString();
                            m.Clear();
                            m.SetRatingCodeOptions(code);
                            SolveState result = m.TrySolve();
                            if (result == SolveState.Solved)
                            {
                                solvedByCode = true;
                                if (!solved)
                                    solved = true;
                                if (extensive)
                                {
                                    if (j < lastDepth)
                                        lastDepth = j;
                                }
                            }
                            else
                            {
                                if (solved)
                                {
                                    if (!extensive || j > lastDepth || j == height*height-1)
                                    {
                                        Output(string.Format("Code {0} failed to solve puzzle solved with less powerful solvers.", code));
                                        using (TextWriter writer = File.CreateText("SelfTestOuput" + failCount + "-" + code + ".loop"))
                                        {
                                            m.Save(writer);
                                        }
                                    }
                                    failCount++;
                                }
                            }
                        }
                        if (solvedByCode)
                            solvedCount++;
                    }

                    m.FullClear();
                    m.SetRatingCodeOptions("S");
                    m.Generate();
                    foreach (string code in codes.Concat(extraCodes))
                    {
                        m.Clear();
                        m.SetRatingCodeOptions(code);
                        SolveState result = m.TrySolve();
                        if (result != SolveState.Solved)
                        {
                            Output(string.Format("Code {0} failed to solve puzzle generated with less powerful generator.", code));
                            using (TextWriter writer = File.CreateText("SelfTestOuput" + failCount + "-" + code + ".loop"))
                            {
                                m.Save(writer);
                            }
                            failCount++;
                        }
                    }
                }
                Output(string.Format("{0} codes solved {1} full generator puzzles {2} times.", codes.Count, extensive ? 100 : 5, solvedCount));
            }
            catch (Exception ex)
            {
                if (!(ex is ThreadAbortException))
                {
                    Output(string.Format("Failure {0}", ex));
                }
            }
        }
        private object invokeLock = new object();
        delegate void Printer(string output);
        private void Output(string p)
        {
            if (this.InvokeRequired)
            {
                bool lockTaken = Monitor.TryEnter(invokeLock);
                try
                {
                    if (lockTaken)
                    {
                        Invoke(new Printer(Output), new object[] { p });
                    }
                }
                finally
                {
                    if (lockTaken)
                        Monitor.Exit(invokeLock);
                }
                return;
            }
            textHelp.AppendText(p+Environment.NewLine);
        }
    }
}