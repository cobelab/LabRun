using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using ServiceLibrary;
using MessageBox = System.Windows.MessageBox;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using UserControl = System.Windows.Controls.UserControl;

namespace UserControls
{
    /// <summary>
    ///     Interaction logic for CustomRun.xaml
    /// </summary>
    public partial class CustomRun : UserControl, ControlUnit
    {
        private readonly MainUI parent;
        private readonly Service service = Service.GetInstance();
        private CustomRunTestApp crTestApp;


        public CustomRun(MainUI parent, List<LabClient> clients)
        {
            InitializeComponent();
            isEnabledSingle = false;
            isEnabledDir = false;
            this.parent = parent;
            TimeStamp = service.GetCurrentTimestamp();
            lblTimestmp.Content = "Timestamp: " + TimeStamp;
            extensions = new List<string>();
            InitProcList(clients);
        }

        public List<CompAndProcesses> procList { get; set; }
        public string filePath { get; set; }
        public string fileName { get; set; }
        public string DirPath { get; set; }
        public string DirFileName { get; set; }
        public string DirFileNameWithExtraDir { get; set; }
        public bool isEnabledSingle { get; set; }
        public bool isEnabledDir { get; set; }
        public bool IsEnabledDirTransfernRun { get; set; }
        public List<string> extensions { get; set; }
        public string TimeStamp { get; set; }
        public string Parameter { get; set; }
        public TabItem TabItem { get; set; }

        public void ButtonClickable(bool enabled)
        {
            btnCleanCustomDir.IsEnabled = enabled;
            btnGetResults.IsEnabled = enabled;
            if ((IsEnabledDirTransfernRun) && (enabled))
            {
                btnTransfernRunDir.IsEnabled = true;
            }
            if ((isEnabledSingle) && (enabled))
            {
                btnTransferSingleFile.IsEnabled = true;
                btnTransfernRunSingleFile.IsEnabled = true;
            }
            if ((isEnabledDir) && (enabled))
            {
                btnTransferDir.IsEnabled = true;
                btnBrowseDirFileToRun.IsEnabled = true;
            }
            if ((isEnabledSingle == false) || (enabled == false))
            {
                btnTransferSingleFile.IsEnabled = false;
                btnTransfernRunSingleFile.IsEnabled = false;
            }
            if ((isEnabledDir == false) || (enabled == false))
            {
                btnTransfernRunDir.IsEnabled = false;
                btnTransferDir.IsEnabled = false;
                btnBrowseDirFileToRun.IsEnabled = false;
            }
        }

        public void SetProject(string projectName)
        {
            if (crTestApp != null)
            {
                crTestApp.ProjectName = projectName;
            }
        }

        public void InitProcList(List<LabClient> clients)
        {
            procList = new List<CompAndProcesses>();
            foreach (var client in clients)
            {
                var ComProc = new CompAndProcesses();
                ComProc.computer = client;
                procList.Add(ComProc);
            }
        }

        public MainUI getParent()
        {
            return parent;
        }

        private void btnTimestmp_Click(object sender, RoutedEventArgs e)
        {
            TimeStamp = service.GetCurrentTimestamp();
            lblTimestmp.Content = "Timestamp: " + TimeStamp;
        }

        private void btnSetParameter_Click(object sender, RoutedEventArgs e)
        {
            Parameter = txtParameter.Text;
            lblParameter.Content = "Current parameter: " + Parameter;
        }

        private void btnBrowseSingleFile_Click(object sender, RoutedEventArgs e)
        {
            // Create OpenFileDialog 
            var dlg = new OpenFileDialog();

            // Display OpenFileDialog by calling ShowDialog method 
            var result = dlg.ShowDialog();

            // Get the selected file name and save the path 
            if (result == true)
            {
                filePath = dlg.FileName;
                var words = filePath.Split('\\');
                foreach (var word in words)
                {
                    fileName = word;
                }
                isEnabledSingle = true;
                if (parent.getSelectedClients().Count != 0)
                {
                    btnTransfernRunSingleFile.IsEnabled = true;
                    btnTransferSingleFile.IsEnabled = true;
                }

                lblFilePath.Content = filePath;
            }
        }


        private void btnTransferSingleFile_Click(object sender, RoutedEventArgs e)
        {
            var clients = parent.getSelectedClients();
            ThreadStart tssingle = delegate
            {
                service.CopyFilesToNetworkShare(this.filePath, this.TimeStamp);
                service.CopyFilesFromNetworkShareToClients(this.filePath, this.fileName, clients, this.TimeStamp);
            };
            service.RunInNewThread(tssingle);
        }

        private void btnTransfernRunSingleFile_Click(object sender, RoutedEventArgs e)
        {
            // Check file name and extension for keeping track of running processes
            var filename = "";
            var split = filePath.Split('\\');
            foreach (var temp in split)
            {
                filename = temp;
            }
            var extname = "";
            var extSplit = filePath.Split('.');
            foreach (var temp in extSplit)
            {
                extname = temp;
            }

            // Launch whatever process to the processList


            // Check if param is not null
            var param = "";
            if (Parameter != null)
                param = Parameter;

            // Set Custom tab as "Running"
            var clients = parent.getSelectedClients();
            parent.SetTabActivity(TabItem, clients, true);

            // Start Custom Running in new thread

            service.CopyFilesToNetworkShare(filePath, TimeStamp);
            service.CopyAndRunFilesFromNetworkShareToClients(filePath, fileName, clients, param, TimeStamp);
        }

        private void btnBrowseDir_Click(object sender, RoutedEventArgs e)
        {
            var browse = new FolderBrowserDialog();
            var result = browse.ShowDialog();

            if (browse.SelectedPath != "")
            {
                DirPath = browse.SelectedPath;
                lblDirPath.Content = "Folder: " + DirPath;
                isEnabledDir = true;
                if (parent.getSelectedClients().Count != 0)
                {
                    btnBrowseDirFileToRun.IsEnabled = true;
                    btnTransferDir.IsEnabled = true;
                    btnBrowseDirFileToRun.IsEnabled = true;
                    isEnabledDir = true;
                }
            }
        }

        private void btnBrowseDirFileToRun_Click(object sender, RoutedEventArgs e)
        {
            // Create OpenFileDialog 
            var dlg = new OpenFileDialog();
            dlg.InitialDirectory = DirPath;

            // Display OpenFileDialog by calling ShowDialog method 
            var result = dlg.ShowDialog();

            // Get the selected file name and save the path 
            if (result == true)
            {
                DirFileName = dlg.FileName;

                var directory = DirPath.Split('\\');
                var file = DirFileName.Split('\\');
                var i = 0;
                var subFileName = "";
                foreach (var word in file)
                {
                    if (i > directory.Length - 1)
                    {
                        subFileName += "\\" + file[i];
                    }
                    i++;
                }
                lblDirFilePath.Content = "File: " + subFileName;
                DirFileNameWithExtraDir = subFileName;
                isEnabledDir = true;
                if (parent.getSelectedClients().Count != 0)
                {
                    IsEnabledDirTransfernRun = true;
                    btnTransfernRunDir.IsEnabled = true;
                    btnTransferDir.IsEnabled = true;
                }
            }
        }

        private void btnTransfernRunDir_Click(object sender, RoutedEventArgs e)
        {
            // Check file name and extension for keeping track of running processes
            var filename = "";
            var split = DirFileName.Split('\\');
            foreach (var temp in split)
            {
                filename = temp;
            }
            var extname = "";
            var extSplit = DirFileName.Split('.');
            foreach (var temp in extSplit)
            {
                extname = temp;
            }


            var param = "";
            if (Parameter != null)
                param = Parameter;
            var clients = parent.getSelectedClients();
            parent.SetTabActivity(TabItem, clients, true);

            ThreadStart ts =
                delegate
                {
                    service.CopyAndRunFolder(clients, this.DirPath, this.DirFileNameWithExtraDir, param, this.TimeStamp);
                };
            service.RunInNewThread(ts);
        }

        private void btnDefineExtensions_Click(object sender, RoutedEventArgs e)
        {
            Window WindowResultsExtensions = new ResultsExtensionWindow(this, extensions);
            WindowResultsExtensions.Show();
        }

        private void btnGetResults_Click(object sender, RoutedEventArgs e)
        {
            if (extensions.Count != 0)
            {
                crTestApp = new CustomRunTestApp(extensions);
                SetProject(parent.getProject());
                crTestApp.testFolder = service.Config.Computerinstallpath;
                crTestApp.TransferResults(parent.getSelectedClients());
            }
            else
                MessageBox.Show("Define some extensions to retrieve!");
        }

        private void btnCleanCustomDir_Click(object sender, RoutedEventArgs e)
        {
            var result =
                MessageBox.Show(
                    @"Are you sure you want to delete the entire ""Transferred files"" directory? Verify that result files are backed up and that nothing of value remains in the directory! Do you wish to continue?",
                    "Are you sure?", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                service.DeleteFiles(parent.getSelectedClients());
            }
        }

        private void btnTransferToDMS_Click(object sender, RoutedEventArgs e)
        {
            if (service.User == null)
            {
                MessageBox.Show(@"You must be logged in to do that.", "Login required", MessageBoxButton.OK,
                    MessageBoxImage.Exclamation);
                return;
            }
            var result =
                MessageBox.Show(
                    "All results data from your project will be copied to DMS\nTo see what will be transferred, click \"Open Results\" to view your project folder.\n\nAre you sure you want to continue?",
                    "Are you sure?", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    if (crTestApp == null)
                    {
                        crTestApp = new CustomRunTestApp(extensions);
                        SetProject(parent.getProject());
                        crTestApp.testFolder = service.Config.Computerinstallpath;
                    }
                    DMSValidation dms = new DMSValidation();
                    dms.Show();
                    crTestApp.ToDms();
                }
                catch (DirectoryNotFoundException ex)
                {
                    MessageBox.Show(
                        "Project folder: \"" + ex.Message +
                        "\" was not found! Make sure that you have transferred results to this project.",
                        "Directory Not Found", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void btnTransferDir_Click(object sender, RoutedEventArgs e)
        {
            var clients = parent.getSelectedClients();
            service.CopyFolder(clients, DirPath, TimeStamp);
        }

        private void btnKill_Click(object sender, RoutedEventArgs e)
        {
            Window KillProcessWindow = new KillProcessWindow(this);
            KillProcessWindow.Show();
        }

        public void ProcessStopped(string procName)
        {
            foreach (var client in parent.getSelectedClients())
            {
                foreach (var compProc in procList)
                {
                    if (compProc.computer == client)
                    {
                        var clients = new List<LabClient>();
                        clients.Add(client);
                        parent.SetTabActivity(TabItem, clients, false);
                    }
                }
            }
        }
    }
}