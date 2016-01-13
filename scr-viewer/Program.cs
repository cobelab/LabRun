// Decompiled with JetBrains decompiler
// Type: Program
// Assembly: scr-viewer, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 842478C8-C819-4DCD-988A-7CDC2A89AD3B
// Assembly location: C:\LabRun\external_source_code_library\scr-viewer\scr-viewer.exe

using System;
using System.Windows.Forms;

internal static class Program
{
  [STAThread]
  private static void Main(string[] args)
  {
    Application.EnableVisualStyles();
    Application.SetCompatibleTextRenderingDefault(false);
    Form1 form1 = new Form1();
    if ((uint) args.Length > 0U)
      form1.host = args[0];
    Application.Run((Form) form1);
  }
}
