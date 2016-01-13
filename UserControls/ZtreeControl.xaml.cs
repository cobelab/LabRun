using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using ServiceLibrary;

namespace UserControls
{
    /// <summary>
    ///     Interaction logic for ZtreeControl.xaml
    /// </summary>
    public partial class ZtreeControl : UserControl
    {
        private readonly Service service = Service.GetInstance();
        private readonly ZTree ztree;
        private readonly string _mailMerge =
    Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), @"MailMerge\MailMerge\bin\Debug",
        "MailMerge.exe");

        public ZtreeControl(ZTree ztree)
        {
            InitializeComponent();
            this.ztree = ztree;
            cmbWindowSizes.ItemsSource = service.WindowSizes;
        }

        public WindowSize GetSelectedWindowSize()
        {
            return (WindowSize) cmbWindowSizes.SelectedItem;
        }

        private void btnRunAdminZTree_Click(object sender, RoutedEventArgs e)
        {
            ztree.RunAdminZTree();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                
                Process.Start(_mailMerge);
            }
            catch (Exception ex)
            {
                MessageBox.Show("The program for merging the pay file into a Word doument was not found! \n\n" + ex,
                    "File not found!", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}