using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;

namespace ServiceLibrary
{
    public abstract class TestApp
    {
        protected string applicationName;
        protected string completionFileName = "DONE";


        protected string projectName = "";

        protected List<string> resultExts = new List<string>();
        protected string resultsFolderName;
        protected Service service = Service.GetInstance();
        protected string tempPath = Path.GetTempPath();

        //e.g.: "test1.py"
        protected string testFileName;

        //e.g.: "E:\MyTest\test1.py"
        protected string testFilePath;

        //e.g.: "MyTest"
        protected string testFolderName;
        private string timePrint = "";

        protected TestApp(string applicationName /*, string applicationExecutableName, string testFilePath*/)
        {
            this.applicationName = applicationName;
            testFolder = service.Config.Computerinstallpath;
            resultsFolderName = service.ResultsFolderName;
            //this.applicationExecutableName = applicationExecutableName;
        }

        public string ApplicationName
        {
            get { return applicationName; }
        }

        protected string ApplicationExecutableName { get; set; }
        public string Extension { get; set; }
        public string ExtensionDescription { get; set; }
        public string testFolder { get; set; }

        public string ProjectName
        {
            get { return projectName; }
            set { projectName = value; }
        }

        public string AppExeName
        {
            get { return Path.GetFileName(ApplicationExecutableName); }
        }

        //must be called before "Run" action!
        public void Initialize(string testFilePath)
        {
            this.testFilePath = testFilePath;
            testFileName = Path.GetFileName(testFilePath);
            testFolderName = Path.GetFileName(Path.GetDirectoryName(testFilePath));
        }

        //returns timestamp in yyyyMMdd_HHmmss format
        public string GetCurrentTimestamp()
        {
            return Service.GetInstance().GetCurrentTimestamp();
        }

        public virtual Thread TransferAndRun(List<LabClient> selectedClients, bool copyAll)
        {
            var t = new Thread(() => xcopy(selectedClients, copyAll));
            t.Start();
            return t;
        }

        // Used to directory from local PC to sharednetwork
        private void xcopy(List<LabClient> selectedClients, bool copyAll)
        {
            var fileArgs = "/V /Y /Q";
            var folderArgs = "/V /E /Y /Q /I";

            //-----local copy
            var copyPath = Path.Combine(tempPath, "localCopy.bat");
            using (var file = new StreamWriter(copyPath))
            {
                timePrint = GetCurrentTimestamp();

                var srcDir = testFilePath;
                var dstDir = Path.Combine(service.Config.Sharednetworkdrive, applicationName, timePrint) + @"\";
                var args = fileArgs;
                if (copyAll)
                {
                    args = folderArgs;
                    srcDir = Path.GetDirectoryName(testFilePath);
                }

                file.WriteLine("@echo off");

                var line = @"xcopy """ + srcDir + @""" """ + dstDir + @""" " + args;
                file.WriteLine(line);
                if (this is PsychoPy)
                {
                    line = @"xcopy """ +
                           Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                               ((PsychoPy) this).RunWithLogsScript) + @""" """ + dstDir + @""" " + "/V /Y /Q";
                    file.WriteLine(line);
                }
            }
            service.StartNewCmdThread(copyPath);
            //-----end

            //---onecall to client: copy and run
            foreach (var client in selectedClients)
            {
                var copyPathRemote = Path.Combine(tempPath, "remoteCopyRun" + client.ComputerName + ".bat");
                using (var file = new StreamWriter(copyPathRemote))
                {
                    file.WriteLine("@echo off");

                    var srcDir = Path.Combine(service.Config.Sharednetworkdrive, applicationName, timePrint,
                        testFileName);
                    var dstDir =
                        Path.Combine(service.Config.Computerinstallpath, applicationName, timePrint, applicationName) +
                        @"\";
                    var args = fileArgs;
                    if (copyAll)
                    {
                        args = folderArgs;
                        srcDir = Path.Combine(service.Config.Sharednetworkdrive, applicationName, timePrint);
                    }
                    var copyCmd = @"xcopy """ + srcDir + @""" """ + dstDir + @""" " + args;

                    Debug.WriteLine(ApplicationExecutableName + " app exe name");
                    var runCmd = @"""" + Path.Combine(dstDir, Path.GetFileName(testFilePath)) + @"""";
                    if (this is PsychoPy)
                    {
                        //RunCmd = @"""" + ApplicationExecutableName + @""" " + RunCmd;
                        var runWithLogs = Path.Combine(dstDir, ((PsychoPy) this).RunWithLogsScript);
                        runCmd = ApplicationExecutableName + @" """ + runWithLogs + @""" " + @" " + runCmd;
                    }
                    var line = @"cmdkey.exe /add:" + client.ComputerName + @" /user:" +
                               service.Credentials.DomainSlashUser + @" /pass:" + service.Credentials.Password;
                    file.WriteLine(line);

                    line = service.Config.PsTools + @"\PsExec.exe -d -i 1 \\" + client.ComputerName + @" -u " +
                           service.Credentials.DomainSlashUser + @" -p " + service.Credentials.Password + @" cmd /c (" +
                           copyCmd + @" ^& cd """ + dstDir + @""" ^& start """" " + runCmd + @")";
                    file.WriteLine(line);
                }
                service.StartNewCmdThread(copyPathRemote);
            }

            //-----notify ui
            service.NotifyStatus("Request Sent");
            //-----end
        }

        // 
        private bool transferDone(List<LabClient> clients)
        {
            foreach (var client in clients)
            {
                var path = Path.Combine(service.Config.Sharednetworkdriveresult, resultsFolderName, projectName,
                    completionFileName + client.ComputerName);
                Debug.WriteLine(path);
                if (!File.Exists(path))
                {
                    return false;
                }
            }
            return true;
        }

        //TODO Fix this method, use asyncrhonous calls instead
        private void waitForTransferCompletion(List<LabClient> selectedClients)
        {
            long timeoutPeriod = 150000;
            var sleepTime = 5000;

            var timedOut = false;
            var watch = Stopwatch.StartNew();
            while (!transferDone(selectedClients) && !timedOut)
            {
                Thread.Sleep(sleepTime);
                if (watch.ElapsedMilliseconds > timeoutPeriod)
                    timedOut = true;
            }
            watch.Stop();

            if (timedOut)
                throw new TimeoutException();
        }

        public virtual Thread TransferResults(List<LabClient> clients)
        {
            var t = new Thread(() => xcopyResults(clients));
            t.IsBackground = true;
            t.Start();
            return t;
        }

        // from clients to labrun
        private void xcopyResults(List<LabClient> clients)
        {
            //-----clean notify files
            var copyPath = Path.Combine(tempPath, "networkClean.bat");
            using (var file = new StreamWriter(copyPath))
            {
                file.WriteLine("@echo off");
                var line = @"del /s /q " +
                           Path.Combine(service.Config.Sharednetworkdriveresult, resultsFolderName, projectName,
                               completionFileName + @"*");
                file.WriteLine(line);
            }
            service.StartNewCmdThread(copyPath);
            //-----end

            //----copy results from client computers to shared network folder
            var i = 0;
            foreach (var client in clients)
            {
                var copyPathRemote = Path.Combine(tempPath, "remoteResultOne" + client.ComputerName + ".bat");
                using (var file = new StreamWriter(copyPathRemote))
                {
                    file.WriteLine("@echo off");
                    var line = @"cmdkey.exe /add:" + client.ComputerName + @" /user:" +
                               service.Credentials.DomainSlashUser + @" /pass:" + service.Credentials.Password;
                    file.WriteLine(line);

                    var src = Path.Combine(testFolder, applicationName);
                    var dst = Path.Combine(service.Config.Sharednetworkdriveresult, resultsFolderName, projectName,
                        client.BoothName + "");

                    var resultFiles = "";
                    foreach (var resultExt in resultExts)
                    {
                        resultFiles += @"xcopy """ + Path.Combine(src, "*." + resultExt) + @""" """ + dst +
                                       @""" /V /E /Y /Q /I ^& ";
                    }
                    resultFiles = resultFiles.Substring(0, resultFiles.Length - 4);

                    var completionNotifyFile = @"copy NUL " +
                                               Path.Combine(service.Config.Sharednetworkdriveresult, resultsFolderName,
                                                   projectName, completionFileName + client.ComputerName);
                    line = @service.Config.PsTools + @"\PsExec.exe -d -i 1 \\" + client.ComputerName + @" -u " +
                           service.Credentials.DomainSlashUser + @" -p " + service.Credentials.Password + @" cmd /c (" +
                           resultFiles + @" ^& " + completionNotifyFile + @")";
                    file.WriteLine(line);
                }
                service.StartNewCmdThread(copyPathRemote);
                i++;
            }
            //----end

            //check to make sure transfer is completed from all clients
            waitForTransferCompletion(clients);

            //-----copy from network to local
            copyPath = Path.Combine(tempPath, "networkResultsCopy.bat");
            using (var file = new StreamWriter(copyPath))
            {
                file.WriteLine("@echo off");
                var src = Path.Combine(service.Config.Sharednetworkdriveresult, resultsFolderName, projectName);
                var dst = Path.Combine(service.Config.Computerinstallpath, resultsFolderName, projectName);
                var line = @"xcopy """ + src + @""" """ + dst + @""" /V /E /Y /Q /I"
                    /*/Exclude:" + service.ProgramInstallPath + @"Excludes.txt"*/;
                file.WriteLine(line);
                line = @"del /s /q " + Path.Combine(dst, completionFileName + @"*");
                file.WriteLine(line);
                ////delete unneedednotif
                //line = @"del /s /q " + testFolder + @"Results\PsychoPy\DONE*";
                //file.WriteLine(line);
            }
            service.StartNewCmdThread(copyPath);
            //-----end

            //-----delete results from network
            copyPath = Path.Combine(tempPath, "networkResultsDelete.bat");
            using (var file = new StreamWriter(copyPath))
            {
                file.WriteLine("@echo off");
                var line = @"rmdir /s /q """ + Path.Combine(service.Config.Sharednetworkdriveresult, resultsFolderName) +
                           @"""";
                //string line = @"del /s /q " + Path.Combine(service.SharedNetworkFolder, resultsFolderName, "*.*");
                file.WriteLine(line);
            }
            service.StartNewCmdThread(copyPath);
            //string sharedPath = Path.Combine(service.SharedNetworkFolder, resultsFolderName);
            //if (Directory.Exists(sharedPath))
            //    Directory.Delete(sharedPath, true);
            //-----end

            //-----notify ui
            service.NotifyStatus("Transfer Complete");
            //-----end
        }

        public virtual void DeleteResults(List<LabClient> clients)
        {
            ThreadStart ts = delegate
            {
                //----del tests files from client computers
                var i = 0;
                foreach (var client in clients)
                {
                    var copyPathRemote = Path.Combine(tempPath, "remoteTestDel" + client.ComputerName + ".bat");
                    using (var file = new StreamWriter(copyPathRemote))
                    {
                        Debug.WriteLine(copyPathRemote);
                        var path = Path.Combine(service.Config.Computerinstallpath, applicationName);

                        var line = @service.Config.PsTools + @"\PsExec.exe -d -i 1 \\" + client.ComputerName + @" -u " +
                                   service.Credentials.DomainSlashUser + @" -p " + service.Credentials.Password +
                                   @" cmd /c (rmdir /s /q """ + path + @""")";
                        file.WriteLine(line);
                    }
                    service.StartNewCmdThread(copyPathRemote);
                    i++;
                }
                //----end

                //----del results from client computers
                i = 0;
                foreach (var client in clients)
                {
                    var copyPathRemote = Path.Combine(tempPath, "remoteResultDel" + client.ComputerName + ".bat");
                    using (var file = new StreamWriter(copyPathRemote))
                    {
                        Debug.WriteLine(copyPathRemote);
                        var path = Path.Combine(service.Config.Computerinstallpath, applicationName);

                        var line = @service.Config.PsTools + @"\PsExec.exe -d -i 1 \\" + client.ComputerName + @" -u " +
                                   service.Credentials.DomainSlashUser + @" -p " + service.Credentials.Password +
                                   @" cmd /c (rmdir /s /q """ + path + @""")";
                        file.WriteLine(line);
                    }
                    service.StartNewCmdThread(copyPathRemote);
                    i++;
                }
                //----end

                //----del results from local
                var pathDel = Path.Combine(tempPath, "delResultsFromLocal.bat");
                using (var file = new StreamWriter(pathDel))
                {
                    file.WriteLine("@echo off");
                    var line = @"rmdir /s /q """ +
                               Path.Combine(service.Config.Computerinstallpath, resultsFolderName, projectName) + @"""";
                    file.WriteLine(line);
                }
                service.StartNewCmdThread(pathDel);
                //----end

                //-----notify ui
                service.NotifyStatus("Local cleaning complete. Request sent to delete from Labclients");
                //-----end
            };
            service.RunInNewThread(ts);
        }

        public void CreateProjectDir()
        {
            var path = Path.Combine(service.Config.Computerinstallpath, resultsFolderName, projectName);
            Directory.CreateDirectory(path);
        }

        public virtual void OpenResultsFolder()
        {
            var path = Path.Combine(service.Config.Computerinstallpath, resultsFolderName, projectName);
            if (!Directory.Exists(path))
            {
                throw new DirectoryNotFoundException(path);
            }
            Process.Start(path);
        }

        public void ToDms()
        {
            var projPath = Path.Combine(service.Config.Computerinstallpath, resultsFolderName, projectName);
            if (!Directory.Exists(projPath))
            {
                throw new DirectoryNotFoundException(projPath);
            }
            service.NotifyStatus("This can take up to 10 minutes");
            var dms = new Dms();
                
            if(service.Config.StormDb == "0")
            {
                service.RunInNewThread(() => dms.BackupDMSTransfer(projPath, this));
            }
            else
            {
                service.RunInNewThread(() => dms.DmsTransfer(projPath, this));
            }    
        }
    }
}