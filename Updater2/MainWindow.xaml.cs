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
        public MainWindow()
        {
            InitializeComponent();
            double newversion;
            try
            {
                if (double.TryParse(File.ReadAllText(path + "\\version.txt"), out newversion))
                {

                    Uri url = new Uri("https://dl.dropboxusercontent.com/u/16364552/DS4Tool/DS4Tool%20-%20J2K%20%28v" + newversion.ToString() + "%29.zip");//Sorry other devs, gonna have to find your own server
                    sw.Start();
                    if (!Directory.Exists(path))
                        Directory.CreateDirectory(path);
                    try { wc.DownloadFileAsync(url, path + "\\Update.zip"); }
                    catch (Exception e) { label1.Content = e.Message; }
                    wc.DownloadFileCompleted += wc_DownloadFileCompleted;
                    wc.DownloadProgressChanged +=wc_DownloadProgressChanged;
                }
            }
            catch
            {
                label1.Content = "version.txt not found, please re-run DS4Tool";
            }

           
            if (processes.Length > 0)
            {
                if (MessageBox.Show("It must be closed to update.", "DS4Tool is still running", MessageBoxButton.OKCancel, MessageBoxImage.Exclamation) == MessageBoxResult.OK)
                    for (int i = processes.Length - 1; i >= 0; i--)
                        processes[i].CloseMainWindow();
                else
                    this.Close();
            }
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
                label1.Content = "New Updater in program folder, replace to finish update";
            else
                label1.Content = "Update complete";
            UpdaterBar.Value = 106;
            TaskbarItemInfo.ProgressState = TaskbarItemProgressState.None;
            btnOpenDS4.IsEnabled = true;
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
