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
        string teacherFileName = "teacher.zip";
        string serverFileName = "server.zip";
        string localConfigFileName = "config.json";

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
            fileInfos = (JArray)serverVersion["files"];
            Debug.WriteLine(fileInfos.Count);
            if (!Directory.Exists(tempFolderPath))
            {
                Directory.CreateDirectory(tempFolderPath);
            }

            // updateClient = serverVersion["update"].ToString() == "1";
            // if (updateClient)
            // {
            //     files.Add(teacherFileName);
            // }
               
            // for(int i=0; i<fileInfos.Count; i++)
            // {
            //     files.Add(fileInfos[i]["name"].ToString());
            // }
            
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

        private void DownloadComplete()
        {
            Debug.WriteLine("DownloadComplete");
            // if (updateClient)
            // {
            //     DirectoryInfo di = new DirectoryInfo(Path.Combine(Directory.GetCurrentDirectory(), "client"));
            //     foreach(FileInfo file in di.GetFiles())
            //     {
            //         file.Delete();
            //     }
            //     foreach (DirectoryInfo dir in di.GetDirectories())
            //     {
            //         dir.Delete(true);
            //     }
            //     Debug.WriteLine("DownloadComplete");
            //     FastZipEvents fastZipEvents = new FastZipEvents();
            //     fastZipEvents.CompletedFile = ZipFileComplete;
            //     fastZipEvents.Progress = ZipFileProgress;
            //     FastZip fastZip = new FastZip(fastZipEvents);
            //     fastZip.ExtractZip(Path.Combine(Directory.GetCurrentDirectory(), tempFolderPath, teacherFileName), Path.Combine(Directory.GetCurrentDirectory(), "client"), null);
            // }
            // else
            //     OpenTeacherClient();
        }

        private void ZipFileProgress(object sender, ProgressEventArgs args)
        {
            Debug.WriteLine("ZipFileProgress " + args.PercentComplete);
        }

        private async void ZipFileComplete(object sender, ScanEventArgs args)
        {
            Debug.WriteLine("ZipFileComplete");
            await Task.Delay(3000);
            OpenTeacherClient();
        }

        private void OpenTeacherClient()
        {
            Debug.WriteLine("OpenTeacherClient");
            //try
            //{
            //    process process = new process();
            //    processstartinfo startinfo = new processstartinfo();
            //    startinfo.windowstyle = processwindowstyle.hidden;
            //    startinfo.filename = "cmd.exe";
            //    startinfo.arguments = "/c forever start server/index.js";
            //    process.startinfo = startinfo;
            //    process.start();
            //}catch(exception ex){
            //    debug.writeline(ex.tostring());
            //}

            Process.Start(Path.Combine(Directory.GetCurrentDirectory(),"client","run.exe"));
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
                webClient.DownloadFileAsync(new Uri(downloadRoot + fileInfos), tempFolderPath + fileInfos);
                Debug.WriteLine("downloading ... " + downloadRoot + fileInfo["path"] + fileInfo["name"] + " " + tempFolderPath +  fileInfo["path"] + fileInfo["name"] );
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

        private async Task<JObject> GetLocalVersion()
        {
            String line;
            try
            {
                using (StreamReader sr = new StreamReader(localConfigFileName))
                {
                    line = await sr.ReadToEndAsync();
                }
                return new JObject(line);
            }
            catch (Exception ex)
            {
                JObject version = new JObject();
                version["version"] = "1.0.0";
                using (StreamWriter sw = new StreamWriter(localConfigFileName))
                {
                    sw.Write(version.ToString());
                }
                return version;
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
