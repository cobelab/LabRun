using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InputBlocker
{
    class CADKiller
    {
        private string path = @"Software\Microsoft\Windows\CurrentVersion\Policies\System";

        public void KillCtrlAltDelete()
        {
            RegistryKey regkey;
            string keyValueInt = "1";
            string subKey = path;

            regkey = Registry.CurrentUser.CreateSubKey(subKey);
            regkey.SetValue("DisableTaskMgr", keyValueInt);
            regkey.Close();
        }

        public void EnableCTRLALTDEL()
        {
            string subKey = path;
            RegistryKey rk = Registry.CurrentUser;
            RegistryKey sk1 = rk.OpenSubKey(subKey);
            if (sk1 != null)
                rk.DeleteSubKeyTree(subKey);
        }
    }
}
