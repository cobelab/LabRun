using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace ServiceLibrary
{
    /// <summary>
    ///     Relies on correct ztree.vbs launcher settings
    /// </summary>
    public class ZTree : TestApp
    {
        private readonly string dumpFolder;
        private readonly string ztreeAdminExe;

        public ZTree()
            : base("zTree")
        {
            ApplicationExecutableName = service.Config.Ztreeleaf;
            ztreeAdminExe = service.Config.Ztreeadmin;
            dumpFolder = service.Config.Ztreedump;

            resultExts.Add("xls");
            resultExts.Add("sbj");
            resultExts.Add("pay");
            resultExts.Add("pay");
        }

        public void RunAdminZTree()
        {
            new Thread(() =>
            {
                var resultsFolder = Path.Combine(dumpFolder, GetCurrentTimestamp(), applicationName);
                if (!Directory.Exists(resultsFolder))
                {
                    Directory.CreateDirectory(resultsFolder);
                }
                //string path = @"C:\ZTree\ztree.exe";
                //string arguments = @"/language en /privdir " + dumpFolder + @" /datadir " + dumpFolder + @" /gsfdir " + dumpFolder;
                //service.LaunchCommandLineApp(path, arguments);

                //service.LaunchCommandLineApp(@"C:\ZTree\JustRun.vbs", "");
                //Process.Start(@"C:\ZTree\JustRun.vbs");

                var copyPath = Path.Combine(tempPath, "ztreeRun.bat");
                using (var file = new StreamWriter(copyPath))
                {
                    file.WriteLine("@echo off");
                    var line = "cd " + Path.GetDirectoryName(ztreeAdminExe);
                    file.WriteLine(line);

                    line = @"start """" " + Path.GetFileName(ztreeAdminExe) + @" /language en /privdir " + resultsFolder +
                           @" /datadir " + resultsFolder + @" /gsfdir " + resultsFolder;
                    file.WriteLine(line);
                }
                service.StartNewCmdThread(copyPath);
            }).Start();
        }

        public Thread TransferAndRun(List<LabClient> selectedClients, WindowSize windowSize)
        {
            var t = new Thread(() => xcopy(selectedClients, windowSize));
            t.Start();
            return t;
        }

        private void xcopy(List<LabClient> selectedClients, WindowSize windowSize)
        {
            //----run leaves with proper args
            var i = 0;
            foreach (var client in selectedClients)
            {
                var batFileName = Path.Combine(tempPath, "ztreeLeaves" + client.ComputerName + ".bat");
                using (var file = new StreamWriter(batFileName))
                {
                    file.WriteLine("@echo off");
                    var adminCompName = Environment.MachineName;

                    //adds zero in front for <10no leaves
                    var zleafNo = client.BoothNo + "";
                    if (client.BoothNo < 9)
                        zleafNo = "0" + zleafNo;

                    //window setting
                    var windSize = "/size " + windowSize.Width + "x" + windowSize.Height;
                    var windPos = "/position " + windowSize.XPos + "," + windowSize.YPos;

                    var line = @"cmdkey.exe /add:" + client.ComputerName + @" /user:" +
                               service.Credentials.DomainSlashUser + @" /pass:" + service.Credentials.Password;
                    file.WriteLine(line);

                    var runCmd = @"""" + ApplicationExecutableName + @""" /name Zleaf_" + zleafNo + @" /server " +
                                 adminCompName + @" /language en " + windSize + " " + windPos;
                    line = @service.Config.PsTools + @"\PsExec.exe -d -i 1 \\" + client.ComputerName + @" -u " +
                           service.Credentials.DomainSlashUser + @" -p " + service.Credentials.Password + @" " + runCmd;
                    file.WriteLine(line);
                }
                service.StartNewCmdThread(batFileName);
                i++;
            }
            //----end

            //-----notify ui
            service.NotifyStatus("Request Sent");
            //-----end
        }

        public override Thread TransferResults(List<LabClient> clients)
        {
            var t = new Thread(() => ResTransfer(clients));
            t.IsBackground = true;
            t.Start();
            return t;
        }

        private void ResTransfer(List<LabClient> clients)
        {
            //-----copy from ztree dir to local res dir
            var copyPath = Path.Combine(tempPath, "resCopyLocal.bat");
            using (var file = new StreamWriter(copyPath))
            {
                file.WriteLine("@echo off");
                var src = dumpFolder;
                var dst = Path.Combine(service.Config.Computerinstallpath, resultsFolderName, projectName,
                    "ZTreeSubject");
                var line = @"xcopy """ + src + @""" """ + dst + @""" /V /E /Y /Q /I";
                file.WriteLine(line);
            }
            service.StartNewCmdThread(copyPath);
            //-----end

            //-----notify ui
            service.NotifyStatus("Transfer Complete");
            //-----end
        }

        public override void DeleteResults(List<LabClient> clients)
        {
            new Thread(delegate()
            {
                //----del results from local
                //kill ztree.exe first to avoid "files are being using" error
                service.KillLocalProcess("ztree.exe");

                var pathDel = Path.Combine(tempPath, "delResultsFromLocal.bat");
                using (var file = new StreamWriter(pathDel))
                {
                    file.WriteLine("@echo off");
                    var line = @"rmdir /s /q """ + Path.Combine(dumpFolder) + @"""";
                    file.WriteLine(line);
                    line = @"rmdir /s /q """ +
                           Path.Combine(service.Config.Computerinstallpath, resultsFolderName, projectName) + @"""";
                    file.WriteLine(line);
                }
                service.StartNewCmdThread(pathDel);
                //----end

                //-----notify ui
                service.NotifyStatus("Cleaning Complete");
                //-----end
            }).Start();
        }
    }
}