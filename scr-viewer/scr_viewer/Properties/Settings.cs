// Decompiled with JetBrains decompiler
// Type: scr_viewer.Properties.Settings
// Assembly: scr-viewer, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 842478C8-C819-4DCD-988A-7CDC2A89AD3B
// Assembly location: C:\LabRun\external_source_code_library\scr-viewer\scr-viewer.exe

using System.CodeDom.Compiler;
using System.Configuration;
using System.Runtime.CompilerServices;

namespace scr_viewer.Properties
{
  [CompilerGenerated]
  [GeneratedCode("Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsSingleFileGenerator", "11.0.0.0")]
  internal sealed class Settings : ApplicationSettingsBase
  {
    private static Settings defaultInstance = (Settings) SettingsBase.Synchronized((SettingsBase) new Settings());

    public static Settings Default
    {
      get
      {
        Settings settings = Settings.defaultInstance;
        return settings;
      }
    }
  }
}
