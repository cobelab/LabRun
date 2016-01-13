using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using Utilities;
using System.Diagnostics;
using System.Messaging;

namespace LabClientService {
	public partial class Form1 : Form {
		globalKeyboardHook gkh = new globalKeyboardHook();

		public Form1() {
			InitializeComponent();
            Program.StartMSMQService();
            if (System.Diagnostics.Process.GetProcessesByName(System.IO.Path.GetFileNameWithoutExtension(System.Reflection.Assembly.GetEntryAssembly().Location)).Length > 1) {
                System.Diagnostics.Process.GetCurrentProcess().Kill();
            }
        }

		private void Form1_Load(object sender, EventArgs e) {
			gkh.HookedKeys.Add(Keys.F9);
			gkh.HookedKeys.Add(Keys.F10);
			gkh.KeyDown += new KeyEventHandler(gkh_KeyDown);
        
        }

		void gkh_KeyDown(object sender, KeyEventArgs e) {
			e.Handled = true;
            if (Keys.F9 == e.KeyCode)
            {
                Debug.WriteLine("F9");
                MessageQueue msQ = new MessageQueue
                                          ("FormatName:Direct=OS:ylgw036487\\private$\\notifyqueue");
                Program.SendMessage("Ready", msQ);
                
            }
            if (Keys.F10 == e.KeyCode)
            {
                MessageQueue msQ = new MessageQueue
                                          ("FormatName:Direct=OS:ylgw036487\\private$\\requesthelpqueue");
                Program.SendMessage("Need help", msQ);
                Debug.WriteLine("F10");
            }
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            this.Visible = false;
        }
    }
}