using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;

namespace Updater2
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        string exepath = Directory.GetParent(Assembly.GetExecutingAssembly().Location).FullName;
        public App()
        {
        this.Exit += (s, e) =>
                {
                    if (System.IO.File.Exists("Updater NEW.exe"))
                    {
                        StreamWriter w = new StreamWriter(exepath + "\\UpdateReplacer.bat");
                        w.WriteLine("@echo off"); // Turn off echo
                        w.WriteLine("@echo Attempting to replace updater, please wait...");
                        w.WriteLine("@ping -n 4 127.0.0.1 > nul"); //Its silly but its the most compatible way to call for a timeout in a batch file, used to give the main updater time to cleanup and exit.
                        w.WriteLine("@del \"" + exepath+ "\\Updater.exe" + "\"");
                        w.WriteLine("@ren \"" + exepath+ "\\Updater NEW.exe" + "\" \"Updater.exe\""); 
                        w.WriteLine("@DEL \"%~f0\""); // Attempt to delete myself without opening a time paradox.
                        w.Close();

                        System.Diagnostics.Process.Start(exepath + "\\UpdateReplacer.bat");
                    }
               };
        }
    }
}
