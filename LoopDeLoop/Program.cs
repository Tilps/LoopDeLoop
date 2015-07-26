using System;
using System.Collections.Generic;
using System.Windows.Forms;
using LoopDeLoop.Properties;
using System.Reflection;

namespace LoopDeLoop
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            if (Settings.Default.LastVersionSave == "0.0.0.0")
            {
                Settings.Default.Upgrade();
                Settings.Default.LastVersionSave = GetVersionNumber(); ;
                Settings.Default.Save();
            }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new LoopDeLoopForm());
        }

        private static string GetVersionNumber()
        {
            try
            {
                return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
            }
            catch
            {
                try
                {
                    object[] attribs = System.Reflection.Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyVersionAttribute), false);
                    foreach (AssemblyVersionAttribute attrib in attribs)
                    {
                        return attrib.Version.ToString();
                    }
                }
                catch
                {
                    return "Can't get version number.";
                }
            }
            return "No version number";
        }
    }
}