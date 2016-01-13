using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Management.Automation;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace ServiceLibrary
{
    public class Service
    {
        private static Service _service;

        private static readonly string AuthFile =
            Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), @"auth.ini");

        private static readonly string ConfigFile =
            Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), @"config.ini");

        private static readonly string PythonLaunch =
            Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), @"run_with_logs.py");

        private readonly string _inputBlockApp =
            Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), @"InputBlocker\bin\Debug",
                "InputBlocker.exe");

        public readonly object Key = new object();
        private readonly ScreenShare _screenShare = ScreenShare.getInstance();

        private readonly string _scrViewerApp =
            Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), @"scr-viewer\bin\Debug",
                "scr-viewer.exe");

        public List<LabClient> Clientlist = new List<LabClient>();
        public List<CompAndProcesses> CompAndProcesseses = new List<CompAndProcesses>();
        public Config Config;
        private List<string> _projects = new List<string>();

        private Service()
        {
            var domainName = "";
            var userName = "";
            var userPassword = "";
            try
            {
                using (var file = new StreamReader(AuthFile))
                {
                    string line;
                    while ((line = file.ReadLine()) != null)
                    {
                        if (line.StartsWith("domainName="))
                        {
                            domainName = Path.Combine(line.Remove(0, "domainName=".Length));
                        }

                        if (line.StartsWith("userName="))
                        {
                            userName = Path.Combine(line.Remove(0, "userName=".Length));
                        }

                        if (line.StartsWith("userPassword="))
                        {
                            userPassword = Path.Combine(line.Remove(0, "userPassword=".Length));
                        }
                    }
                    Credentials = new Credentials(domainName, userName, userPassword);

                    ResultsFolderName = "Results";
                    AppActive = true;

                    WindowSizes.Add(new WindowSize("Full Screen", null, null));
                    WindowSizes.Add(new WindowSize("Half Screen Left", 960, 1080));
                    var size = new WindowSize("Half Screen Right", 960, 1080);
                    size.XPos = 960;
                    size.YPos = 0;
                    WindowSizes.Add(size);
                }
            }
            // If auth.ini doesnt exist, this method creates the file with the correct input variables.
            catch (FileNotFoundException ex)
            {
                MessageBox.Show("Please close the program, and enter your credentials into auth.ini", ex.Message,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                using (var file = new StreamWriter("auth.ini"))
                {
                    file.WriteLine("domainName=");
                    file.WriteLine("userName=");
                    file.WriteLine("userPassword=");
                }
            }
            // Checks if clients.txt exist, if it doesn't it creates the file
            if (!File.Exists("clients.txt"))
            {
                File.Create("clients.txt");
            }
            Config = new Config(ConfigFile);
        }

        public Credentials Credentials { get; set; }
        public User User { get; private set; }
        public string ResultsFolderName { get; set; }
        public List<WindowSize> WindowSizes { get; } = new List<WindowSize>();
        private bool AppActive { get; set; }
        public string TempPath { get; } = Path.GetTempPath();
        public event EventHandler ProgressUpdate;

        public static Service GetInstance()
        {
            if (_service == null)
                _service = new Service();
            return _service;
        }

        public void StopAndClean()
        {
            AppActive = false;
        }

        public void StartPingSvc(List<LabClient> clients)
        {
            new Thread(delegate()
            {
                while (AppActive)
                {
                    foreach (var client in clients)
                    {
                        var t = new Thread(delegate()
                        {
                            var success = false;
                            var ping = new Ping();
                            try
                            {
                                var pingReply = ping.Send(client.ComputerName);
                                if (pingReply.Status == IPStatus.Success)
                                {
                                    success = true;
                                }
                            }
                            catch (Exception)
                            {
                                // Do nothing because the computer turns red in the GUI, if it cannot ping it.
                            }

                            client.Active = success;
                        });
                        t.IsBackground = true;
                        t.Start();
                    }
                    //check again after x sec or interrupt if app is stopped - in this example it update the LabClient list every 30sec
                    lock (Key)
                    {
                        Monitor.Wait(Key, new TimeSpan(0, 0, 30));
                    }
                }
            }).Start();
        }

        /// <summary>
        ///     Reads the clients.txt into the program for a list of computers in a specific lab.
        /// </summary>
        /// <returns>List of clients</returns>
        public List<LabClient> GetLabComputersFromStorage()
        {
            using (var file = new StreamReader("clients.txt"))
            {
                int roomNo;
                int boothNo;
                string line;
                string mac;
                string compname;
                string ip;
                while (((line = file.ReadLine()) != null))
                {
                    var data = line.Split(null);
                    foreach (var temp in data)
                    {
                        Debug.WriteLine(temp);
                    }
                    roomNo = int.Parse(data[0]);
                    boothNo = int.Parse(data[1]);
                    compname = data[2];
                    ip = data[3];
                    mac = data[4];
                    var client = new LabClient(roomNo, compname, boothNo, mac, ip);
                    Clientlist.Add(client);
                }
            }

            return Clientlist;
        }

        public List<LabClient> FilterForRoom(List<LabClient> clients, int roomNo)
        {
            var newClients = new List<LabClient>();
            foreach (var client in clients)
            {
                if (client.RoomNo == roomNo)
                    newClients.Add(client);
            }
            return newClients;
        }

        /// <summary>
        ///     Downloads the bridge's list of computers which have MAC and booth number.
        ///     Then connects MACs to IP-s using local ARP pool, and looks up computer names using NBTSTAT from IP address.
        ///     Throws exception if ARP list is not filled up sufficiently or the bridge's client list cannot be downloaded.
        /// </summary>
        /// <returns>List of clients</returns>
        public List<LabClient> GetLabComputersNew2(int labNo)
        {
            var clientlist = new List<LabClient>();
            //Get MAC addresses from Bridge
            try
            {
                var contents = new WebClient().DownloadString("http://10.204.77.17:8000/?downloadcfg=1");
                // Write bridge list to a file.
                var file0 = new StreamWriter("bridgelist.txt");
                file0.WriteLine(contents);
                file0.Close();
            }
            catch (Exception ex)
            {
                throw new WebException("Error trying to reach the bridge's client list. Error:", ex);
            }


            //Get MAC addresses from list
            using (var file = new StreamReader("bridgelist.txt"))
            {
                int roomNo;
                int boothNo;
                string line;
                string mac;
                while (((line = file.ReadLine()) != null) && (line.Length > 10))
                {
                    roomNo = int.Parse(line.Substring(0, 1));
                    mac = line.Substring(4);
                    mac = mac.Replace(" ", string.Empty);
                    mac = mac.Replace(":", string.Empty);
                    mac = mac.Replace("\u0009", string.Empty);
                    boothNo = int.Parse(line.Substring(2, 2).Trim());
                    var client = new LabClient(roomNo, "", boothNo, mac, "");
                    clientlist.Add(client);
                }
            }

            //Get local ARP list to match MAC addresses to IP-s
            var p = new Process();
            p.StartInfo = new ProcessStartInfo(@"C:\Windows\System32\arp.exe", "/a")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                RedirectStandardInput = true,
                CreateNoWindow = true
            };
            p.Start();

            //Store the output to a string
            var Contents = p.StandardOutput.ReadToEnd();

            // Write the string to a file.
            var file2 = new StreamWriter("arp.txt");
            file2.WriteLine(Contents);
            file2.Close();

            //Process file
            using (var file = new StreamReader("arp.txt"))
            {
                string line;
                var mac = "";
                var ip = "";
                var i = 1;
                var cutLines = 3;

                while ((line = file.ReadLine()) != null)
                {
                    if (i <= cutLines)
                    {
                        i++;
                    }
                    else
                    {
                        if (line.Length > 20)
                        {
                            ip = line.Substring(0, 17);
                            ip = ip.Trim();
                            mac = line.Substring(17, 25);
                            mac = mac.Replace(" ", string.Empty);
                            mac = mac.Replace("-", string.Empty);
                            mac = mac.Replace("\u0009", string.Empty);
                        }

                        //Check for matching MACs, if found, update list of clients with IP
                        foreach (var client in clientlist)
                        {
                            if (client.Mac == mac)
                            {
                                client.Ip = ip;
                            }
                        }
                    }
                }

                //Get computer names, match with IP using NBTSTAT

                foreach (var client in clientlist)
                {
                    var contents5 = ExecuteCommand("nbtstat.exe -a " + client.Ip, true);
                    if (contents5.IndexOf("<00>  UNIQUE") == -1)
                    {
                        throw new Exception();
                    }
                    var until = contents5.Substring(0, contents5.IndexOf("<00>  UNIQUE")).Trim();
                    var stringArray = until.Split(null);
                    var name = "";
                    foreach (var str in stringArray)
                    {
                        name = str;
                    }
                    client.ComputerName = name;
                }

                //Write clientlist to file for testing
                var clientListString = "";
                foreach (var client in clientlist)
                {
                    clientListString += client.RoomNo + " " + client.BoothNo + " " + client.ComputerName + " " +
                                        client.Ip + " " + client.Mac + Environment.NewLine;
                    var fileClients = new StreamWriter("clients.txt");
                    fileClients.WriteLine(clientListString);
                    fileClients.Close();
                }
            }
            return clientlist;
        }

        // TODO Old code written by the interns, used for the bridge could be optimized
        public string ExecuteCommand(string command, bool waitForExit = false)
        {
            int exitCode;
            ProcessStartInfo processInfo;
            Process process;

            processInfo = new ProcessStartInfo("cmd.exe", "/c " + command);
            processInfo.CreateNoWindow = true;
            processInfo.UseShellExecute = false;
            // *** Redirect the output ***
            processInfo.RedirectStandardError = true;
            processInfo.RedirectStandardOutput = true;

            process = Process.Start(processInfo);
            if (waitForExit)
                process.WaitForExit();

            // *** Read the streams ***
            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();

            exitCode = process.ExitCode;

            process.Close();
            return output;
        }

        //Copies a selected file to the shared network drive
        public void CopyFilesToNetworkShare(string srcDir, string timestamp)
        {
            var copyPath = Path.Combine(TempPath, "localCopy" + ".bat");
            var sharednPath = Path.Combine(Config.Sharednetworkdrive, "temp", "Custom Run", timestamp);
            using (var file = new StreamWriter(copyPath))

            {
                file.WriteLine("@echo off");
                var dstDir = @sharednPath + @"\";
                var line = @"xcopy """ + srcDir + @""" """ + dstDir + @""" /V /Y /Q";
                file.WriteLine(line);
            }
            _service.StartNewCmdThread(copyPath);
        }

        /// <summary>
        ///     Transfers a file from the shared drive to each selected lab client.
        /// </summary>
        /// <returns>Nothing</returns>
        public void CopyFilesFromNetworkShareToClients(string srcPath, string fileName, List<LabClient> clients,
            string timestamp)
        {
            foreach (var client in clients)
            {
                var batFileName = Path.Combine(TempPath, "CustomCopy" + client.ComputerName + ".bat");
                var sharednPath = Path.Combine(Config.Sharednetworkdrive, "temp", "Custom Run", timestamp,
                    fileName + "*");
                var installPath = Path.Combine(Config.Computerinstallpath, "Custom Run", timestamp);
                using (var file = new StreamWriter(batFileName))
                {
                    file.WriteLine("@echo off");
                    var line = @"cmdkey.exe /add:" + client.ComputerName + @" /user:" +
                               _service.Credentials.DomainSlashUser + @" /pass:" +
                               _service.Credentials.Password;
                    file.WriteLine(line);

                    // Embed xcopy command to transfer ON labclient FROM shared drive TO labclient
                    var copyCmd = @"xcopy """ + @sharednPath + @""" """ + @installPath + @"\" + @""" /S /V /Y /Q";
                    // Deploy and run batfile FROM Server TO labclient using PSTools
                    line = Config.PsTools + @"\PsExec.exe -d -i 1 \\" + client.ComputerName + @" -u " +
                           _service.Credentials.DomainSlashUser + @" -p " + _service.Credentials.Password + @" cmd /c (" +
                           copyCmd + @")";
                    file.WriteLine(line);
                }
                _service.StartNewCmdThread(batFileName);
            }
        }

        /// <summary>
        ///     Transfers and runs a file from the shared drive to each selected lab client.
        /// </summary>
        /// <returns>Nothing</returns>
        public void CopyAndRunFilesFromNetworkShareToClients(string srcPath, string fileName, List<LabClient> clients,
            string param, string timestamp)
        {
            foreach (var client in clients)
            {
                var batFileName = Path.Combine(TempPath, "CustomCopy" + client.ComputerName + ".bat");
                using (var file = new StreamWriter(batFileName))
                {
                    file.WriteLine("@echo off");
                    var line = @"cmdkey.exe /add:" + client.ComputerName + @" /user:" +
                               _service.Credentials.DomainSlashUser + @" /pass:" + _service.Credentials.Password;
                    file.WriteLine(line);

                    var sharednPath = Path.Combine(Config.Sharednetworkdrive, "temp", "Custom Run", timestamp, fileName);
                    var installPath = Path.Combine(Config.Computerinstallpath, "Custom Run", timestamp);
                    // Embed xcopy command to transfer ON labclient FROM shared drive TO labclient
                    var copyCmd = @"xcopy """ + @sharednPath + @""" """ + @installPath + @"\" + @""" /V /Y /Q ";
                    var runCmd = @"""" + installPath + @"\" + fileName + @"""";

                    // Deploy and run batfile FROM Server TO labclient using PSTools
                    line = Config.PsTools + @"\PsExec.exe -d -i 1 \\" + client.ComputerName + @" -u " +
                           _service.Credentials.DomainSlashUser + @" -p " + _service.Credentials.Password + @" cmd /c (" +
                           copyCmd + @" ^& " + runCmd + @")";
                    file.WriteLine(line);
                }
                var t = new Thread(() => RunCmd(batFileName, fileName, client));
                t.Start();
            }
        }

        public void ProjectFilesAndRunIt(List<LabClient> clients, string appConfigName, string appName)
        {
            //-----local copy
            var copyPath = Path.Combine(TempPath, "localCopy.bat");
            using (var file = new StreamWriter(copyPath))
            {
                file.WriteLine("@echo off");
                var srcDir = Path.GetDirectoryName(appConfigName);
                var dstDir = Path.Combine(Config.Sharednetworkdrive, appName);
                var line = @"xcopy """ + srcDir + @""" """ + dstDir + @""" /V /E /Y /Q /I";
                file.WriteLine(line);
            }
            _service.StartNewCmdThread(copyPath);
            //-----end

            //---onecall to client: copy and run
            foreach (var client in clients)
            {
                var copyPathRemote = Path.Combine(TempPath, "remoteCopyRun" + client.ComputerName + ".bat");
                using (var file = new StreamWriter(copyPathRemote))
                {
                    file.WriteLine("@echo off");
                    var line = @"cmdkey.exe /add:" + client.ComputerName + @" /user:" +
                               _service.Credentials.DomainSlashUser + @" /pass:" + _service.Credentials.Password;
                    file.WriteLine(line);
                    var srcDir = Path.Combine(Config.Sharednetworkdrive, appName);
                    var dstDir = Path.Combine(Config.Computerinstallpath, appName);
                    var copyCmd = @"xcopy """ + srcDir + @""" """ + dstDir + @""" /V /E /Y /Q /I";
                    var runLocation = Path.Combine(dstDir, Path.GetFileName(appConfigName));

                    var runCmd = @"start """" """ + runLocation + @"""";
                    line = Config.PsTools + @"\PsExec.exe -d -i 1 \\" + client.ComputerName + @" -u " +
                           _service.Credentials.DomainSlashUser + @" -p " + _service.Credentials.Password +
                           @" cmd /c (" + copyCmd + @" ^& " + runCmd + @")";

                    file.WriteLine(line);
                }
                _service.StartNewCmdThread(copyPathRemote);
            }
        }

        public void InputDisable(List<LabClient> clients)
        {
            var t = new Thread(delegate()
            {
                ProjectFilesAndRunIt(clients, _inputBlockApp, "InputBlocker");

                //-----notify ui
                _service.NotifyStatus("Input Disable Request Sent");
                //-----end
            });
            t.IsBackground = true;
            t.Start();
        }

        public void InputEnable(List<LabClient> clients)
        {
            foreach (var client in clients)
            {
                KillRemoteProcess(client.ComputerName, "InputBlocker.exe");


                //-----notify ui
                NotifyStatus("Input Enabled");
                //-----end
            }
        }

        public void RunRemoteProgram(List<LabClient> compList, string path, string param = "", bool cmdWithQuotes = true)
        {
            foreach (var client in compList)
            {
                var compName = client.ComputerName;
                var copyPathRemote = Path.Combine(TempPath, "remoteRun" + compName + ".bat");

                using (var file = new StreamWriter(copyPathRemote))
                {
                    file.WriteLine("@echo off");
                    var line = @"cmdkey.exe /add:" + client.ComputerName + @" /user:" +
                               _service.Credentials.DomainSlashUser + @" /pass:" + _service.Credentials.Password;
                    file.WriteLine(line);

                    var runCmd = path;
                    if (cmdWithQuotes)
                    {
                        runCmd = @"""" + runCmd + @"""";
                    }
                    runCmd = runCmd + " " + param;
                    line = Config.PsTools + @"\PsExec.exe -d -i 1 \\" + compName + @" -u " +
                           _service.Credentials.DomainSlashUser + @" -p " + _service.Credentials.Password + " " + runCmd;
                    file.WriteLine(line);
                }
                _service.StartNewCmdThread(copyPathRemote);
            }
        }

        // This method is used alot, its used everything you want to start a process on the targets machine or even on your own machine.
        public void RunCmd(string target)
        {
            using (var process = new Process())
            {
                process.StartInfo.FileName = target;
                // Disables the CMD output, and makes it possible to read the error and output stream to get feedback from PSTools
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.RedirectStandardInput = true;

                var output = new StringBuilder();
                var error = new StringBuilder();


                using (var outputWaitHandle = new AutoResetEvent(false))
                using (var errorWaitHandle = new AutoResetEvent(false))
                {
                    process.OutputDataReceived += (sender, e) =>
                    {
                        if (e.Data == null)
                        {
                            try
                            {
                                outputWaitHandle.Set();
                            }
                            catch (Exception)
                            {
                                // Had some troubles it trowed a weird exception that didn't seem to matter anything.
                            }
                        }
                        else
                        {
                            output.AppendLine(e.Data);
                        }
                    };
                    process.ErrorDataReceived += (sender, e) =>
                    {
                        if (e.Data == null)
                        {
                            try
                            {
                                errorWaitHandle.Set();
                            }
                            catch (Exception)
                            {
                                // Had some troubles it trowed a weird exception that didn't seem to matter anything.
                            }
                        }
                        else
                        {
                            error.AppendLine(e.Data);
                        }
                    };
                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                    // If the process waits for more than 15sec type some error stuff.
                    var timeout = 15000;
                    if (process.WaitForExit(timeout) &&
                        outputWaitHandle.WaitOne(timeout) &&
                        errorWaitHandle.WaitOne(timeout))
                    {
                        //Could type some Process.Exist code here but it aint needed.
                        Debug.Write(output);
                        Debug.Write("ERROR:" + error);
                    }
                    Debug.Write("Test");
                }
            }
        }

        // Overloaded method used to store the threads ID and what file gets opened when CustomRun is ran.
        public void RunCmd(string target, string fileName, LabClient client)
        {
            var s = "";
            using (var process = new Process())
            {
                process.StartInfo.FileName = target;
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.RedirectStandardInput = true;

                var output = new StringBuilder();
                var error = new StringBuilder();

                using (var outputWaitHandle = new AutoResetEvent(false))
                using (var errorWaitHandle = new AutoResetEvent(false))
                {
                    process.OutputDataReceived += (sender, e) =>
                    {
                        if (e.Data == null)
                        {
                            outputWaitHandle.Set();
                        }
                    };
                    process.ErrorDataReceived += (sender, e) =>
                    {
                        if (e.Data == null)
                        {
                            errorWaitHandle.Set();
                        }
                        else
                        {
                            if (e.Data.Contains("ID"))
                            {
                                var index = e.Data.IndexOf("ID");
                                var temp = e.Data.Substring(index, e.Data.Length - index - 1);
                                s = temp.Substring(3, temp.Length - 3);

                                Debug.Write("TEST: ", s);
                                var cp1 = new CompAndProcesses();
                                cp1.computer = client;
                                cp1.processName = fileName;
                                cp1.threadID = s;
                                CompAndProcesseses.Add(cp1);
                            }
                            error.AppendLine(e.Data);
                        }
                    };

                    process.Start();

                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                    var timeout = 15000;
                    if (process.WaitForExit(timeout) &&
                        outputWaitHandle.WaitOne(timeout) &&
                        errorWaitHandle.WaitOne(timeout))
                    {
                        //Could type some Process.Exist code here but it aint needed.

                        Debug.Write(output);
                        Debug.Write("ERROR:" + error);
                    }
                }
            }
        }

        // Used to get the current logged in user on a remote computer
        public void RunCmd(string target, LabClient client)
        {
            var s = "";
            using (var process = new Process())
            {
                process.StartInfo.FileName = target;
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.RedirectStandardInput = true;

                var output = new StringBuilder();
                var error = new StringBuilder();
                var domainName = _service.Credentials.DomainName.ToUpper();
                using (var outputWaitHandle = new AutoResetEvent(false))
                using (var errorWaitHandle = new AutoResetEvent(false))
                    //Used to find whatever user that currently is online with the domain Credentials

                {
                    process.OutputDataReceived += (sender, e) =>
                    {
                        if (e.Data == null)
                        {
                            outputWaitHandle.Set();
                        }
                        else
                        {
                            if (e.Data.Contains(domainName))
                            {
                                var index = e.Data.IndexOf(domainName);
                                var temp = e.Data.Substring(index, e.Data.Length - index);
                                s = temp.Substring(4);
                                client.CurrentlyLoggedInUser = s;
                            }
                        }
                    };
                    process.ErrorDataReceived += (sender, e) =>
                    {
                        if (e.Data == null)
                        {
                            errorWaitHandle.Set();
                        }
                        else
                        {
                            error.Append(e.Data);
                        }
                    };

                    process.Start();

                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                    var timeout = 15000;
                    if (process.WaitForExit(timeout) &&
                        outputWaitHandle.WaitOne(timeout) &&
                        errorWaitHandle.WaitOne(timeout))
                    {
                        //Could type some Process.Exist code here but it aint needed.

                        Debug.Write(output);
                        Debug.Write(error);
                    }
                }
            }
        }

        public Thread StartNewCmdThread(string cmd)
        {
            var t = new Thread(() => RunCmd(cmd));
            t.Start();
            return t;
        }

        public void MonitorLabclients(List<LabClient> clients)
        {
            ThreadStart threadStart = delegate
            {
                var line = "";
                foreach (var client in clients)
                {
                    line += " HOST " + client.ComputerName + " PORT 5900" + " PASSWORD 1234 ";
                }

                var startInfo = new ProcessStartInfo("cmd.exe", "/C c:/viewer " + line);
                startInfo.UseShellExecute = false;
                startInfo.RedirectStandardError = true;
                Process.Start(startInfo);
            };
            RunInNewThread(threadStart);
        }

        // Used to kill a local process on the LadAdmins PC e.g Closing ZTree
        public void KillLocalProcess(string processName)
        {
            using (var powershell = PowerShell.Create())
            {
                powershell.AddScript(@"$a = Taskkill /IM " + processName + @" /F");
                powershell.AddScript(@"echo $a");
                var results = powershell.Invoke();
                foreach (var item in results)
                {
                    Debug.WriteLine(item);
                }
                if (powershell.Streams.Error.Count > 0)
                {
                    Debug.WriteLine("{0} errors", powershell.Streams.Error.Count);
                }
            }
        }

        // Runs whatever TreadStart in a new Thread
        public void RunInNewThread(ThreadStart ts)
        {
            var t = new Thread(ts);
            t.IsBackground = true;
            t.Start();
        }

        public void KillRemoteProcess(string computerName, string processName, bool waitForFinish = false)
        {
            KillProcThread(computerName, processName);
        }

        public void KillRemoteProcess(List<LabClient> computers, string processName, bool waitForFinish = false)
        {
            foreach (var client in computers)
            {
                _service.KillRemoteProcess(client.ComputerName, processName, waitForFinish);
            }

            //-----notify ui
            NotifyStatus("Task Kill Completed");
            //-----end
        }

        public void KillProcThread(string computerName, string processName)
        {
            var copyPathRemote = Path.Combine(TempPath, "remoteRun" + computerName + ".bat");
            using (var file = new StreamWriter(copyPathRemote))
            {
                file.WriteLine("@echo off");
                var line = @"cmdkey.exe /add:" + computerName + @" /user:" +
                           _service.Credentials.DomainSlashUser + @" /pass:" + _service.Credentials.Password;
                file.WriteLine(line);

                line = Config.PsTools + @"\PSKill.exe \\" + computerName + @" -u " +
                       _service.Credentials.DomainSlashUser + @" -p " + _service.Credentials.Password + " " + processName;
                file.WriteLine(line);
            }
            _service.StartNewCmdThread(copyPathRemote);
        }

        public string GetLocalIpAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            throw new Exception("Local IP Address Not Found!");
        }

        public void ShutdownComputers(List<LabClient> clients)
        {
            new Thread(() => ShutItThread(clients)).Start();
        }

        private void ShutItThread(List<LabClient> clients)
        {
            Debug.WriteLine("Shutting begins >:)");


            foreach (var client in clients)
            {
                new Thread(delegate()
                {
                    var copyPathRemote = Path.Combine(TempPath, "remoteRun" + client.ComputerName + ".bat");
                    using (var file = new StreamWriter(copyPathRemote))
                    {
                        file.WriteLine("@echo off");
                        var line = @"cmdkey.exe /add:" + client.ComputerName + @" /user:" +
                                   _service.Credentials.DomainSlashUser + @" /pass:" + _service.Credentials.Password;
                        file.WriteLine(line);

                        line = Config.PsTools + @"\PSShutdown.exe \\" + client.ComputerName + @" -u " +
                               _service.Credentials.DomainSlashUser + @" -p " + _service.Credentials.Password +
                               @" -t 10 -m "" Your pc is shutting down in 10s""";
                        file.WriteLine(line);
                    }
                    _service.StartNewCmdThread(copyPathRemote);
                }).Start();
            }

            NotifyStatus("Shutdown request sent");
            //-----end
        }

        public void NotifyStatus(string msg)
        {
            //-----notify ui
            if (ProgressUpdate != null)
                ProgressUpdate(this, new StatusEventArgs(msg));
            //-----end
        }

        public Thread TransferAndRun(List<LabClient> selectedClients)
        {
            var t = new Thread(() => ScrVwrCopyAndRun(selectedClients));
            t.Start();
            return t;
        }

        public void ScrVwrCopyAndRun(List<LabClient> selectedClients)
        {
            var t = new Thread(delegate() { ProjectFilesAndRunIt(selectedClients, _scrViewerApp, "scr-viewer"); });
            t.IsBackground = true;
            t.Start();
        }

        public void NetDisable(List<LabClient> clients)
        {
            ThreadStart threadStart = delegate
            {
                //add new block https rule
                var name = @"""block https""";
                var cmd = @"cmd /c (netsh advfirewall firewall add rule name=" + name +
                          @" protocol=TCP dir=out remoteport=443,80,8080 action=block)";
                RunRemoteProgram(clients, cmd, "", false);

                //-----notify ui
                if (ProgressUpdate != null)
                    ProgressUpdate(this, new StatusEventArgs("Net Access (http(s)) was disabled!"));
                //-----end
            };
            RunInNewThread(threadStart);
        }

        private string GetCurrentMachineIp()
        {
            IPHostEntry host;
            var localIp = "?";
            host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    localIp = ip.ToString();
                }
            }
            return localIp;
        }

        //adds fw rule to labclients to allow psexec from given ip
        public void UpdateFirewallRules(List<LabClient> clients)
        {
            //-----notify ui
            if (ProgressUpdate != null)
                ProgressUpdate(this, new StatusEventArgs("Working..."));

            //gets admin id for admin pc unique fw rule

            var ruleName = "PsExec";

            //gets ip of this machine
            var ip = GetCurrentMachineIp();
            if (ip == "?")
                throw new Exception("Unable to retrieve IP of this machine!");

            ThreadStart threadStart = delegate
            {
                //add new fw rule
                var cmd = @"cmd /c (netsh AdvFirewall firewall add rule name=" + ruleName +
                          @" dir=in action=allow protocol=TCP localport=RPC RemoteIP=" + ip +
                          @" profile=domain,private program=%WinDir%\system32\services.exe service=any)";
                RunRemoteProgram(clients, cmd, "", false);

                //-----notify ui
                if (ProgressUpdate != null)
                    ProgressUpdate(this, new StatusEventArgs("FW rules update request sent"));
            };
            RunInNewThread(threadStart);
        }

        public void NetEnable(List<LabClient> clients)
        {
            ThreadStart threadStart = delegate
            {
                //add new block https rule
                var name = @"""block https""";
                var cmd = @"cmd /c (netsh advfirewall firewall delete rule name=" + name +
                          @" protocol=TCP dir=out remoteport=443,80,8080)";
                RunRemoteProgram(clients, cmd, "", false);

                //-----notify ui
                if (ProgressUpdate != null)
                    ProgressUpdate(this, new StatusEventArgs("Net Access (http(s)) was Enabled!"));
                //-----end
            };
            RunInNewThread(threadStart);
        }

        public void StartScreenSharing(List<LabClient> clients)
        {
            var rdsKeyLocation = Path.Combine(Config.Sharednetworkdrive, "lr-temp", "rds-key.txt");
            _screenShare.Start(clients, rdsKeyLocation);
        }

        public void StopScreenSharing(List<LabClient> clients)
        {
            _screenShare.Stop(clients);
        }

        public User Login(string username, string password)
        {
            var dms = new Dms();
            var user = dms.Login(username, password);
            User = user;
            return user;
        }

        public void LogOut()
        {
            User = null;
        }

        public bool LoggedIn()
        {
            return User != null;
        }

        public void InitProjects()
        {
            var t = new Thread(() => { _projects = new Dms().GetProjects(); });
            t.IsBackground = true;
            t.Start();
        }

        public List<string> GetProjects()
        {
            return _projects;
        }

        public bool LocalProjectExists(string projectName)
        {
            var path = Path.Combine(Config.Computerinstallpath, ResultsFolderName, projectName);
            return Directory.Exists(path);
        }

        public void MoveProject(string oldProject, string newProject)
        {
            var resPath = Path.Combine(Config.Computerinstallpath, ResultsFolderName);
            var oldPath = Path.Combine(resPath, oldProject);
            var newPath = Path.Combine(resPath, newProject);
            //if the new project already exists, rename it with its timestamp
            if (Directory.Exists(newPath))
            {
                var timestamp = string.Format("{0:yyyyMMdd_HHmmss}", Directory.GetLastWriteTime(newPath));
                Directory.Move(newPath, Path.Combine(resPath, newProject + "_" + timestamp));
            }
            //rename old projects name
            Directory.Move(oldPath, newPath);
        }

        /// <summary>
        ///     Deletes the temp directory containing transferred files on the supplied clients.
        /// </summary>
        /// <returns>Nothing</returns>
        public void DeleteFiles(List<LabClient> clients)
        {
            var localPathOnPc = Path.Combine(Config.Computerinstallpath);
            foreach (var client in clients)
            {
                var batFileName = Path.Combine(TempPath, "DeleteTemp" + client.ComputerName + ".bat");
                using (var file = new StreamWriter(batFileName))
                {
                    file.WriteLine("@echo off");
                    var line = @"cmdkey.exe /add:" + client.ComputerName + @" /user:" +
                               _service.Credentials.DomainSlashUser + @" /pass:" + _service.Credentials.Password;
                    file.WriteLine(line);

                    var deleteCmd = @"rmdir " + @"""" + localPathOnPc + @"""\" + @" /S /Q";
                    line = Config.PsTools + @"\PsExec.exe -d -i 1 \\" + client.ComputerName + @" -u " +
                           _service.Credentials.DomainSlashUser + @" -p " + _service.Credentials.Password + @" cmd /c (" +
                           deleteCmd + @")";
                    file.WriteLine(line);
                }
                _service.StartNewCmdThread(batFileName);
            }
        }

        public void GetCurrentLoggedOnUser(List<LabClient> clients)
        {
            foreach (var client in clients)
            {
                var batFileName = Path.Combine(TempPath, "remoteRun" + client.ComputerName + ".bat");
                using (var file = new StreamWriter(batFileName))
                {
                    file.WriteLine("@echo off");
                    var line = Config.PsTools + @"\PsLoggedon.exe -l -x \\" + client.ComputerName;
                    file.WriteLine(line);
                }
                var t = new Thread(() => RunCmd(batFileName, client));
                t.Start();
            }
        }

        public void DeleteCookies(List<LabClient> clients, List<String> browserName)
        {
            if(browserName != null)
            foreach(var name in browserName) { 
            foreach (var client in clients)
            {
                var localPathOnPc =
                    Path.Combine(@"C:\Users\" + client.CurrentlyLoggedInUser +
                                 @"\AppData\Local\Google\Chrome\User Data\Default\Cookies");
                var batFileName = Path.Combine(TempPath, "DeleteCookies" + client.ComputerName + ".bat");
                using (var file = new StreamWriter(batFileName))
                {
                    file.WriteLine("@echo off");
                    var line = @"cmdkey.exe /add:" + client.ComputerName + @" /user:" +
                               _service.Credentials.DomainSlashUser + @" /pass:" + _service.Credentials.Password;
                    file.WriteLine(line);

                    var deleteCmd = @"DEL " + @"""" + localPathOnPc + @"""" + @" /F /Q";
                    line = Config.PsTools + @"\PsExec.exe -d -i 1 \\" + client.ComputerName + @" -u " +
                           _service.Credentials.DomainSlashUser + @" -p " + _service.Credentials.Password + @" cmd /c (" +
                           deleteCmd + @")";
                    file.WriteLine(line);
                }
                _service.StartNewCmdThread(batFileName);
            }
                
        }
        }

        //returns timestamp in yyyyMMdd_HHmmss format
        public string GetCurrentTimestamp()
        {
            var timeStamp = DateTime.Now;
            return string.Format("{0:yyyyMMdd_HHmmss}", timeStamp);
        }

        /// <summary>
        ///     Transfers a folder first to the shared drive then to each selected labclient.
        ///     Uses PSExec delegated batch files, running on each client.
        ///     Then runs selected file using same PSExec batch.
        /// </summary>
        /// <returns>Nothing</returns>
        public void CopyAndRunFolder(List<LabClient> clients, string folderPath, string filePath, string parameter,
            string timestamp)
        {
            //Get folder name without path
            var folderName = "";
            var words = folderPath.Split('\\');
            foreach (var word in words)
            {
                folderName = word;
            }

            // Paths, for both the SharedNetwork 
            var copyPath = Path.Combine(TempPath, "localCopy.bat");
            var dstDir = Path.Combine(Config.Sharednetworkdrive, "temp", "Custom Run", timestamp, folderName);
            var localPath = Path.Combine(Config.Computerinstallpath, "Custom Run", timestamp, folderName);

            using (var file = new StreamWriter(copyPath))
            {
                file.WriteLine("@echo off");
                var line = @"xcopy """ + folderPath + @""" """ + dstDir + @""" /i /s /e /V /Y /Q";
                file.WriteLine(line);
            }
            _service.StartNewCmdThread(copyPath);

            // From Network drive, to clients
            foreach (var client in clients)
            {
                var batFileName = Path.Combine(TempPath, "CopyFolder" + client.ComputerName + ".bat");
                using (var file = new StreamWriter(batFileName))
                {
                    file.WriteLine("@echo off");
                    var line = @"cmdkey.exe /add:" + client.ComputerName + @" /user:" +
                               _service.Credentials.DomainSlashUser + @" /pass:" + _service.Credentials.Password;
                    file.WriteLine(line);

                    // Embed xcopy command to transfer ON labclient FROM shared drive TO labclient
                    var copyCmd = @"xcopy """ + dstDir + @""" " + @"""" + localPath + @"""" + @" /i /s /e /V /Y /Q ";
                    // Build run command to embed in bat also
                    var runCmd = "";
                    // Manage parameter
                    if (parameter != "")
                        runCmd = @"""" + localPath +
                                 filePath + @""" """ + parameter + @"""";
                    else
                        runCmd = @"""" + localPath +
                                 filePath + @"""";

                    // Deploy and run batfile FROM Server TO labclient using PSTools
                    line = Config.PsTools + @"\PsExec.exe -d -i 1 \\" + client.ComputerName + @" -u " +
                           _service.Credentials.DomainSlashUser + @" -p " + _service.Credentials.Password + @" cmd /c (" +
                           copyCmd + @" ^& " + runCmd + @")";
                    file.WriteLine(line);
                }
                _service.StartNewCmdThread(batFileName);
            }
        }

        /// <summary>
        ///     Transfers a folder first to the shared drive then to each selected labclient.
        ///     Uses PSExec delegated batch files, running on each client.
        ///     Then runs selected file using same PSExec batch.
        /// </summary>
        /// <returns>Nothing</returns>
        public void CopyFolder(List<LabClient> clients, string folderPath, string timestamp)
        {
            //Get folder name without path
            var folderName = "";
            var words = folderPath.Split('\\');
            foreach (var word in words)
            {
                folderName = word;
            }

            // Paths
            var copyPath = Path.Combine(TempPath, "localCopy.bat");
            var dstDir = Path.Combine(Config.Sharednetworkdrive, "temp", "Custom Run", timestamp, folderName);
            var localPath = Path.Combine(Config.Computerinstallpath, "Custom Run", timestamp, folderName);
            using (var file = new StreamWriter(copyPath))
            {
                file.WriteLine("@echo off");

                var line = @"xcopy """ + folderPath + @""" """ + dstDir + @""" /i /s /e /V /Y /Q";
                file.WriteLine(line);
            }
            _service.StartNewCmdThread(copyPath);

            // From Network drive, to clients
            foreach (var client in clients)
            {
                var batFileName = Path.Combine(TempPath, "CopyFolder" + client.ComputerName + ".bat");
                using (var file = new StreamWriter(batFileName))
                {
                    file.WriteLine("@echo off");
                    var line = @"cmdkey.exe /add:" + client.ComputerName + @" /user:" +
                               _service.Credentials.DomainSlashUser + @" /pass:" + _service.Credentials.Password;
                    file.WriteLine(line);

                    // Embed xcopy command to transfer ON labclient FROM shared drive TO labclient
                    var copyCmd = @"xcopy """ + dstDir + @"""" + @" """ + localPath + @"""" + @" /i /s /e /V /Y /Q ";

                    // Deploy and run batfile FROM Server TO labclient using PSTools
                    line = Config.PsTools + @"\PsExec.exe -d -i 1 \\" + client.ComputerName + @" -u " +
                           _service.Credentials.DomainSlashUser + @" -p " + _service.Credentials.Password + @" cmd /c (" +
                           copyCmd + @")";
                    file.WriteLine(line);
                }
                _service.StartNewCmdThread(batFileName);
            }
        }

        //Deletes the whole NetworkDrive - most secure (If you want to change the path, just add it to snDrive)
        public void DeleteNetworkTempFiles()
        {
            var batFileName = Path.Combine(TempPath, "DeleteNetworkTemp" + ".bat");
            if (File.Exists(Config.Sharednetworkdrive))
            {
                var snDrive = Path.Combine(Config.Sharednetworkdrive);
                using (var file = new StreamWriter(batFileName))
                {
                    file.WriteLine("@echo off");
                    var deleteCmd = @"rmdir " + @"""" + snDrive + @"""" + @" /S /Q";
                    file.WriteLine(deleteCmd);
                }
                _service.StartNewCmdThread(batFileName);
            }
        }
    }
}