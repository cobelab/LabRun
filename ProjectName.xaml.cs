using System.Windows;
using System.Windows.Controls;

namespace LabRun
{
    /// <summary>
    ///     Interaction logic for ProjectName.xaml
    /// </summary>
    public partial class ProjectName : Window
    {
        private readonly MainWindow parent;
        private string project = "";

        public ProjectName(MainWindow parent, string project)
        {
            InitializeComponent();
            this.parent = parent;
            Owner = parent;
            this.project = project;
            txbProjectName.Text = project;
            txbProjectName.Focus();
            txbProjectName.SelectAll();
        }

        private void btnOk_Click(object sender, RoutedEventArgs e)
        {
            var projectName = txbProjectName.Text;
            parent.SetProject(projectName, true);
            Close();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void txbProjectName_TextChanged(object sender, TextChangedEventArgs e)
        {
            var content = ((TextBox) e.Source).Text;
            btnOk.IsEnabled = !content.Equals("");
        }
    }
}