using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using ServiceLibrary;

namespace UserControls
{
    /// <summary>
    ///     Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class TabControl : UserControl, ControlUnit
    {
        private readonly MainUI parent;
        private readonly Service service = Service.GetInstance();
        private readonly TestApp testApp;
        private readonly ZtreeControl ztreeCtrl;
        private bool clientSelected;
        private bool inited;

        public TabControl(MainUI parent, TestApp testApp)
        {
            InitializeComponent();
            this.parent = parent;
            this.testApp = testApp;

            if (testApp is ZTree)
            {
                inited = true;

                ztreeCtrl = new ZtreeControl((ZTree) testApp);
                ztreeControls.Children.Add(ztreeCtrl);
                btnDelResults.IsEnabled = true;
                btnGetResults.IsEnabled = true;
            }
        }

        public TabItem TabItem { get; set; }

        public void ButtonClickable(bool enabled)
        {
            clientSelected = enabled;
            if (testApp is ZTree)
            {
                clientSelected = true;
            }
            SetClickable();
        }

        public void SetProject(string projectName)
        {
            testApp.ProjectName = projectName;
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            // Create OpenFileDialog 
            var dlg = new OpenFileDialog();

            // Set filter for file extension and default file extension 
            //dlg.DefaultExt = testApp.Extension;

            dlg.Filter = testApp.ExtensionDescription;

            // Display OpenFileDialog by calling ShowDialog method 
            var result = dlg.ShowDialog();

            // Get the selected file name and display in a TextBox 
            if (result == true)
            {
                // Open document 
                var testFullPath = dlg.FileName;

                txbBrowse.Text = testFullPath;
                inited = true;

                btnRun.IsEnabled = true;
                testApp.Initialize(testFullPath);
                SetClickable();
            }
        }

        public void setTestLogo(string path)
        {
            var uriSource = new Uri(path, UriKind.Relative);
            imgTest.Source = new BitmapImage(uriSource);
        }

        private void btnRun_Click(object sender, RoutedEventArgs e)
        {
            var computerNames = parent.getSelectedClients();

            if (computerNames.Count == 0)
            {
                MessageBox.Show("Select some clients first!");
                return;
            }

            parent.SetTabActivity(TabItem, computerNames, true);
            parent.updateStatus("In Progress...");

            if (testApp is ZTree)
            {
                //MessageBox.Show("Make sure", "Attention", MessageBoxButton.OK, MessageBoxImage.Information);
                var winSize = ztreeCtrl.GetSelectedWindowSize();
                ((ZTree) testApp).TransferAndRun(computerNames, winSize);
            }
            else
            {
                var copyAll = (bool) cbxCopyAll.IsChecked;
                testApp.TransferAndRun(computerNames, copyAll);
            }
        }

        private void btnKill_Click(object sender, RoutedEventArgs e)
        {
            var clients = parent.getSelectedClients();

            parent.updateStatus("In Progress...");
            parent.SetTabActivity(TabItem, clients, false);

            var appExeName = testApp.AppExeName;
            try
            {
                service.KillRemoteProcess(clients, appExeName);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void btnGetResults_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBoxResult.Yes;
            if (!(testApp is ZTree))
            {
                result =
                    MessageBox.Show(
                        "Are you sure you have selected all the labclients that you want to transfer results from?\n\nClick Yes to continue.",
                        "Are you sure?", MessageBoxButton.YesNo, MessageBoxImage.Question);
            }
            if (result == MessageBoxResult.Yes)
            {
                parent.updateStatus("In Progress...");

                try
                {
                    testApp.TransferResults(parent.getSelectedClients());
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Transfer timed out: " + ex.Message + "\n\nNot all results might be transferred.",
                        "Transfer Failure", MessageBoxButton.OK, MessageBoxImage.Warning);
                    parent.updateStatus("Idle");
                }
            }
        }

        private void SetClickable()
        {
            btnKill.IsEnabled = clientSelected;
            btnRun.IsEnabled = clientSelected && inited;
            btnGetResults.IsEnabled = clientSelected;
        }

        private void btnDelResults_Click(object sender, RoutedEventArgs e)
        {
            var msg =
                "Are you sure you want to delete all result files from lab computers and admin computer?\nMake sure you have a backup!";
            if (!(testApp is ZTree))
            {
                msg = "Make sure you have selected all lab computers that you want results to be deleted from!\n\n" +
                      msg;
            }
            else
            {
                msg = msg + "\n\nIf you have a running ztree application it will be closed!";
            }

            var result = MessageBox.Show(msg, "Are you sure?", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                testApp.DeleteResults(parent.getSelectedClients());
            }
        }

        private void CreateDir()
        {
            try
            {
                testApp.CreateProjectDir();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "Failed to create directory", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnOpenRes_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                testApp.OpenResultsFolder();
            }
            catch (DirectoryNotFoundException ex)
            {
                var result =
                    MessageBox.Show(
                        "Project directory: \"" + ex.Message +
                        "\" was not found.\n\nDo you want to create a folder for this project?", "Directory not found",
                        MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    CreateDir();
                    btnOpenRes_Click(sender, e);
                }
            }
        }

        private void btnExportDb_Click(object sender, RoutedEventArgs e)
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
                    DMSValidation dms = new DMSValidation();
                    dms.Show();
                    testApp.ToDms();
                  
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
    }
}