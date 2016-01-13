using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using ServiceLibrary;

namespace UserControls
{
    public partial class ChromeTab : UserControl, ControlUnit
    {
        private readonly MainUI parent;
        private readonly Service service = Service.GetInstance();

        public ChromeTab(MainUI parent)
        {
            InitializeComponent();
            this.parent = parent;
        }

        public TabItem TabItem { get; set; }

        public void ButtonClickable(bool enabled)
        {
            btnRun.IsEnabled = enabled;
            btnClose.IsEnabled = enabled;
        }

        public void SetProject(string projectName)
        {
            //nothing :)
        }

        public void setTestLogo(string path)
        {
            var uriSource = new Uri(path, UriKind.Relative);
            imgTest.Source = new BitmapImage(uriSource);
        }

        private void btnRun_Click(object sender, RoutedEventArgs e)
        {
            var clients = parent.getSelectedClients();
            var param1 = "";
            var newWindowMode = ((ComboBoxItem) cmbBoxNewWindowMode.SelectedItem).Tag.ToString();
            switch (newWindowMode)
            {
                case "newtab":
                {
                    break;
                }
                case "newwindow":
                {
                    param1 = " --new-window";
                    break;
                }
                case "newchrome":
                {
                    foreach (var client in clients)
                    {
                        service.KillRemoteProcess(client.ComputerName, "Chrome.exe");
                    }
                    break;
                }
            }

            var param = param1 + "-incognito ";
            var windowMode = ((ComboBoxItem) cmbBoxWindowMode.SelectedItem).Tag.ToString();
            switch (windowMode)
            {
                case "1":
                {
                    param += " -start-maximized";
                    break;
                }
                case "2":
                {
                    param += " -start-fullscreen";
                    break;
                }
                case "3":
                {
                    param += " -kiosk";
                    break;
                }
            }
            param += " " + urlTxtBox.Text;
            parent.SetTabActivity(TabItem, clients, true);
            service.RunRemoteProgram(clients, service.Config.Chrome, param);
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            var clients = parent.getSelectedClients();
            parent.SetTabActivity(TabItem, clients, false);
            service.KillRemoteProcess(clients, "Chrome");
        }

        public void MenuItem_Click(string click)
        {
        }
    }
}