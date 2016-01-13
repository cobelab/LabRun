using System.Windows;

namespace LabRun
{
    /// <summary>
    ///     Interaction logic for About.xaml
    /// </summary>
    public partial class About : Window
    {
        public About(MainWindow parent)
        {
            InitializeComponent();
            Owner = parent;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}