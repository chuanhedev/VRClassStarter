using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using Newtonsoft.Json.Linq;
using ICSharpCode.SharpZipLib.Zip;
using ICSharpCode.SharpZipLib.Core;
using System.Threading;

namespace VRClassUpdater
{
    public partial class Form1 : Form
    {
        string downloadRoot = "http://localhost/vrclass/resources/";
        //string downloadRoot = "http://www.iyoovr.com/vrclass/";
        string tempFolderPath = "temp/";
        string serverFolderPath = "server/";
        string serverResourcesPath = "resources/";
        WebClient webClient;
        // List<string> files = new List<string>();
        JArray fileInfos;
        int fileIndex = 0;
        bool updateClient = false;
        bool updateServer = false;
        string teacherFileName = "teacher.zip";
        string serverFileName = "server.zip";
        string localConfigFileName = "config.json";
        string versionNumber;

        public Form1()
        {
            InitializeComponent();
        }

        private async void Form1_Load(object sender, EventArgs e)
        {
            Debug.WriteLine("hello Form1_Load");
            JObject localVersion = await GetLocalVersion();
            Debug.WriteLine(localVersion);
            JObject serverVersion;
            try
            {
                serverVersion = await GetServerVersion();
            }
            catch(Exception ex)
            {
                Debug.WriteLine("catch " + ex.ToString());
                OpenTeacherClient();
                return;
            }
            //JObject sv = JObject.Parse(serverVersion);
            Debug.WriteLine("localVersion " + localVersion["version"]);
            Debug.WriteLine("serverVersion " + serverVersion["version"]);
            fileInfos = (JArray)serverVersion["files"];
            Debug.WriteLine(fileInfos.Count);
            if (!Directory.Exists(tempFolderPath))
            {
                Directory.CreateDirectory(tempFolderPath);
            }

            updateClient = serverVersion["update_teacher"].ToString() == "1";
            if (updateClient)
            {
                JObject t = new JObject();
                t["name"] = teacherFileName;
                t["path"] = "";
                fileInfos.Add(t);
            }
            //KillNodeProcess();
            updateServer = serverVersion["update_server"].ToString() == "1";
            if (updateServer)
            {
                KillNodeProcess();
                JObject t = new JObject();
                t["name"] = serverFileName;
                t["path"] = "";
                fileInfos.Add(t);
            }

            if (fileIndex < fileInfos.Count)
            {
               label1.Text = string.Format("下载中({0}/{1})", fileIndex + 1, fileInfos.Count);
               JToken fileInfo = fileInfos[fileIndex];
               webClient = new WebClient();
               webClient.DownloadFileCompleted += WebClient_DownloadFileCompleted;
               webClient.DownloadProgressChanged += Wb_DownloadProgressChanged;
                webClient.DownloadFileAsync(new Uri(downloadRoot + fileInfo["path"] + fileInfo["name"]), tempFolderPath +  fileInfo["path"] + fileInfo["name"]);
                Debug.WriteLine("downloading ... " + downloadRoot + fileInfo["path"] + fileInfo["name"] + " " + tempFolderPath +  fileInfo["path"] + fileInfo["name"] );
            }

        }

        public void KillNodeProcess()
        {
            Debug.WriteLine("KillNodeProcess");
            foreach (var process in Process.GetProcessesByName("node"))
            {
                //Debug.WriteLine(process.ProcessName);
                process.Kill();
            }
        }


        public static bool UnpackFiles(string file, string dir)
        {
            try
            {
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                ZipInputStream s = new ZipInputStream(File.OpenRead(file));
                ZipEntry theEntry;
                while ((theEntry = s.GetNextEntry()) != null)
                {
                    string directoryName = Path.GetDirectoryName(theEntry.Name);
                    string fileName = Path.GetFileName(theEntry.Name);
                    if (directoryName != String.Empty)
                    {
                        Directory.CreateDirectory(dir + directoryName);
                    }
                    if (fileName != String.Empty)
                    {
                        FileStream streamWriter = File.Create(dir + theEntry.Name);
                        int size = 2048;
                        byte[] data = new byte[2048];
                        while (true)
                        {
                            size = s.Read(data, 0, data.Length);
                            if (size > 0)
                            {
                                streamWriter.Write(data, 0, size);
                            }
                            else
                            {
                                break;
                            }
                        }
                        streamWriter.Close();
                    }
                }
                s.Close();
                return true;
            }
            catch (Exception)
            {
                throw;
            }
        }

        private void UpdateServerFolder()
        {
            Debug.WriteLine("UpdateServerFolder");
            if (updateServer)
            {
                DirectoryInfo di = new DirectoryInfo(Path.Combine(Directory.GetCurrentDirectory(), "server"));
                foreach (FileInfo file in di.GetFiles())
                {
                    file.Delete();
                }
                foreach (DirectoryInfo dir in di.GetDirectories())
                {
                    if (dir.Name != "resources") 
                        dir.Delete(true);
                }
                System.IO.Compression.ZipFile.ExtractToDirectory(Path.Combine(Directory.GetCurrentDirectory(), tempFolderPath, serverFileName), Path.Combine(Directory.GetCurrentDirectory(), "server"));
             
            }
        }

        private void UpdateTeacherFolder()
        {
            Debug.WriteLine("UpdateTeacherFolder");
            if (updateClient)
            {
                DirectoryInfo di = new DirectoryInfo(Path.Combine(Directory.GetCurrentDirectory(), "client"));
                foreach (FileInfo file in di.GetFiles())
                {
                    file.Delete();
                }
                foreach (DirectoryInfo dir in di.GetDirectories())
                {
                    if (dir.Name != "") ;
                    dir.Delete(true);
                }
                System.IO.Compression.ZipFile.ExtractToDirectory(Path.Combine(Directory.GetCurrentDirectory(), tempFolderPath, teacherFileName), Path.Combine(Directory.GetCurrentDirectory(), "client"));
                //UnpackFiles(Path.Combine(Directory.GetCurrentDirectory(), tempFolderPath, teacherFileName), Path.Combine(Directory.GetCurrentDirectory(), "client"));
                Debug.WriteLine("done");
            }
        }

        private void DownloadComplete()
        {
            Debug.WriteLine("DownloadComplete");
            UpdateServerFolder();
            UpdateTeacherFolder();
            OpenTeacherClient();
        }

        //private void ZipFileProgress(object sender, ProgressEventArgs args)
        //{
        //    Debug.WriteLine("ZipFileProgress " + args.PercentComplete);
        //}

        //private async void ZipFileComplete(object sender, ScanEventArgs args)
        //{
        //    Debug.WriteLine("ZipFileComplete");
        //    await Task.Delay(3000);
        //    OpenTeacherClient();
        //}

        private void OpenTeacherClient()
        {
            Debug.WriteLine("OpenTeacherClient");
            try
            {
                Process process = new Process();
                ProcessStartInfo startinfo = new ProcessStartInfo();
                startinfo.WindowStyle = ProcessWindowStyle.Hidden;
                startinfo.FileName = "cmd.exe";
                startinfo.Arguments = "/c forever start server/index.js";
                process.StartInfo = startinfo;
                process.Start();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }

            Process.Start(Path.Combine(Directory.GetCurrentDirectory(), "client", "run.exe"));
            Application.Exit();
        }

        private void WebClient_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            JToken fileInfo2= fileInfos[fileIndex];
            Debug.WriteLine("Wb_DownloadDataCompleted" + " " +fileInfo2["path"]+ fileInfo2["name"]);
            fileIndex++;
            // Debug.WriteLine("Wb_DownloadDataCompleted" + " " + fileIndex + " " + files.Count);
            if (fileIndex < fileInfos.Count)
            {
                JToken fileInfo= fileInfos[fileIndex];
                string fileFullName = fileInfo["path"].ToString() + fileInfo["name"];
                label1.Text = string.Format("下载中({0}/{1})", fileIndex + 1, fileInfos.Count);

                
                if (!Directory.Exists(tempFolderPath + fileInfo["path"]))
                {
                    Directory.CreateDirectory(tempFolderPath + fileInfo["path"]);
                }

                webClient.DownloadFileAsync(new Uri(downloadRoot + fileFullName), tempFolderPath + fileFullName);
                Debug.WriteLine("downloading2 ... " + downloadRoot + fileInfo["path"] + fileInfo["name"] + " " + tempFolderPath +  fileInfo["path"] + fileInfo["name"] );
            }
            else
            {
                DownloadComplete();
            }
        }

        private void Wb_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            //Debug.WriteLine(e.BytesReceived + " " + e.ProgressPercentage);
            progressBar1.Value = e.ProgressPercentage;
        }

        private JObject SaveVersion()
        {
            JObject version = new JObject();
            version["version"] = versionNumber;
            using (StreamWriter sw = new StreamWriter(localConfigFileName))
            {
                sw.Write(version.ToString());
            }
            return version;
        }

        private async Task<JObject> GetLocalVersion()
        {
            String line;
            try
            {
                using (StreamReader sr = new StreamReader(localConfigFileName))
                {
                    line = await sr.ReadToEndAsync();
                }
                return JObject.Parse(line);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("GetLocalVersion err");
                Debug.WriteLine(ex.ToString());
                versionNumber = "1.0.0";
                return SaveVersion();
            }
        }

        private async Task<JObject> GetServerVersion()
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create("http://localhost/vrclass/version.php");
            request.Method = "Get";

            var response = await request.GetResponseAsync();
            var responseStream = response.GetResponseStream();
            string myResponse = "";
            using (StreamReader sr = new StreamReader(responseStream))
            {
                myResponse = await sr.ReadToEndAsync();
            }
            Debug.WriteLine("result " + myResponse);
            return JObject.Parse(myResponse);
        }
    }
}
