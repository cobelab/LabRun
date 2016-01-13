using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

namespace ServiceLibrary
{
    public class Config
    {
        public Config(string configFile)
        {
            if (File.Exists(configFile))
            {
                ReadFile(configFile);
            }
            else
            {
                CreateFile(configFile);
                MessageBox.Show("Please enter your configs file into config.ini", "", MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        public string Psychopy { get; set; }
        public string EPrime { get; set; }
        public string Ztreeadmin { get; set; }
        public string Ztreeleaf { get; set; }
        public string Ztreedump { get; set; }
        public string Chrome { get; set; }
        public string DmsUpload { get; set; }
        public string CustomRun { get; set; }
        public string Sharednetworkdrive { get; set; }
        public string Sharednetworkdriveresult { get; set; }
        public string Computerinstallpath { get; set; }
        public string PsTools { get; set; }
        public string Viewer { get; set; }
        public string DMSLocalPath { get; set; }
        public string StormDb { get; set; }

        private void ReadFile(string configFile)
        {
            using (var file = new StreamReader(configFile))
            {
                string line;
                while ((line = file.ReadLine()) != null)
                {
                    if (line.StartsWith("psychopy="))
                    {
                        Psychopy = line.Remove(0, "psychopy=".Length);
                    }

                    if (line.StartsWith("eprime="))
                    {
                        EPrime = line.Remove(0, "eprime=".Length);
                    }

                    if (line.StartsWith("ztreeadmin="))
                    {
                        Ztreeadmin = line.Remove(0, "ztreeadmin=".Length);
                    }

                    if (line.StartsWith("ztreeleaf="))
                    {
                        Ztreeleaf = line.Remove(0, "ztreeleaf=".Length);
                    }

                    if (line.StartsWith("ztreedump="))
                    {
                        Ztreedump = line.Remove(0, "ztreedump=".Length);
                    }

                    if (line.StartsWith("chrome="))
                    {
                        Chrome = line.Remove(0, "chrome=".Length);
                    }

                    if (line.StartsWith("dmsupload="))
                    {
                        DmsUpload = line.Remove(0, "dmsupload=".Length);
                    }

                    if (line.StartsWith("sharednetworkdrive="))
                    {
                        Sharednetworkdrive = line.Remove(0, "sharednetworkdrive=".Length);
                    }
                    if (line.StartsWith("sharednetworkdriveresult="))
                    {
                        Sharednetworkdriveresult = line.Remove(0, "sharednetworkdriveresult=".Length);
                    }
                    if (line.StartsWith("computerinstallpath="))
                    {
                        Computerinstallpath = line.Remove(0, "computerinstallpath=".Length);
                    }
                    if (line.StartsWith("pstools="))
                    {
                        PsTools = line.Remove(0, "pstools=".Length);
                    }
                    if (line.StartsWith("tightVNCviewer="))
                    {
                        Viewer = line.Remove(0, "tightVNCviewer=".Length);
                    }
                    if (line.StartsWith("DMSLocalPath="))
                    {
                        DMSLocalPath = line.Remove(0, "DMSLocalPath=".Length);
                    }
                    if (line.StartsWith("EnableStormDb="))
                    {
                        StormDb = line.Remove(0, "EnableStormDb=".Length);
                    }
                }
            }
        }

        public void CreateFile(string configFile)
        {
            using (var file = new StreamWriter(configFile))
            {
                file.WriteLine("## Paths should be ending without slash");
                file.WriteLine("psychopy=");

                file.WriteLine("eprime=");

                file.WriteLine("## Ztree: ztreleaf and ztreedump points to the labclients path");
                file.WriteLine("ztreeadmin=");

                file.WriteLine("ztreeleaf=");

                file.WriteLine("ztreedump=");

                file.WriteLine("chrome=");

                file.WriteLine("dmsupload=");

                file.WriteLine("sharednetworkdrive=");

                file.WriteLine("sharednetworkdriveresult");

                file.WriteLine("computerinstallpath=");

                file.WriteLine("pstools=");

                file.WriteLine("tightVNCviewer=");

                file.WriteLine("DMSLocalPath=");

                file.WriteLine("EnableStormDb");

                file.Close();
            }
            Process.Start("config.ini");
        }
    }
}