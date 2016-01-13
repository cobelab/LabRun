using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Xceed.Wpf.Toolkit;
using ServiceLibrary;

namespace UserControls
{
    /// <summary>
    /// Interaction logic for DMSValidation.xaml
    /// </summary>
    public partial class DMSValidation : Window
    {
        public DMSValidation()
        {
            InitializeComponent();

            Service.GetInstance().ProgressUpdate += (s, e) =>
            {
                Dispatcher.Invoke(delegate
                {
                    var args = (StatusEventArgs)e;
                    txtStatus.Text = args.Message;
                }
                    );
            };
        }
    }
}
