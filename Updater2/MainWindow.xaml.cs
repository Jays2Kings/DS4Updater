using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Reflection;
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
        Process[] processes = Process.GetProcessesByName("DS4Tool");
        double version, newversion;
        protected String m_Profile = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\DS4Tool\\Profiles.xml";
        protected XmlDocument m_Xdoc = new XmlDocument();
        bool newupdate;
        public MainWindow()
        {
            InitializeComponent();
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
            Load();
            if (!File.Exists(path + "\\version.txt"))
            {
                Uri urlv = new Uri("https://dl.dropboxusercontent.com/u/16364552/DS4Tool/newest%20version.txt");
                //Sorry other devs, gonna have to find your own server
                WebClient wc2 = new WebClient();
                wc.DownloadFile(urlv, path + "\\version.txt");
                if (double.TryParse(File.ReadAllText(path + "\\version.txt"), out newversion))
                    if (newversion > version)
                        newupdate = true;
                    else
                        File.Delete(path + "\\version.txt");
                else
                {
                    label1.Content = "J2K has messed up the update, please contact him";
                    File.Delete(path + "\\version.txt");
                }
            }
            else
            {
                if (double.TryParse(File.ReadAllText(path + "\\version.txt"), out newversion))
                    if (newversion > version)
                        newupdate = true;
            }

            if (newupdate)
            {
                Uri url = new Uri("https://dl.dropboxusercontent.com/u/16364552/DS4Tool/DS4Tool%20-%20J2K%20%28v" + newversion.ToString() + "%29.zip");
                //Sorry other devs, gonna have to find your own server
                sw.Start();
                try { wc.DownloadFileAsync(url, path + "\\Update.zip"); }
                catch (Exception e) { label1.Content = e.Message; }
                wc.DownloadFileCompleted += wc_DownloadFileCompleted;
                wc.DownloadProgressChanged += wc_DownloadProgressChanged;
            }
            else
            {
                label1.Content = "DS4Tool is up to date";
                File.Delete(path + "\\version.txt");
                btnOpenDS4.IsEnabled = true;
            }

            if (processes.Length > 0)
                if (MessageBox.Show("It must be closed to update.", "DS4Tool is still running", MessageBoxButton.OKCancel, MessageBoxImage.Exclamation) == MessageBoxResult.OK)
                    for (int i = processes.Length - 1; i >= 0; i--)
                        processes[i].CloseMainWindow();
                else
                    this.Close();
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
            label2.Opacity = 0;
            label1.Content = "Deleting old files";
            UpdaterBar.Value = 102;
            TaskbarItemInfo.ProgressValue = UpdaterBar.Value / 106d;
            File.Delete("DS4Tool.exe");
            File.Delete("DS4Control.dll");
            File.Delete("DS4Library.dll");
            File.Delete("HidLibrary.dll");
            label1.Content = "Installing new files";
            UpdaterBar.Value = 104;
            TaskbarItemInfo.ProgressValue = UpdaterBar.Value / 106d;
            try { ZipFile.ExtractToDirectory(path + "\\Update.zip", Directory.GetParent(Assembly.GetExecutingAssembly().Location).FullName); }
            catch (IOException) { } //Since profiles may be in the zip ignore them if already exists
            File.Delete(path + "\\version.txt");
            File.Delete(path + "\\Update.zip");
            if (File.Exists("Updater NEW.exe"))
                label1.Content = "New Updater in program folder, close this to finish update.";
            else
                label1.Content = "Update complete";
            Save();
            UpdaterBar.Value = 106;
            TaskbarItemInfo.ProgressState = TaskbarItemProgressState.None;
            btnOpenDS4.IsEnabled = true;
        }

        public void Save()
        {
            m_Xdoc.Load(m_Profile);
            XmlNode Node = m_Xdoc.DocumentElement;
            XmlNode oldxmlvesion = m_Xdoc.SelectSingleNode("/Profile/DS4Version");
            XmlNode newxmlVersion = m_Xdoc.CreateNode(XmlNodeType.Element, "DS4Version", null);
            newxmlVersion.InnerText = newversion.ToString();
            Node.ReplaceChild(newxmlVersion, oldxmlvesion);
            m_Xdoc.Save(m_Profile);
        }

        public void Load()
        {
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
                label1.Content = "Current version not found, please re-run DS4Tool";
        }

        private void btnChangelog_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://docs.google.com/document/d/1l4xcgVQkGUskc5CQ0p069yW22Cd5WAH_yE3Fz2hXo0E/edit?usp=sharing");
        }

        private void btnOpenDS4_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("DS4Tool.exe");
            this.Close();
        }
    }
}
