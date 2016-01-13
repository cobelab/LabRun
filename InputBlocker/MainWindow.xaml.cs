using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace InputBlocker
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private CADKiller cadKiller;

        public MainWindow()
        {
            //InitializeComponent();
            KeyboardListener kListener = new KeyboardListener();
            kListener.KeyDown += kListener_KeyDown;
            //cadKiller = new CADKiller();
            //cadKiller.KillCtrlAltDelete();
        }

        void kListener_KeyDown(object sender, RawKeyEventArgs args)
        {
            Debug.WriteLine(args.VKCode);
        }

        ~MainWindow()
        {
            if (cadKiller != null)
                cadKiller.EnableCTRLALTDEL();
        }
    }
}
