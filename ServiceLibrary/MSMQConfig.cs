using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ServiceLibrary
{
    public class MSMQConfig
    {
        public Boolean MSMQ { get; set; }

        public MSMQConfig(string configFile)
        {
            if (File.Exists(configFile))
            {
                ReadFile(configFile);
            }
            else
            {
                CreateFile(configFile);
                MessageBox.Show("Please enter your MessageQueue settings into config.ini", "", MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }


        private void ReadFile(string configFile)
        {
            using (var file = new StreamReader(configFile))
            {
                string line;
                while ((line = file.ReadLine()) != null)
                {
                    if (line.StartsWith("MSMQ="))
                    {
                        string MSMQTemp = line.Remove(0, "MSMQ=".Length);
                        if(MSMQTemp == "1")
                        {
                            MSMQ = true;
                        }
                        else
                        {
                            MSMQ = false;
                        }
                       
                    }
                }
            }
        }
        private void CreateFile(string configFile)
        {
            using (var file = new StreamWriter(configFile))
            {
                file.WriteLine("## Paths should be ending without slash");
                file.WriteLine("## Type 1 to enable MSMQ, 0 to disable");

                file.WriteLine("MSMQ=");
            }
            Process.Start("MSMQConfig.ini");
        }
       
    }
}
