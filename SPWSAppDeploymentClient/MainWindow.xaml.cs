using Microsoft.Win32;
using SPWSAppDeploymentClient.Models;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace SPWSAppDeploymentClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static Settings settings;
        public List<AppVersion> newVersions;
        public Apps currentApp;
        int fi = 0;
        Dictionary<AppFile, byte[]> filesList = new Dictionary<AppFile, byte[]>();
        RegistryKey rk;
        string mainExecutable = "";

        private Task Startup()
        {
            return Task.Factory.StartNew(() =>
            {
                SqlConnection sql = new SqlConnection();
                bool status = true;

                status = SettingsCheck().Result;
                UpdateMainProgBar(0);
                UpdateSubProgBar(100);

                if (status) { status = ConnectionCheck().Result; }

                UpdateMainProgBar(20);

                if (status) { status = VersionCheck().Result; }

                UpdateMainProgBar(40);

                if (status == true && newVersions.Count != 0) { status = UpdateApp().Result; }
                else { scanMainExecutable(); }

                UpdateMainProgBar(60);
                UpdateMainProgBar(100);

                if (mainExecutable != "") { RunApp(); }
            });
        }

        private async Task<bool> SettingsCheck()
        {
            bool result = false;

            try
            {
                PostNotice("Settings check...");
                var currentDirectory = AppDomain.CurrentDomain.BaseDirectory;

                if (File.Exists(currentDirectory + "/settings.ini"))
                {
                    PostNotice("Settings detected!");

                    using (StreamReader sr = new StreamReader(currentDirectory + "/settings.ini"))
                    {
                        string line = sr.ReadLine().Split('=')[1];

                        settings = new Settings();
                        rk = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\SPWS\AppDeploymentClient\" + line, true);
                        if (rk == null) { rk = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\SPWS\AppDeploymentClient\" + line); }
                        if (rk.GetValue("Server") != null) { settings.Server = rk.GetValue("Server").ToString(); }
                        if (rk.GetValue("Username") != null) { settings.Username = rk.GetValue("Username").ToString(); }
                        if (rk.GetValue("Password") != null) { settings.Password = rk.GetValue("Password").ToString(); }

                        if (rk.GetValue("AppId") != null)
                        {
                            int AppId = 0;
                            int.TryParse(rk.GetValue("AppId").ToString(), out AppId);
                            settings.AppId = AppId;
                        }

                        if (rk.GetValue("AppName") != null) { settings.AppName = rk.GetValue("AppName").ToString(); }
                        if (rk.GetValue("AppVersion") != null) { settings.AppVersion = rk.GetValue("AppVersion").ToString(); }

                        if (string.IsNullOrEmpty(settings.Server) || string.IsNullOrEmpty(settings.Username) || string.IsNullOrEmpty(settings.Password) || settings.AppId == 0)
                        {
                            PostNotice("Settings file has problems... please try to correct parameters!");
                            result = OpenSettings().Result;
                            Startup();
                        }
                        else { result = true; }
                    }
                }
                else
                {
                    settings = new Settings();
                    PostNotice("No settings detected... please enter parameters.");
                    result = OpenSettings().Result;
                    Startup();
                }

            }
            catch (Exception ex)
            {
                PostNotice("An error has occured. " + ex.Message);
                CreateErrorLog(ex);
            }
            return result;
        }

        private async Task<bool> ConnectionCheck()
        {
            bool result = false;
            PostNotice("Connection check...");
            try
            {
                SqlProcessor sqlProcessor = new SqlProcessor();
                SqlConnection sqlConn = await sqlProcessor.TestConnection(settings);
                if (sqlConn.State == System.Data.ConnectionState.Open)
                {
                    result = true;
                    PostNotice("Connection established...");
                }
            }
            catch (Exception ex)
            {
                PostNotice("An error has occured:" + ex.Message);
                CreateErrorLog(ex);
            }
            return result;
        }

        private async Task<bool> VersionCheck()
        {
            bool result = false;
            PostNotice("Version Check! ");
            try
            {
                List<Apps> apps = await Apps.GetAllApps(settings);
                currentApp = apps.FirstOrDefault(a => a.AppId == settings.AppId);
                List<AppVersion> appVersions = await AppVersion.GetAllAppVersions(settings);
                AppVersion currentVersion = appVersions.FirstOrDefault(av =>
                av.AppId == settings.AppId &&
                av.AppVersionName == settings.AppVersion);
                PostNotice(currentVersion != null ? "Current version detected: " + currentVersion.AppVersionName : "No versions installed...");
                currentApp.AppVersions = appVersions.Where(av => av.AppId == currentApp.AppId).OrderBy(av => av.Date).ToList();

                if (currentVersion != null)
                {
                    newVersions = currentApp.AppVersions.Where(av => av.Date > currentVersion.Date).ToList();
                    result = true;
                }
                else
                {
                    newVersions = currentApp.AppVersions.ToList();
                    result = true;
                }

                PostNotice(newVersions.Count != 0 ? "New updates detected!" : "No updates available!");
            }
            catch (Exception ex)
            {
                PostNotice("An error has occured:" + ex.Message);
                CreateErrorLog(ex);
            }
            return result;
        }

        private async void ProcessVersions(List<AppVersion> appVersions)
        {
            filesList.Clear();
            int i = 0;
            int appVersionCount = 0;

            PostNotice("Updating...");
            foreach (var appVersion in appVersions)
            {
                appVersionCount++;
                PostNotice(appVersionCount + " / " + appVersions.Count + ": " + "Updating...Getting files for version..." + appVersion.AppVersionName);
                var appFiles = await appVersion.GetAllFiles(settings);
                int appFileCount = 0;
                foreach (var appFile in appFiles)
                {
                    appFileCount++;
                    fi++;
                    PostNotice(appVersionCount + " / " + appVersions.Count + ": " + "Updating...Getting files for version..." + appVersion.AppVersionName + " : " + appFileCount + " / " + appFiles.Count);
                    if (appFile.AppFileSize != "0")
                    {
                        byte[] blob = await appFile.GetData(settings);
                        filesList.Add(appFile, blob);
                    }
                    else { filesList.Add(appFile, null); }

                    double a = (double)fi;
                    double flCount = (double)appFiles.Count;
                    double res = (a / flCount) * 100;
                    UpdateSubProgBar(res);
                }
            }

            PostNotice("Processing...");
            foreach (var appVersion in appVersions)
            {
                fi = 0;
                PostNotice("Processing AppVer: " + i + " out of " + appVersions.Count);
                string currentDirectory = Environment.CurrentDirectory;
                processFiles(0, filesList, appVersion, currentDirectory);
                rk.SetValue("AppVersion", appVersion.AppVersionName);
            }
        }

        private async Task<bool> UpdateApp()
        {
            bool result = false;
            try
            {
                filesList.Clear();

                if (newVersions.Any(av => av.isMajorRevision))
                {
                    var currentMR = newVersions.FirstOrDefault(av => av.isMajorRevision);
                    var newVersionPlot = newVersions;
                    newVersions.RemoveAll(av => av.Date < currentMR.Date);
                    DeleteFiles(Environment.CurrentDirectory);
                }

                ProcessVersions(newVersions);
                result = true;
            }
            catch (Exception ex)
            {
                PostNotice("An error has occured:" + ex.Message);
                Dispatcher.Invoke(() => { CreateErrorLog(ex); });
            }

            return result;
        }

        private void processFiles(int FolderId, Dictionary<AppFile, byte[]> filesList, AppVersion appVersion, string currentDirectory)
        {
            if (FolderId != 0)
            {
                fi++;
                if (!Directory.Exists(currentDirectory)) { Directory.CreateDirectory(currentDirectory); }
            }
            var currentVersionFiles = filesList.Where(fl => fl.Key.AppVersionId == appVersion.AppVersionId).ToList();
            var rootfiles = currentVersionFiles.Where(fl => fl.Key.AppFileSize != "0" && fl.Key.parentFolder == FolderId);
            var rootFolders = currentVersionFiles.Where(fl => fl.Key.AppFileSize == "0" && fl.Key.parentFolder == FolderId);

            foreach (var rootFile in rootfiles)
            {
                fi++;
                PostNotice("Processing Files:" + fi + " out " + filesList.Count);

                string filePath = currentDirectory + "\\" + rootFile.Key.AppFileName;
                var splits = rootFile.Key.AppFileName.Split('.');

                if (currentDirectory == Environment.CurrentDirectory && rootFile.Key.AppFileExt == "exe" && splits.Length == 2) { mainExecutable = rootFile.Key.AppFileName; }
                if (File.Exists(filePath)) { File.Delete(filePath); }

                using (FileStream fs = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.Write))
                {
                    BinaryWriter br = new BinaryWriter(fs);
                    br.Write(rootFile.Value);
                    br.Dispose();
                }
            }

            double a = (double)fi;
            double flCount = (double)filesList.Count;
            double res = (a / flCount) * 100;

            UpdateSubProgBar(res);
            foreach (var rootFolder in rootFolders)
            {
                processFiles(rootFolder.Key.AppFileId, filesList, appVersion, currentDirectory + "\\" + rootFolder.Key.AppFileName);
            }
        }

        private void DeleteFiles(string Path)
        {
            DirectoryInfo directoryInfo = new DirectoryInfo(Path);
            FileInfo[] info = directoryInfo.GetFiles();
            DirectoryInfo[] directories = directoryInfo.GetDirectories();

            foreach (var d in directories)
            {
                DeleteFiles(d.FullName);
                d.Delete();
            }

            foreach (FileInfo f in info)
            {
                if (!(f.Name == "settings.ini" || f.Name.ToLower().Contains("spwsappdeployment"))) { f.Delete(); }
            }
        }

        private void RunApp()
        {
            var currentDirectory = AppDomain.CurrentDomain.BaseDirectory;

            if (File.Exists(currentDirectory + "\\" + mainExecutable))
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    var process = new Process();
                    process.StartInfo.FileName = currentDirectory + "\\" + mainExecutable;
                    process.EnableRaisingEvents = true;
                    process.Exited += Process_Exited;
                    process.Start();
                    ShowInTaskbar = false;
                    WindowState = WindowState.Minimized;
                }));
            }
        }

        private async Task<bool> OpenSettings()
        {
            bool result = false;
            try
            {
                await Dispatcher.BeginInvoke(new Action(() =>
                {
                    SettingsWindow sw = new SettingsWindow(settings);
                    sw.Owner = this;
                    sw.ShowInTaskbar = false;
                    sw.ShowDialog();
                    if (sw.DialogResult == true)
                    {
                        PostNotice("Settings set! ");
                        result = true;
                    }
                    else
                    {
                        result = false;
                        Environment.Exit(1);
                    }
                }));
            }
            catch (Exception ex)
            {
                PostNotice("An error has occured:" + ex.Message);
                Dispatcher.Invoke(() => { CreateErrorLog(ex); });
            }

            return result;
        }

        private void PostNotice(string Message)
        {
            Dispatcher.BeginInvoke(new Action(() => { txtMessage.Text = Message; }));
        }

        private void UpdateMainProgBar(double a)
        {
            Dispatcher.BeginInvoke(new Action(() => { pbMain.Value = a; }));
        }

        private void UpdateSubProgBar(double a)
        {
            Dispatcher.BeginInvoke(new Action(() => { pbSub.Value = a; }));
        }

        private bool scanMainExecutable()
        {
            DirectoryInfo di = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            List<FileInfo> fi = di.GetFiles().ToList();
            var executable = fi.FirstOrDefault(f => f.Name.Split('.').Last() == "exe" && f.Name.Split('.').Length == 2 && f.Name != "SPWSAppDeploymentClient.exe");
            if (executable != null) { mainExecutable = executable.Name; }

            return mainExecutable != "";
        }

        public static void CreateErrorLog(Exception ex)
        {
            string currentDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string ErrorLogFilePath = currentDirectory + "/ErrorLogs/" + DateTime.Now.ToString("yyyyMMdd") + ".txt";

            if (!Directory.Exists(currentDirectory + "/ErrorLogs")) { Directory.CreateDirectory(currentDirectory + "/ErrorLogs"); }

            using (StreamWriter sw = File.Exists(ErrorLogFilePath) ? File.AppendText(ErrorLogFilePath) : File.CreateText(ErrorLogFilePath))
            {
                sw.WriteLine("-----------------------");
                sw.WriteLine("Message: " + ex.Message);
                sw.WriteLine("Date: " + DateTime.Now.ToString("MMMM d, yyyy h:mm:ss tt"));
                sw.WriteLine("StackTrace: " + ex.StackTrace);
                sw.WriteLine("InnerException: " + ex.InnerException);
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            double width = System.Windows.Forms.Screen.PrimaryScreen.WorkingArea.Width;
            double height = System.Windows.Forms.Screen.PrimaryScreen.WorkingArea.Height;
            Left = width - this.Width;
            Top = height - this.Height;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Startup();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F8)
            {
                OpenSettings();
            }
        }

        private void Process_Exited(object sender, EventArgs e)
        {
            Environment.Exit(1);
            //try lang
        }
    }
}
