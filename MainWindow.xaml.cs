using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ServiceLibrary;
using UserControls;
using TabControl = UserControls.TabControl;
using System.Messaging;
using System.Timers;

namespace LabRun
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, MainUI
    {
        private readonly string unnamedProject = "UnnamedProject";
        private List<LabClient> clients = new List<LabClient>();
        private bool isSelectionByCmbbx;
        public int labNo = 2;
        private Login login;
        private ProjectName projectNameWnd;
        private Project projectWnd;
        private readonly Service service;
        private readonly List<ControlUnit> tabControls = new List<ControlUnit>();
        private MessageQueue msMqHelp;
        private MessageQueue msMqNotify;
        private MessageQueue msMqReply;

        private Window notifyWindow;

        public MainWindow()
        {
            InitializeComponent();
            try
            {
                service = Service.GetInstance();
            }
            catch (FileNotFoundException)
            {           
                    var msg = "File run_with_logs.py was not found! PsychoPy might not function properly.";
                    MessageBox.Show(msg, "File not found", MessageBoxButton.OK, MessageBoxImage.Error);            
            }
           

            service.ProgressUpdate += (s, e) =>
            {
                Dispatcher.Invoke(delegate
                {
                    var args = (StatusEventArgs) e;
                    lblStatus.Content = args.Message;
                }
                    );
            };
            InitClients();
            InitTabs();
            SetProject(unnamedProject);
            service.StartPingSvc(clients);

            // USE MSMQ if it is enabled
            if (service.MSMQConfig.MSMQ)
            {
                service.TransferLabClientService(clients);
                StartMSMQService();
            }
        }


        public string Project { get; private set; } = "";

        public string getProject()
        {
            return Project;
        }

        public void SetFeatureActivity(Feature feature, List<LabClient> selectedClients, bool active)
        {
            var exists = false;
            DataGridColumn column = null;
            switch (feature)
            {
                case Feature.WEB:
                    selectedClients.ForEach(i => i.Web = active);
                    exists = clients.Exists(i => i.Web);
                    column = dgrClients.Columns[8];
                    break;
                case Feature.SHARESCR:
                    selectedClients.ForEach(i => i.ShareScr = active);
                    exists = clients.Exists(i => i.ShareScr);
                    column = dgrClients.Columns[9];
                    break;
                case Feature.INPUT:
                    selectedClients.ForEach(i => i.Input = active);
                    exists = clients.Exists(i => i.Input);
                    column = dgrClients.Columns[10];
                    break;
                case Feature.NOTIFY:
                    selectedClients.ForEach(i => i.Notify = active);
                    exists = clients.Exists(i => i.Notify);
                    column = dgrClients.Columns[2];
                    break;
            }
            SetColumnVisibility(column, exists);

        }

        public void SetTabActivity(TabItem tabItem, List<LabClient> selectedClients, bool active)
        {
            if (!(tabItem.Header is TextBlock))
            {
                return;
            }

            var exists = false;
            DataGridColumn column = null;
            switch (tabItem.Name)
            {
                case "tabPsy":
                    selectedClients.ForEach(i => i.PsychoPy = active);
                    exists = clients.Exists(i => i.PsychoPy);
                    column = dgrClients.Columns[3];
                    break;
                case "tabEPrime":
                    selectedClients.ForEach(i => i.EPrime = active);
                    exists = clients.Exists(i => i.EPrime);
                    column = dgrClients.Columns[4];
                    break;
                case "tabZTree":
                    selectedClients.ForEach(i => i.ZTree = active);
                    exists = clients.Exists(i => i.ZTree);
                    column = dgrClients.Columns[5];
                    break;
                case "tabCustom":
                    selectedClients.ForEach(i => i.Custom = active);
                    exists = clients.Exists(i => i.Custom);
                    column = dgrClients.Columns[6];
                    break;
                case "tabChrome":
                    selectedClients.ForEach(i => i.Chrome = active);
                    exists = clients.Exists(i => i.Chrome);
                    column = dgrClients.Columns[7];
                    break;
            }
            ((TextBlock) tabItem.Header).Foreground = exists ? Brushes.Red : Brushes.Black;
            SetColumnVisibility(column, exists);
        }

        public List<LabClient> getSelectedClients()
        {
            return dgrClients.SelectedItems.Cast<LabClient>().ToList();
        }

        public List<string> getSelectedClientsNames()
        {
            var clients = dgrClients.SelectedItems.Cast<LabClient>().ToList();

            var computerNames = new List<string>();
            foreach (var client in clients)
            {
                computerNames.Add(client.ComputerName);
            }
            return computerNames;
        }

        public void updateStatus(string msg)
        {
            lblStatus.Content = msg;
        }
        // TODO Have to make sure this code is working correctly
        public void SetProject(string projectName, bool checkForExistingProject = false)
        {
            if (checkForExistingProject && service.LocalProjectExists(Project))
            {
                var result =
                    MessageBox.Show(
                        "Previous project had some data! Would you like to move that data from the old project to this new project?",
                        "Warning", MessageBoxButton.YesNoCancel, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Cancel)
                {
                    return;
                }

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        service.MoveProject(Project, projectName);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(
                            "Unable to rename the folder. Make sure that there are no open files or explorer windows from that directory.\n\n" +
                            ex.Message, "Failed to rename", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }

            Project = projectName;
            lblProject.Text = Project;
            foreach (var cUnit in tabControls)
            {
                cUnit.SetProject(Project);
            }
        }

        // Updates the grid
        public void InitClients()
        {
            try
            {
                clients = service.GetLabComputersFromStorage();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            UpdateClientsGrid();
        }

        public void UpdateClientsGrid()
        {
            if (labNo == 0)
            {
                dgrClients.ItemsSource = clients;
            }
            else
            {
                var selectedClients = service.FilterForRoom(clients, labNo);
                dgrClients.ItemsSource = selectedClients;
            }
        }

        private void InitTabs()
        {
            var tC = new TabControl(this, new PsychoPy());
            tC.setTestLogo(@"\Images\Psychopy.png");
            tabPsy.Content = tC;
            tC.TabItem = tabPsy;
            tabControls.Add(tC);
            tabPsy.Header = new TextBlock
            {
                Text = tabPsy.Header.ToString()
            };

            var tC2 = new TabControl(this, new EPrime());
            tC2.setTestLogo(@"\Images\eprime.png");
            tabEPrime.Content = tC2;
            tC2.TabItem = tabEPrime;
            tabControls.Add(tC2);
            tabEPrime.Header = new TextBlock
            {
                Text = tabEPrime.Header.ToString()
            };

            var tC3 = new TabControl(this, new ZTree());
            tC3.setTestLogo(@"\Images\ztree.png");
            tabZTree.Content = tC3;
            tC3.TabItem = tabZTree;
            tabControls.Add(tC3);
            tabZTree.Header = new TextBlock
            {
                Text = tabZTree.Header.ToString()
            };

            ((Button) tC3.FindName("btnRun")).Content = "Run Leaves";
            ((Button) tC3.FindName("btnBrowse")).Visibility = Visibility.Hidden;
            ((TextBlock) tC3.FindName("txbBrowse")).Visibility = Visibility.Hidden;
            ((CheckBox) tC3.FindName("cbxCopyAll")).Visibility = Visibility.Hidden;

            var tC4 = new ChromeTab(this);
            tC4.setTestLogo(@"\Images\chrome-logo.png");
            tabChrome.Content = tC4;
            tC4.TabItem = tabChrome;
            tabControls.Add(tC4);
            tabChrome.Header = new TextBlock
            {
                Text = tabChrome.Header.ToString()
            };

            var tC5 = new CustomRun(this, clients);
            tabCustom.Content = tC5;
            tC5.TabItem = tabCustom;
            tabControls.Add(tC5);
            tabCustom.Header = new TextBlock
            {
                Text = tabCustom.Header.ToString()
            };
        }

        private void SetColumnVisibility(DataGridColumn column, bool visible)
        {
            column.Visibility = visible ? Visibility.Visible : Visibility.Hidden;
            dgrClients.Items.Refresh();
        }

        private List<string> GetSelectedCompsMacs()
        {
            var selectedMACs = new List<string>();
            var clients = dgrClients.SelectedItems.Cast<LabClient>().ToList();
            foreach (var client in clients)
            {
                selectedMACs.Add(client.Mac);
            }
            return selectedMACs;
        }

        private void btnShutdown_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Are you sure you want to shutdown selected computers?", "Are you sure?",
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                lblStatus.Content = "In Progress...";
                service.ShutdownComputers(getSelectedClients());
            }
        }

        public IEnumerable<DataGridRow> GetDataGridRows(DataGrid grid)
        {
            var itemsSource = grid.ItemsSource;
            if (null == itemsSource) yield return null;
            foreach (var item in itemsSource)
            {
                var row = grid.ItemContainerGenerator.ContainerFromItem(item) as DataGridRow;
                if (null != row) yield return row;
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var list = GetSelectedCompsMacs();
            var cList = getSelectedClients();
            foreach (var mac in list)
            {
                try
                {
                    MACAddress.SendWOLPacket(mac);
                }
                catch (Exception Error)
                {
                    MessageBox.Show(
                        string.Format("Error:\n\n{0}", Error.Message), "Error");
                }
            }

            if (service.MSMQConfig.MSMQ) {
            service.TransferLabClientService(cList, true);
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            var result =
                MessageBox.Show(
                    "Are you sure you want to update the list of computers? Please make sure that the computers are all turned on after the server, to enable discovery. Check ARP list to be sure or restart all lab computers manually. Do you wish to continue?",
                    "Are you sure?", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    dgrClients.ItemsSource = service.GetLabComputersNew2(labNo);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        "ARP error! The computer is not listed in the ARP pool. Restart client computers to solve the problem. \n" +
                        ex.Message, "ARP error!");
                }
            }
        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            new About(this).Show();
        }

        private void cmbBxLabSelect_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (service == null)
                return;

            var labNo = ((ComboBoxItem) cmbBxLabSelect.SelectedItem).Tag.ToString();
            switch (labNo)
            {
                case "lab1":
                {
                    this.labNo = 1;
                    break;
                }
                case "lab2":
                {
                    this.labNo = 2;
                    break;
                }
                case "both":
                {
                    this.labNo = 0;
                    break;
                }
            }
            UpdateClientsGrid();
            cmbSelectionClients.SelectedIndex = 0;
        }

        private void dgrClients_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var smthSelected = dgrClients.SelectedItems.Count > 0;
            foreach (var tab in tabControls)
            {
                tab.ButtonClickable(smthSelected);
            }
            btnStartUp.IsEnabled = smthSelected;
            btnShutdown.IsEnabled = smthSelected;
            btnInputDisable.IsEnabled = smthSelected;
            btnInputEnable.IsEnabled = smthSelected;
            btnNetDisable.IsEnabled = smthSelected;
            btnNetEnable.IsEnabled = smthSelected;
            BtnScrShare.IsEnabled = smthSelected;
            btnStopSharing.IsEnabled = smthSelected;
            Monitor_clients.IsEnabled = smthSelected;
            if (!isSelectionByCmbbx)
                cmbSelectionClients.SelectedIndex = 1;
        }

        private void btnInputDisable_Click(object sender, RoutedEventArgs e)
        {
            var clients = getSelectedClients();
            SetFeatureActivity(Feature.INPUT, clients, true);
            service.InputDisable(clients);
        }

        private void btnInputEnable_Click(object sender, RoutedEventArgs e)
        {
            var clients = getSelectedClients();
            SetFeatureActivity(Feature.INPUT, clients, false);
            service.InputEnable(clients);
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            var clients = getSelectedClients();
            SetFeatureActivity(Feature.SHARESCR, clients, true);
            service.StartScreenSharing(clients);
        }

        private void btnNetDisable_Click(object sender, RoutedEventArgs e)
        {
            var clients = getSelectedClients();
            SetFeatureActivity(Feature.WEB, clients, true);
            service.NetDisable(clients);
        }

        private void btnNetEnable_Click(object sender, RoutedEventArgs e)
        {
            var clients = getSelectedClients();
            SetFeatureActivity(Feature.WEB, clients, false);
            service.NetEnable(clients);
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            service.StopAndClean();
            lock (service.Key)
            {
                Monitor.Pulse(service.Key);
            }
        }

        private void SelectClients(List<LabClient> clients)
        {
            dgrClients.SelectedItems.Clear();
            foreach (var client in clients)
            {
                dgrClients.SelectedItems.Add(client);
            }
        }

        //TODO Make a dialog window, that specifics if you want to close LabRun or not
        private void MenuItemExit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void cmbSelectionClients_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var clients = (List<LabClient>) dgrClients.ItemsSource;

            var selection = ((ComboBoxItem) cmbSelectionClients.SelectedItem).Tag.ToString();
            switch (selection)
            {
                case "all":
                {
                    isSelectionByCmbbx = true;
                    SelectClients(clients);
                    break;
                }
                case "none":
                {
                    isSelectionByCmbbx = true;
                    dgrClients.SelectedItems.Clear();
                    break;
                }
                case "odd":
                {
                    isSelectionByCmbbx = true;
                    clients = clients.Where(i => i.BoothNo%2 != 0).ToList();
                    SelectClients(clients);
                    break;
                }
                case "even":
                {
                    isSelectionByCmbbx = true;
                    clients = clients.Where(i => i.BoothNo%2 == 0).ToList();
                    SelectClients(clients);
                    break;
                }
                case "zigzag":
                {
                    isSelectionByCmbbx = true;
                    var clientsSelected = new List<LabClient>();
                    var even = true;
                    var odd = false;

                    foreach (var client in clients)
                    {
                        //Selecting every second odd
                        if (client.BoothNo%2 == 0)
                        {
                            if (odd)
                            {
                                odd = false;
                                clientsSelected.Add(client);
                            }
                            else
                                odd = true;
                        }

                        //Selecting every first even
                        if ((client.BoothNo%2 != 0) && client.BoothNo != null)
                        {
                            if (even)
                            {
                                even = false;
                                clientsSelected.Add(client);
                            }
                            else
                                even = true;
                        }
                    }
                    SelectClients(clientsSelected);
                    break;
                }
                case "zagzig":
                {
                    isSelectionByCmbbx = true;
                    var clientsSelected = new List<LabClient>();
                    var even = false;
                    var odd = true;

                    foreach (var client in clients)
                    {
                        //Selecting every first odd
                        if (client.BoothNo%2 == 0)
                        {
                            if (odd)
                            {
                                odd = false;
                                clientsSelected.Add(client);
                            }
                            else
                                odd = true;
                        }

                        //Selecting every second even
                        if ((client.BoothNo%2 != 0) && client.BoothNo != null)
                        {
                            if (even)
                            {
                                even = false;
                                clientsSelected.Add(client);
                            }
                            else
                                even = true;
                        }
                    }
                    SelectClients(clientsSelected);
                    break;
                }
            }
        }

        private void btnStopSharing_Click(object sender, RoutedEventArgs e)
        {
            var clients = getSelectedClients();
            SetFeatureActivity(Feature.SHARESCR, clients, false);
            service.StopScreenSharing(clients);
        }

        private void dgrClients_MouseLeftButtonUp_1(object sender, MouseButtonEventArgs e)
        {
            isSelectionByCmbbx = false;
            cmbSelectionClients.SelectedIndex = 1;
        }

        private void dgrClients_MouseUp_1(object sender, MouseButtonEventArgs e)
        {
            isSelectionByCmbbx = false;
            cmbSelectionClients.SelectedIndex = 1;
        }

        private void btnSelProject_Click(object sender, RoutedEventArgs e)
        {
            //if(project Wnd is already open, just show in foreground)
            if (projectWnd != null)
            {
                projectWnd.Focus();
                return;
            }

            //if(projectName Wnd is already open, just show in foreground)
            if (projectNameWnd != null)
            {
                projectNameWnd.Focus();
                return;
            }

            if (service.LoggedIn())
            {
                projectWnd = new Project(this);
                projectWnd.Closed += (senders, args) => projectWnd = null;
                projectWnd.Show();
            }
            else
            {
                projectNameWnd = new ProjectName(this, Project);
                projectNameWnd.Closed += (senders, args) => projectNameWnd = null;
                projectNameWnd.Show();
            }
        }

        private void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            if (!service.LoggedIn())
            {
                //if(login is already open, just show in foreground)
                if (login != null)
                {
                    login.Focus();
                    return;
                }
                login = new Login(this);
                login.Closed += (senders, args) => login = null;
                login.Show();
            }
            else
            {
                service.LogOut();
                btnLogin.Content = "Login";
                lblLogin.Content = "Logged in as Guest";
                btnSelProject.Content = "Set Your Project";
                SetProject(unnamedProject);
            }
        }

        public void SetLogin(User user)
        {
            lblLogin.Content = "Logged in as " + user.Username;
            btnSelProject.Content = "Choose Your Project";
            btnLogin.Content = "Logout";
        }

        private void AURPS_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://aucobe.sona-systems.com/");
        }

        private void AUCBL_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://cobelab.au.dk/");
        }

        private void COBELAB_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("http://bss.au.dk/research/research-labs/cognition-and-behavior-lab/");
        }

        private void BtnFwUpdate(object sender, RoutedEventArgs e)
        {
            var result =
                MessageBox.Show(
                    "This will update firewall rules for lab computers to allow speedy remote launches. Make sure that all lab computers are turned on.\n\nAre you sure you want to continue?",
                    "Are you sure?", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            try
            {
                var clients = getSelectedClients();
                service.UpdateFirewallRules(clients);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Unable to get IP Address", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BRIDGE_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("http://cobelab.au.dk:8000");
        }

        private void btnConfEdit(object sender, RoutedEventArgs e)
        {
            Process.Start("config.ini");
        }

        private void Monitor_clients_Click(object sender, RoutedEventArgs e)
        {
            var clients = getSelectedClients();
            service.MonitorLabclients(clients);
        }

        private void AddComputer(object sender, RoutedEventArgs e)
        {
            new NewComputer().Show();
        }

        public void DeleteCookies(object sender, RoutedEventArgs e)
        {
            var clients = getSelectedClients();
            ThreadStart tssingle = delegate
            {
                service.GetCurrentLoggedOnUser(clients);
                service.DeleteCookies(clients);
            };
            service.RunInNewThread(tssingle);
        }

        public void StartMSMQService()
        {
            // removes all messages on start up

            msMqHelp = new MessageQueue("FormatName:Direct=OS:" + Environment.MachineName.ToLower() + "\\private$\\requesthelpqueue");
            if (System.Messaging.MessageQueue.Exists(".\\private$\\requesthelpqueue"))
            {
                msMqHelp.Purge();
                if (msMqHelp != null)
                {
                    msMqHelp.BeginReceive();
                }
                msMqHelp.ReceiveCompleted += new ReceiveCompletedEventHandler(mq_ReceiveCompletedRequestHelpQ);
            }
            else
            {
                msMqHelp = System.Messaging.MessageQueue.Create(".\\private$\\requesthelpqueue", true);
            }

            msMqNotify = new MessageQueue("FormatName:Direct=OS:" + Environment.MachineName.ToLower() + "\\private$\\notifyqueue");
            if (System.Messaging.MessageQueue.Exists(".\\private$\\notifyqueue"))
            {
                msMqNotify.Purge();
                if (msMqNotify != null)
                {
                    msMqNotify.BeginReceive();
                }
            }
            else
            {
                msMqNotify = System.Messaging.MessageQueue.Create(".\\private$\\notifyqueue", true);
            }
            msMqNotify.ReceiveCompleted += new ReceiveCompletedEventHandler(mq_ReceiveCompletedNotifyQ);


            msMqReply = new MessageQueue("FormatName:Direct=OS:" + Environment.MachineName.ToLower() + "\\private$\\replyqueue");
            if (System.Messaging.MessageQueue.Exists(".\\private$\\replyqueue"))
            {
                msMqReply.Purge();
                if (msMqReply != null)
                {
                    msMqReply.BeginReceive();
                }
                else
                {
                    System.Messaging.MessageQueue.Create(".\\private$\\replyqueue", true);
                }
            }
            msMqReply.ReceiveCompleted += new ReceiveCompletedEventHandler(mq_ReceiveCompletedReplyQ);
        }

        public void mq_ReceiveCompletedRequestHelpQ(object sender, ReceiveCompletedEventArgs e)
        {
            //queue that have received a message
            MessageQueue cmq = (MessageQueue)sender;
            try
            {
                //a message we have received (it is already removed from queue)
                System.Messaging.Message msg = cmq.EndReceive(e.AsyncResult);

                var messageLabel = msg.Label;

                foreach(var client in clients)
                {
                    if (client.ComputerName.Equals(messageLabel))
                    {                 
                        if(client.RoomNo == 2)
                        {
                            // If true display UpperCase
                            bool isCaps = true;

                            //Convert from numbers to letters 1 = A and so on.
                            Char c = (Char)((isCaps ? 65 : 97) + (client.BoothNo - 1));
                            MessageBox.Show("Lab participant at Booth: " + c  + " needs help!");
                        }
                        else
                        {
                            MessageBox.Show("Lab participant at Booth: " + client.BoothNo + " needs help!");
                        }
                    }
                }
            }
            catch
            {
            }
            //refresh queue just in case any changes occurred (optional)
            cmq.Refresh();
            //tell MessageQueue to receive next message when it arrives
            cmq.BeginReceive();
        }


        //#TODO Improve code
        public void mq_ReceiveCompletedNotifyQ(object sender, ReceiveCompletedEventArgs e)
        {
            //queue that have received a message
            MessageQueue cmq = (MessageQueue)sender;
            try
            {
                //a message we have received (it is already removed from queue)
                System.Messaging.Message msg = cmq.EndReceive(e.AsyncResult);

                var messageLabel = msg.Label;

                foreach (var client in clients)
                {
                    if (client.ComputerName.Equals(messageLabel))
                    {
                        DataGridColumn column = null;
                        column = dgrClients.Columns[2];
                        //Create a temp LabClient list to send the result
                        List<LabClient> tLabClients = new List<LabClient>();
                        tLabClients.Add(client);
                        SetFeatureActivity(Feature.NOTIFY, tLabClients, true);
                    }
                }
            }
            catch
            {
            }
            //refresh queue just in case any changes occurred (optional)
            cmq.Refresh();
            //tell MessageQueue to receive next message when it arrives
            cmq.BeginReceive();
          
        }

        // Used Whenever a LabClient pressed F10, it pops the message from the MessageQueue and displays a MessageBox
        public void mq_ReceiveCompletedReplyQ(object sender, ReceiveCompletedEventArgs e)
        {
            //queue that have received a message
            MessageQueue cmq = (MessageQueue)sender;
            try
            {
                //a message we have received (it is already removed from queue)
                System.Messaging.Message msg = cmq.EndReceive(e.AsyncResult);

                var messageLabel = msg.Label;

                foreach (var client in clients)
                {
                    if (client.ComputerName.Equals(messageLabel))
                    {
                        msg.Formatter = new ActiveXMessageFormatter();
                        StreamReader sr = new StreamReader(msg.BodyStream);
                        string msgBody = sr.ReadToEnd();
                        if (msgBody.Contains("succesful"))
                        {
                            Debug.Write("Ok");
                            // everything went smooth
                        }
                        else
                        {
                            MessageBox.Show("Your command failed");
                            Debug.Write("Failed sending to " + client.ComputerName);
                        }
                      
                    }         
                }
            }
            catch
            {
            }
            //refresh queue just in case any changes occurred (optional)
            cmq.Refresh();
            //tell MessageQueue to receive next message when it arrives
            cmq.BeginReceive();
        }

        private void InstallMQ(object sender, RoutedEventArgs e)
        {
            var clients = getSelectedClients();
            service.InstallMSMQ(clients);
        }

        private void btnClearNotify_Click(object sender, RoutedEventArgs e)
        {
            bool active = false;
            clients.ForEach(i => i.Notify = active);
    }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            ReplaceComputer rcGui = new ReplaceComputer();
            rcGui.Show();
        }
    }
}