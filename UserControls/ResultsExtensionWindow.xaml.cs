using System.Collections.Generic;
using System.Windows;

namespace UserControls
{
    /// <summary>
    ///     Interaction logic for Window1.xaml
    /// </summary>
    public partial class ResultsExtensionWindow : Window
    {
        private readonly List<StringValue> extensions = new List<StringValue>();
        private readonly CustomRun parent;

        public ResultsExtensionWindow(CustomRun parent, List<string> paramExtensions)
        {
            InitializeComponent();
            if (paramExtensions != null)
            {
                foreach (var temp in paramExtensions)
                {
                    extensions.Add(new StringValue(temp));
                }
            }
            dgrExtensions.ItemsSource = extensions;
            this.parent = parent;
        }

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            if (txtbxExtField.Text.Trim().Length != 0)
            {
                var str = new StringValue(txtbxExtField.Text);
                extensions.Add(str);
                dgrExtensions.Items.Refresh();
            }
        }

        private void btnRemove_Click(object sender, RoutedEventArgs e)
        {
            extensions.Remove((StringValue) dgrExtensions.SelectedItem);
            dgrExtensions.Items.Refresh();
        }

        private void btnDone_Click(object sender, RoutedEventArgs e)
        {
            var strExtensions = new List<string>();
            foreach (var temp in extensions)
            {
                strExtensions.Add(temp.Value);
            }
            parent.extensions = strExtensions;
            Close();
        }

        public class StringValue
        {
            public StringValue(string s)
            {
                Value = s;
            }

            public string Value { get; set; }
        }
    }
}