using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ServiceLibrary;

namespace LabRun
{
    /// <summary>
    ///     Interaction logic for Project.xaml
    /// </summary>
    public partial class Project : Window
    {
        private readonly MainWindow parent;
        private readonly List<string> projects = new List<string>();
        private readonly Service service = Service.GetInstance();

        public Project(MainWindow parent)
        {
            InitializeComponent();
            this.parent = parent;
            Owner = parent;
            projects = service.GetProjects();
            lstProjects.ItemsSource = projects;
        }

        private void UpdateSelectedProject(string project)
        {
            if (project == null)
                return;
            parent.SetProject(project, true);
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            var project = (string) lstProjects.SelectedValue;
            UpdateSelectedProject(project);
            Close();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void lstProjects_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lstProjects.SelectedIndex != 0)
            {
                btnOK.IsEnabled = true;
            }
            else
            {
                btnOK.IsEnabled = false;
            }
        }

        protected void HandleDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var project = ((ListBoxItem) sender).Content as string; //Casting back to the binded Track
            UpdateSelectedProject(project);
            Close();
        }
    }
}