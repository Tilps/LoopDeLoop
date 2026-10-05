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

        Thread? runner;

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
            //Random rnd = new Random();
            ProcessPuzzles();
            while (true)
            {
                //MakePuzzle(rnd);
                /*
                SelfTest(2, 2, MeshType.Square, true);
                SelfTest(4, 4, MeshType.Square, true);
                SelfTest(2, 2, MeshType.Triangle, true);
                SelfTest(2, 2, MeshType.Octagon, true);
                SelfTest(10, 10, MeshType.Square, false);
                SelfTest(5, 5, MeshType.Triangle, false);
                SelfTest(5, 5, MeshType.Octagon, false);
                SelfTest(20, 14, MeshType.Square, false);
                */
            }
        }

        private void ProcessPuzzles()
        {
            string[] puzzleDirs = Directory.GetDirectories(".");
            foreach (string puzzleDir in puzzleDirs)
            {
                string[] files = Directory.GetFiles(puzzleDir, "*.loop");
                foreach (string file in files)
                {
                    if (File.Exists(Path.ChangeExtension(file, "done"))) continue;
                    string[] lines = File.ReadAllLines(file);
                    MeshType type = (MeshType)Enum.Parse(typeof(MeshType), lines[0]);
                    Mesh mesh = new Mesh(0, 0, type);
                    if (!mesh.LoadFromText(lines))
                        throw new InvalidOperationException();
                    mesh.SetRatingCodeOptions("SC+EOMP+");
                    mesh.IterativeSolverDepth = int.MaxValue;
                    List<IAction> actions = new List<IAction>();
                    if (!mesh.PerformStart(actions))
                    {
                        throw new InvalidOperationException();
                    }
                    mesh.PerformEndIfPossible(actions);
                    int depth = 0;
                    while (true)
                    {
                        using (TextWriter writer = File.CreateText(Path.ChangeExtension(file, "loop"+depth)))
                        {
                            mesh.Save(writer);
                        }
                        int[,] scores = new int[mesh.Edges.Count, 2];
                        List<IAction> action1 = new List<IAction>();
                        List<IAction> action2 = new List<IAction>();
                        List<IAction> realActions = new List<IAction>();
                        int bestScore = 0;
                        int bestIndex = -1;
                        for (int i = 0; i < mesh.Edges.Count; i++)
                        {
                            IAction trial = new SetAction(mesh, i, EdgeState.Filled);
                            if (mesh.IsPointlessTrial(trial))
                            {
                                continue;
                            }
                            action1.Clear();
                            action2.Clear();
                            realActions.Clear();
                            bool success1;
                            bool success2;
                            if (!mesh.PerformBasicTrial(trial, action1, action2, realActions, out success1, out success2))
                            {
                                throw new InvalidOperationException();
                            }
                            int score = 0;
                            int progressScore = 0;
                            foreach (IAction action in realActions)
                            {
                                if (action is SetAction)
                                {
                                    score += 2;
                                    progressScore += 2;
                                } else if (action is ColorJoinAction)
                                {
                                    score += 2;
                                } else if (action is EdgeRestrictionAction)
                                {
                                    score += 1;
                                }
                            }
                            mesh.Unperform(realActions);

                            if (progressScore == 0)
                            {
                                score = 0;
                            }
                            if (score > bestScore)
                            {
                                bestScore = score;
                                bestIndex = i;
                            }
                            if (success1 && success2)
                            {
                                scores[i, 0] = scores[i, 1] = score;
                            } else if (success1)
                            {
                                scores[i, 1] = score;
                            }
                            else
                            {
                                scores[i, 0] = score;
                            }
                        }
                        if (bestScore == 0) break;
                        StringBuilder output = new StringBuilder();
                        for (int i = 0; i < mesh.Edges.Count; i++)
                        {
                            for (int j = 0; j < 2; j++)
                            {
                                if (output.Length != 0) output.Append(',');
                                output.Append(scores[i, j]);
                            }
                        }
                        File.WriteAllText(Path.ChangeExtension(file, "scores"+depth), output.ToString());
                        IAction progressAction = new SetAction(mesh, bestIndex, EdgeState.Filled);
                        bool ignore1, ignore2;
                        action1.Clear();
                        action2.Clear();
                        realActions.Clear();
                        mesh.PerformBasicTrial(progressAction, action1, action2, realActions, out ignore1, out ignore2);
                        mesh.PerformEndIfPossible(realActions);
                        depth++;
                    }
                    File.WriteAllText(Path.ChangeExtension(file, "done"), string.Empty);
                }
            }
        }

        private void MakePuzzle(Random rnd)
        {
            List<string> codes = new List<string> { "S", "SC", "SCO", "SCOM", "SC+OM", "SC+EOM", "SC+EOMP", "SC+EOMP+", "FC+EOMP+" };
            List<string> extraCodes = new List<string> { "SI", "SIC", "SIO", "SM", "SIM", "SOM", "SP", "SP+", "SC+EP", "SE", "SEP", "SC+E", "SCE", "SCP" };
            Mesh m = new Mesh(10, 10, MeshType.Square);
            int choice = rnd.Next(10);
            if (choice < 6)
            {
                string code = codes[rnd.Next(codes.Count)];
                if (choice < 5) code = code + choice.ToString();
                m.SetRatingCodeOptions(code);
            } else if (choice < 9)
            {
                string code = extraCodes[rnd.Next(extraCodes.Count)];
                if (choice < 8) code = code + (choice-5).ToString();
                m.SetRatingCodeOptions(code);
            }
            else
            {
                m.SetRatingCodeOptions("F");
                m.ContaminateFullSolver = true;
            }
            m.GenerateBoringFraction = 0.05*rnd.Next(1, 10);
            m.GenerateLengthFraction = 0.5 + 0.05*rnd.Next(6);
            m.Generate();
            DateTime now = DateTime.UtcNow;
            string dirName = now.ToString("yyyyMMdd_HH");
            Directory.CreateDirectory(dirName);
            using (TextWriter writer = File.CreateText(dirName + Path.DirectorySeparatorChar + "MakePuzzle-" + now.ToString("o").Replace(':','_') + ".loop"))
            {
                m.Save(writer);
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