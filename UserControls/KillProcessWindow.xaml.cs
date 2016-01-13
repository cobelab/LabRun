using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using ServiceLibrary;

namespace UserControls
{
    /// <summary>
    ///     Interaction logic for KillProcessWindow.xaml
    /// </summary>
    public partial class KillProcessWindow : Window
    {
        private readonly List<string> allProcList = new List<string>();
        private readonly CustomRun parent;
        private readonly HashSet<string> procList;
        private readonly Service service = Service.GetInstance();

        public KillProcessWindow(CustomRun parent)
        {
            InitializeComponent();
            this.parent = parent;
            foreach (var client in parent.getParent().getSelectedClients())
            {
                foreach (var temp in service.CompAndProcesseses)
                {
                    if (temp.computer == client)
                        allProcList.Add(temp.processName);
                }
            }
            procList = new HashSet<string>(allProcList);
            lstbxProcesses.ItemsSource = procList;
        }

        private void btnKill_Click(object sender, RoutedEventArgs e)
        {
            btnKill.IsEnabled = true;
            var exename = (string) lstbxProcesses.SelectedValue;
            var threadId = "";
            string test1 = exename;
            foreach (var client in parent.getParent().getSelectedClients())
                
            {
                foreach (var temp in service.CompAndProcesseses)
                {
                    if (temp.computer == client && temp.processName == exename)
                    {
                       
                        threadId = temp.threadID;
                    }
                }
            }
            service.KillRemoteProcess(parent.getParent().getSelectedClients(), threadId);
            procList.Remove(exename);
            lstbxProcesses.ItemsSource = null;
            lstbxProcesses.ItemsSource = procList;
            parent.ProcessStopped(exename);
        }

        private void lstbxProcesses_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            btnKill.IsEnabled = true;
        }
    }
}