
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Threading;

namespace ServiceLibrary
{
    public class Dms
    {
        private readonly string customRunDmsModality = "Other";
        private readonly string dmsUrl = "https://cobelab.au.dk";
        private readonly Service service = Service.GetInstance();
        private readonly MyWebClient webClient;
        private User user = null;


        public Dms()
        {
            webClient = new MyWebClient();
        }

        //returns User if logged in successful, else returns null
        public User Login(string username, string password)
        {
            var userHash =
                webClient.DownloadString(dmsUrl + "/modules/StormDb/extract/login?username=" + username + "&password=" +
                                         password);

            if (userHash.Contains("templogin"))
            {
                user = new User(username, password);
                user.UniqueHash = userHash;
            }

            return user;
        }

        public List<string> GetProjects()
        {
            var projectsStr =
                webClient.DownloadString(dmsUrl + "/modules/StormDb/extract/projects?" + service.User.UniqueHash);
            var lines = projectsStr.Split(new[] {"\r\n", "\n"}, StringSplitOptions.None);
            return new List<string>(lines);
        }

        //get all subjects from project
        private List<string> GetAllSubjects(TestApp testApp)
        {
            var subjStr =
                webClient.DownloadString(dmsUrl + "/modules/StormDb/extract/subjectswithcode?" + service.User.UniqueHash +
                                         "&projectCode=" + testApp.ProjectName);
            var lines = subjStr.Split(new[] {"\r\n", "\n"}, StringSplitOptions.None);
            return new List<string>(lines);
        }

        public void DmsTransfer(string projPath, TestApp testApp)
        {
            var subjects = new DirectoryInfo(projPath).GetDirectories();
            var zipsForUpload = new List<string>();

            //iterate through all subjects and their results
            foreach (var subject in subjects)
            {
                foreach (var timeline in subject.GetDirectories())
                {
                    foreach (var modality in timeline.GetDirectories())
                    {
                        if (modality.Name != testApp.ApplicationName)
                        {
                            continue;
                        }

                        var subjId = CreateSubject(subject.Name, testApp);
                        var appName = testApp.ApplicationName;

                        if (testApp is CustomRunTestApp)
                        {
                            appName = customRunDmsModality;
                        }
                        var dirToZip = Path.Combine(projPath, subject.Name, timeline.Name, testApp.ApplicationName);
                        var zipFileName = testApp.ProjectName + "." + subjId + "." + timeline.Name + "." + appName +
                                          ".zip";
                        var zipPath = Path.Combine(projPath, zipFileName);

                        ZipDirectory(dirToZip, zipPath);
                        zipsForUpload.Add(zipPath);
                    }
                }
            }
            bool result = false;
            UploadZips(zipsForUpload);

            foreach (var subject in subjects)
            {
                foreach (var timeline in subject.GetDirectories())
                {
                    foreach (var modality in timeline.GetDirectories())
                    {
                        if (modality.Name != testApp.ApplicationName)
                        {
                            continue;
                        }
                        if(ValidateEachResultFolder(testApp.ProjectName, timeline.Name, testApp.ApplicationName, projPath))
                        {
                            result = true;  
                        }
                        else
                        {
                            result = false;
                        }
                    }
                }
            }

            if (result == true)
            {
                service.NotifyStatus("All your files have been successful validated.");
            }
            else
            {
                service.NotifyStatus("Could't validate all the files. Check the reconciled files for errors " + Path.GetTempPath());
            }

        }

        public bool ValidateEachResultFolder(string projectName, string name, string applicationName, string projPath)
        {
            bool succes = true;
            //Creatres connection to the shared networkdrive
            NetworkCredential networkcred = new NetworkCredential(service.User.Username, service.User.Password, "ASB");
            NetworkConnection nc = new NetworkConnection(service.Config.DmsUpload, networkcred);

            var path = Path.Combine(@"\\cobelab.au.dk\projects", projectName, "raw", "0001", name, applicationName, "001.Not_Available", "files");

            //waits for the file to get stored correct into StormDb
            DateTime dt = DateTime.Now + TimeSpan.FromMinutes(10);
            while (!Directory.Exists(path) || DateTime.Now > dt)
            {
            }

            var copyPath = Path.Combine(Path.GetTempPath(), "localCopy" + name + ".bat");
            var logPath = Path.Combine(Path.GetTempPath(), "reconcile" + name + ".txt");

            using (var file = new StreamWriter(copyPath))
            {
                file.WriteLine("@echo off");
                var line = @"ROBOCOPY " + "\"" + Path.Combine(projPath, "ZTreeSubject", name, applicationName) + "\"" + " " + "\"" + path + "\"" + " /e /l /ns /njs /njh /ndl /fp /log:" + logPath;
                file.WriteLine(line);

            }
            service.StartNewCmdThread(copyPath);
            // wait for robycopy to generate the txt file
            Thread.Sleep(2000);

            try
            {
                using (var file = new StreamReader(logPath))
                {
                    string line;
                    while (((line = file.ReadLine()) != null && line != ""))
                    {
                        if (line.Contains("Newer"))
                        {
                            continue;
                        }
                        else
                        {
                            succes = false;
                        }
                    }
                }
            }
            catch (System.IO.FileNotFoundException)
            {
                succes = false;
            }
            return succes;
        }

        public void BackupDMSTransfer(string projpath, TestApp testApp)
        {
            string cs = @"Data Source=YLGW036487\SQLEXPRESS;Initial Catalog=LabRun;Persist Security Info=True;User ID=sa;Password=Test1234";

         
            using (SqlConnection con = new SqlConnection(cs))       
            {
                con.Open();
                foreach (var file in Directory.GetFiles(@projpath, ".", SearchOption.AllDirectories))
                {
                    byte[] data = File.ReadAllBytes(file);
                    string sql = "INSERT INTO Files VALUES (@Files, default)";
                    SqlCommand cmd = new SqlCommand(sql, con);
                    cmd.Parameters.Add("@Files", data);
                    cmd.ExecuteNonQuery();
                }
                con.Close();
            }
        }

        //creates subject and returns subject number
        private string CreateSubject(string boothNo, TestApp testApp)
        {
            var subjId = "";
            var subjectName = "Booth" + boothNo;

            if (testApp is ZTree)
            {
                var exists = false;
                subjectName = "ztree_subject";
                var subjects = GetAllSubjects(testApp);
                foreach (var subject in subjects)
                {
                    //subject number structure: "0001_ABC"
                    if (subject.Length != 8)
                        continue;
                    var subjNoStr = subject.Remove(subject.Length - 4).TrimStart('0');

                    //ztree_subject reserved number is 1 (can be any number though)
                    if (subjNoStr == "1")
                    {
                        subjId = subject;
                        exists = true;
                        break;
                    }
                }

                if (exists)
                {
                    return subjId;
                }
            }

            //Creates subject in DMS. If successful returns new subject's id,
            //else returns a string containing "error" keyword
            var url = dmsUrl + "/modules/StormDb/extract/createsubject?subjectName=" + subjectName + "&" +
                      service.User.UniqueHash + "&projectCode=" + testApp.ProjectName;
            var result = webClient.DownloadString(url);

            //cut "\n" - new line seperators from result
            result = Regex.Replace(result, @"\n", string.Empty);
            subjId = result;
            if (subjId.Contains("error"))
                throw new Exception("Failed to create new subject for BoothNo " + boothNo);

            return subjId;
        }

        private void ZipDirectory(string dirToZip, string zipPath)
        {
            if (File.Exists(zipPath))
            {
                File.Delete(zipPath);
            }
            ZipFile.CreateFromDirectory(@dirToZip, @zipPath);
        }

        private void searchForProjectFolder(string path)
        {
            var DMSLocalPath = @service.Config.DMSLocalPath;
            var copyPath = Path.GetTempPath() + "dmsValidate.bat";

            using (var file = new StreamWriter(copyPath))
            {
                file.WriteLine("@echo off");
                file.WriteLine(@"net use """ + DMSLocalPath + @""" " + service.User.Password + @" /user:" +
                          service.User.Username);


            }
        }

        private void UploadZips(List<string> zips)
        {
            var uploadPath = @service.Config.DmsUpload;
            var copyPath = Path.GetTempPath() + "zipsUpload.bat";
            using (var file = new StreamWriter(copyPath))
            {
                file.WriteLine("@echo off");
                file.WriteLine(@"net use """ + uploadPath + @""" " + service.User.Password + @" /user:" +
                               service.User.Username);

                file.WriteLine(":copy");
                foreach (var zip in zips)
                {
                    var line = @"xcopy """ + zip + @""" """ + uploadPath + @""" /V /Y /Q /I";
                    file.WriteLine(line);
                }

                file.WriteLine("IF ERRORLEVEL 0 goto disconnect");
                file.WriteLine("goto disconnect");

                file.WriteLine(":disconnect");
                file.WriteLine(@"net use """ + uploadPath + @""" /delete");
                file.WriteLine("goto end");
                file.WriteLine(":end");
                file.WriteLine("exit");
            }
            service.StartNewCmdThread(copyPath);
        }


        private class MyWebClient : WebClient
        {
            protected override WebRequest GetWebRequest(Uri uri)
            {
                var w = base.GetWebRequest(uri);
                w.Timeout = 10*1000;
                return w;
            }
        }
    }
}