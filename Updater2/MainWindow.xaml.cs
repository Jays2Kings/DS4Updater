using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Shell;
using System.Xml;

namespace Updater2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        WebClient wc = new WebClient();
        protected string path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\DS4Tool";
        double version, newversion;
        string exepath = Directory.GetParent(Assembly.GetExecutingAssembly().Location).FullName;
        protected XmlDocument m_Xdoc = new XmlDocument();
        bool newupdate;

        public bool AdminNeeded()
        {
            try
            {
                File.WriteAllText(exepath + "\\test.txt", "test");
                File.Delete(exepath + "\\test.txt");
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                return true;
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            if (AdminNeeded())
                label1.Content = "Please re-run with admin rights";
            else
            {
                try { File.Delete(exepath + "\\Update.zip"); }
                catch { label1.Content = "Cannot access Update.zip at this time"; return; }
                Load();
                if (!File.Exists(exepath + "\\version.txt") && !File.Exists(path + "\\version.txt"))
                {
                    Uri urlv = new Uri("https://dl.dropboxusercontent.com/u/16364552/DS4Tool/newest%20version.txt");
                    //Sorry other devs, gonna have to find your own server
                    WebClient wc2 = new WebClient();
                    wc.DownloadFile(urlv, exepath + "\\version.txt");
                    if (double.TryParse(File.ReadAllText(exepath + "\\version.txt"), NumberStyles.Any, CultureInfo.InvariantCulture, out newversion))
                        if (newversion > version)
                            newupdate = true;
                        else
                            File.Delete(exepath + "\\version.txt");
                    else
                    {
                        label1.Content = "J2K has messed up the update, please contact him";
                        File.Delete(exepath + "\\version.txt");
                    }
                }
                else if (File.Exists(path + "\\version.txt"))
                {
                    if (double.TryParse(File.ReadAllText(path + "\\version.txt"), NumberStyles.Any, CultureInfo.InvariantCulture, out newversion))
                        if (newversion > version)
                            newupdate = true;
                }
                else if (File.Exists(exepath + "\\version.txt"))
                {
                    if (double.TryParse(File.ReadAllText(exepath + "\\version.txt"), NumberStyles.Any, CultureInfo.InvariantCulture, out newversion))
                        if (newversion > version)
                            newupdate = true;
                }

                if (newupdate)
                {
                    Uri url = new Uri("https://dl.dropboxusercontent.com/u/16364552/DS4Tool/DS4Tool%20-%20J2K%20%28v" + newversion + "%29.zip");
                    //Sorry other devs, gonna have to find your own server
                    sw.Start();
                    try { wc.DownloadFileAsync(url, exepath + "\\Update.zip"); }
                    catch (Exception e) { label1.Content = e.Message; }
                    wc.DownloadFileCompleted += wc_DownloadFileCompleted;
                    wc.DownloadProgressChanged += wc_DownloadProgressChanged;
                }
                else
                {
                    label1.Content = "DS4Tool is up to date";
                    try
                    {
                        File.Delete(path + "\\version.txt");
                        File.Delete(exepath + "\\version.txt");
                    }
                    catch { }
                    btnOpenDS4.IsEnabled = true;
                }
            }
        }

        public bool IsAdministrator()
        {
            var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        Stopwatch sw = new Stopwatch();
        private void wc_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            label2.Opacity = 1;
            double speed = e.BytesReceived / sw.Elapsed.TotalSeconds;
            double timeleft = (e.TotalBytesToReceive - e.BytesReceived) / speed;
            if (timeleft > 3660)
                label2.Content = (int)timeleft / 3600 + "h left";
            else if (timeleft > 90)
                label2.Content = (int)timeleft / 60 + "m left";
            else
                label2.Content = (int)timeleft + "s left";
            UpdaterBar.Value = e.ProgressPercentage;
            TaskbarItemInfo.ProgressValue = UpdaterBar.Value / 106d;
            string convertedrev, convertedtotal;
            if (e.BytesReceived > 1024 * 1024 * 5) convertedrev = (int)(e.BytesReceived / 1024d / 1024d) + "MB";
            else convertedrev = (int)(e.BytesReceived / 1024d) + "kB";
            if (e.TotalBytesToReceive > 1024 * 1024 * 5) convertedtotal = (int)(e.TotalBytesToReceive / 1024d / 1024d) + "MB";
            else convertedtotal = (int)(e.TotalBytesToReceive / 1024d) + "kB";
            label1.Content = "Downloading update: " + convertedrev + " / " + convertedtotal;
        }

        private void wc_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            sw.Reset();
            if (new FileInfo(exepath + "\\Update.zip").Length > 0)
            {
                Process[] processes = Process.GetProcessesByName("DS4Tool");
                Process[] processes2 = Process.GetProcessesByName("DS4Windows");
                label1.Content = "Download Complete";
                if (processes.Length > 0 || processes2.Length > 0)
                    if (MessageBox.Show("It will be closed to contine this update.", "DS4Tool/DS4Windows is still running", MessageBoxButton.OKCancel, MessageBoxImage.Exclamation) == MessageBoxResult.OK)
                    {
                        foreach (Process p in processes)
                            p.Kill();
                        foreach (Process p in processes2)
                            p.Kill();
                        System.Threading.Thread.Sleep(5000);
                    }
                    else
                        this.Close();
                label1.Content = "Deleting old files";
                while (processes.Length + processes2.Length > 0)
                {
                    label1.Content = "Waiting for DS4Windows to close";
                    processes = Process.GetProcessesByName("DS4Tool");
                    processes2 = Process.GetProcessesByName("DS4Windows");
                    System.Threading.Thread.Sleep(10);
                }
                label2.Opacity = 0;
                label1.Content = "Deleting old files";
                UpdaterBar.Value = 102;
                TaskbarItemInfo.ProgressValue = UpdaterBar.Value / 106d;
                try
                {
                    File.Delete(exepath + "\\DS4Windows.exe");
                    File.Delete(exepath + "\\DS4Tool.exe");
                    File.Delete(exepath + "\\DS4Control.dll");
                    File.Delete(exepath + "\\DS4Library.dll");
                    File.Delete(exepath + "\\HidLibrary.dll");
                    File.Delete(exepath + "DS4Updater NEW.exe");
                }
                catch { }
                label1.Content = "Installing new files";
                UpdaterBar.Value = 104;
                TaskbarItemInfo.ProgressValue = UpdaterBar.Value / 106d;
                //string zipPath = exepath + "\\Update.zip";
                /*try
                {
                    // Opens existing zip file
                    ZipStorer zip = ZipStorer.Open(exepath + "\\Update.zip", FileAccess.Read);
                    // Read all directory contents
                    List<ZipStorer.ZipFileEntry> dir = zip.ReadCentralDir();
                    // Extract all files in target directory
                    foreach (ZipStorer.ZipFileEntry entry in dir)
                        zip.ExtractFile(entry, exepath + "\\" + System.IO.Path.GetFileName(entry.FilenameInZip));
                    zip.Close();
                }
                catch (InvalidDataException)
                { }*/
                try { ZipFile.ExtractToDirectory(exepath + "\\Update.zip", exepath);}
                catch (IOException) { }
                try
                {
                    File.Delete(exepath + "\\version.txt");
                    File.Delete(path + "\\version.txt");
                }
                catch { }
                if (File.Exists(exepath + "\\DS4Windows.exe") || File.Exists(exepath + "\\DS4Tool.exe"))
                {
                    File.Delete(exepath + "\\Update.zip");
                    label1.Content = "DS4Windows has been updated to v" + newversion;
                }
                else
                    label1.Content = "Could not unpack zip, please manually unzip";
                Save();
                UpdaterBar.Value = 106;
                TaskbarItemInfo.ProgressState = TaskbarItemProgressState.None;
                btnOpenDS4.IsEnabled = true;
            }
            else
            {
                label1.Content = "Could not download update";
                btnOpenDS4.IsEnabled = true;
            }
        }
        protected string m_Profile = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\DS4Tool\\Profiles.xml";
        public void Save()
        {
            if (File.Exists(m_Profile))
                try
                {
                    m_Xdoc.Load(m_Profile);
                    XmlNode Node = m_Xdoc.DocumentElement;
                    XmlNode oldxmlvesion = m_Xdoc.SelectSingleNode("/Profile/DS4Version");
                    XmlNode newxmlVersion = m_Xdoc.CreateNode(XmlNodeType.Element, "DS4Version", null);
                    newxmlVersion.InnerText = newversion.ToString();
                    Node.ReplaceChild(newxmlVersion, oldxmlvesion);
                    m_Xdoc.Save(m_Profile);
                }
                catch { }
        }

        public void Load()
        {
            if (File.Exists(exepath + "\\Profiles.xml"))
            {
                m_Profile = exepath + "\\Profiles.xml";
                path = exepath;
            }
            bool missingSetting = false;
            try
            {
                if (File.Exists(m_Profile))
                {
                    XmlNode Item;
                    m_Xdoc.Load(m_Profile);
                    try { Item = m_Xdoc.SelectSingleNode("/Profile/DS4Version"); Double.TryParse(Item.InnerText, out version); }
                    catch { missingSetting = true; }
                }
            }
            catch { missingSetting = true; }
            if (missingSetting)
                label1.Content = "Current version not found, please run DS4Tool";
        }

        private void btnChangelog_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://docs.google.com/document/d/1l4xcgVQkGUskc5CQ0p069yW22Cd5WAH_yE3Fz2hXo0E/edit?usp=sharing");
        }

        private void btnOpenDS4_Click(object sender, RoutedEventArgs e)
        {
            if (File.Exists(exepath + "\\DS4Windows.exe") || File.Exists(exepath + "\\DS4Tool.exe"))
                try { Process.Start("DS4Tool.exe"); }
                catch { Process.Start("DS4Windows.exe"); }
            else
                Process.Start(exepath);
            this.Close();
        }
    }
}
