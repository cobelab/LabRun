using System;
using System.IO;
using System.Windows;
using ServiceLibrary;
using System.Text;
using System.Net.NetworkInformation;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace UserControls
{
    /// <summary>
    /// Interaction logic for ReplaceComputer.xaml
    /// </summary>
    public partial class ReplaceComputer : Window
    {
        Service service = Service.GetInstance();
        public ReplaceComputer()
        {
           
            InitializeComponent();
            listBox.ItemsSource = service.GetLabComputersFromStorage();
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            
            LabClient lc = (LabClient)listBox.SelectedItem;
            if(lc != null) { 
            string line;
            string text = File.ReadAllText("clients.txt");
            using (var file = File.Open("clients.txt", FileMode.Open, FileAccess.ReadWrite))
            {
                var reader = new StreamReader(file);
                while((line = reader.ReadLine()) != null)
                {
                    if (line.Contains(lc.ComputerName))
                    {
                        if(Regex.IsMatch(macAdresstxb.Text, "^(?:[0-9a-fA-F]{2}:){5}[0-9a-fA-F]{2}|(?:[0-9a-fA-F]{2}-){5}[0-9a-fA-F]{2}|(?:[0-9a-fA-F]{2}){5}[0-9a-fA-F]{2}$"))
                        {               
                        lc.ComputerName = computerNametxb.Text;
                        lc.Mac = macAdresstxb.Text;
                        StringBuilder newFile = new StringBuilder();
                      
                        string newline = lc.BoothName + " " + lc.BoothNo + " " + lc.ComputerName + " "+ lc.Mac;
                        text = text.Replace(line, newline);

                        MessageBox.Show("The computer was replaced");
                        }
                        else
                        {
                            MessageBox.Show("Mac address is in a wrong format");
                        }
                    }
                }
            }
            File.WriteAllText("clients.txt", text);
            listBox.ItemsSource = null;
            listBox.ItemsSource = service.GetLabComputersFromStorage();
        }
        }


        private void button_Click_1(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
