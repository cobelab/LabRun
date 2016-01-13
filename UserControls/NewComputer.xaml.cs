using System;
using System.IO;
using System.Windows;
using ServiceLibrary;

namespace UserControls
{
    /// <summary>
    ///     Interaction logic for NewComputer.xaml
    /// </summary>
    public partial class NewComputer : Window
    {
        public NewComputer()
        {
            InitializeComponent();
        }

        private void btnAddComputer_Click(object sender, RoutedEventArgs e)
        {
            var clientListString = "";
            clientListString += Convert.ToInt32(txRoomNr.Text) + " " + Convert.ToInt32(txtbNr.Text) + " " + txbName.Text +
                                " " +
                                "" + " " + txbMacAddress.Text + Environment.NewLine;
            var fileClients = new StreamWriter("clients.txt", true);
            fileClients.Write(clientListString);
            fileClients.Close();
            var lc = new LabClient(Convert.ToInt32(txRoomNr.Text), txbName.Text, Convert.ToInt32(txtbNr.Text),
                txbMacAddress.Text);
            Service.GetInstance().clientlist.Add(lc);

            MessageBox.Show(
                "The LabClient has been added, if its turn on it should appear on the list with a white background");
            Close();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}