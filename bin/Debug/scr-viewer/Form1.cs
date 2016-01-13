// Decompiled with JetBrains decompiler
// Type: Form1
// Assembly: scr-viewer, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 842478C8-C819-4DCD-988A-7CDC2A89AD3B
// Assembly location: C:\LabRun\external_source_code_library\scr-viewer\scr-viewer.exe

using AxRDPCOMAPILib;
using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

public class Form1 : Form
{
  private IContainer components = (IContainer) null;
  private AxRDPViewer axRDPViewer1;
  private Panel panel1;

    private static readonly string configFile =
          Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), @"config.ini");

    public string host { get; set; }
  public string keypath { get; set;  }

  public Form1()
  {
    this.InitializeComponent();
      if (!File.Exists(configFile))
      {
          System.Threading.Thread.Sleep(3000);
          try
          {
              if (!File.Exists("config.ini"))
              {
              }


          }
          catch (FileNotFoundException)
          {
              MessageBox.Show("Be sure that you got a config.ini file, with keypath=pathtokey");
          }
      }
      using (System.IO.StreamReader file = new System.IO.StreamReader(configFile))
      {
          string line;
          while ((line = file.ReadLine()) != null)
          {
              if (line.StartsWith(@"#"))
              {
                  continue;
              }

              if (line.StartsWith("keypath="))
              {
                  keypath = Path.Combine(line.Remove(0, "keypath=".Length + 1), "lr-temp", "rds-key.txt");

              }

          }
      }
        using (StreamReader streamReader = new StreamReader(@"\\" + keypath))
      this.host = streamReader.ReadLine();
    this.axRDPViewer1.Connect(this.host, "User1", "");
  }

  private void button1_Click(object sender, EventArgs e)
  {
    this.axRDPViewer1.Connect(this.host, "User1", "");
  }

  private void button2_Click(object sender, EventArgs e)
  {
    this.axRDPViewer1.Disconnect();
  }

  private void Form1_Load(object sender, EventArgs e)
  {
    this.panel1.Height = Screen.PrimaryScreen.Bounds.Height - 100;
  }

  private void button3_Click(object sender, EventArgs e)
  {
    int num = (int) MessageBox.Show(this.host);
  }

  protected override void Dispose(bool disposing)
  {
    if (disposing && this.components != null)
      this.components.Dispose();
    base.Dispose(disposing);
  }

  private void InitializeComponent()
  {
    ComponentResourceManager componentResourceManager = new ComponentResourceManager(typeof (Form1));
    this.axRDPViewer1 = new AxRDPViewer();
    this.panel1 = new Panel();
    this.axRDPViewer1.BeginInit();
    this.panel1.SuspendLayout();
    this.SuspendLayout();
    this.axRDPViewer1.Dock = DockStyle.Fill;
    this.axRDPViewer1.Enabled = true;
    this.axRDPViewer1.Location = new Point(0, 0);
    this.axRDPViewer1.Margin = new Padding(4, 5, 4, 5);
    this.axRDPViewer1.Name = "axRDPViewer1";
    this.axRDPViewer1.Size = new Size(618, 554);
    this.axRDPViewer1.TabIndex = 0;
    this.panel1.Controls.Add((Control) this.axRDPViewer1);
    this.panel1.Dock = DockStyle.Bottom;
    this.panel1.Location = new Point(0, 63);
    this.panel1.Margin = new Padding(4, 5, 4, 5);
    this.panel1.Name = "panel1";
    this.panel1.Size = new Size(618, 554);
    this.panel1.TabIndex = 4;
    this.AllowDrop = true;
    this.AutoScaleDimensions = new SizeF(9f, 20f);
    this.AutoScaleMode = AutoScaleMode.Font;
    this.ClientSize = new Size(618, 617);
    this.Controls.Add((Control) this.panel1);
    this.Margin = new Padding(4, 5, 4, 5);
    this.Name = "Form1";
    this.StartPosition = FormStartPosition.CenterScreen;
    this.Text = "LabRun Screen Viewer";
    this.WindowState = FormWindowState.Maximized;
    this.Load += new EventHandler(this.Form1_Load);
    this.axRDPViewer1.EndInit();
    this.panel1.ResumeLayout(false);
    this.ResumeLayout(false);
  }
}
