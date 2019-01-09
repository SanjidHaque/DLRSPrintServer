using RestSharp;
using RestSharp.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Configuration;
using System.Diagnostics;
using System.IO;

namespace DLRSPrintServer
{
    public partial class PrintStation : Form
    {
        bool WorkingState = false;
        bool PrintWorkingState = false;
        PrintJob pjob = new PrintJob();
        string filepath = "";
        string path = ConfigurationSettings.AppSettings["savedfilepath"];
        string appURL = ConfigurationSettings.AppSettings["applicationURL"];
        string user = ConfigurationSettings.AppSettings["userName"];
        string password = ConfigurationSettings.AppSettings["password"];

        string listURL = "print/list";
        string updateURL = "print/update";
        public PrintStation()

        {
            InitializeComponent();
           
            initiateTimer();
        }

        private void initiateTimer()
        {
            //timer.Interval = 10000;

            timer.Start();
        }

        private void timer_Tick(object sender, EventArgs e)
        {
            if(!WorkingState)
            {
                //start worker
                worker.RunWorkerAsync();
                while (this.worker.IsBusy)
                {
                    
                    // Keep UI messages moving, so the form remains 
                    // responsive during the asynchronous operation.
                    Application.DoEvents();
                }
            }

        }

        private void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            WorkingState = true;
            //Download file
            //System.Threading.Thread.Sleep(4000);
           downloadfile();
           // printfile(job.Path);

            Console.WriteLine("main job start");

        }
        private void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            WorkingState = false;
            //log printing
           // filepath = @"C:\Users\Jossy\Desktop\Invoice November17_5332.pdf";
          // deleteFile(job);
            Console.WriteLine("main job done");

        }

    

        private void printfile(string filepath)
        {
           // filepath = @"C:\Users\Jossy\Documents\Invoice November17_5332.pdf";
            PrintPDF(filepath, "");
        }
        public static void PrintPDF(string path, string printer)
        {
            Process process = new Process();
            process.StartInfo.FileName = path;
            process.StartInfo.Verb = "printto";
            process.StartInfo.Arguments = "\"" ;

            process.Start();

            //System.Threading.Thread.Sleep(1000);

            // I have to use this in case of Adobe Reader to close the window

            process.WaitForInputIdle();
            //process.Kill();
        }

        private void downloadfile()
        {
            //return "";
            string filename = "";
            RestClient client = new RestClient(appURL );
            client.UserAgent = "PrintServer";
            var request = new RestRequest(appURL+listURL, Method.POST);
            request.AddParameter("username", user);
            request.AddParameter("password", password);
            //request.AddHeader("x-token", "1234567890");
            IRestResponse< List<PrintJob>> printJobs = client.Execute<List<PrintJob>>(request);
            if(printJobs.IsSuccessful)
            {
                foreach (PrintJob item in printJobs.Data)
                {
                    printWorker.RunWorkerAsync(item);
                    //if (!PrintWorkingState)
                    //{
                    //    //start worker
                    //    printWorker.RunWorkerAsync(item);
                    //    while (this.printWorker.IsBusy)
                    //    {

                    //        // Keep UI messages moving, so the form remains 
                    //        // responsive during the asynchronous operation.
                    //        Application.DoEvents();
                    //    }
                    //}

                }
               // getFile(out filename, out client, out request, printJob);

            }




           // return printJob.Data;
            //request.AddParameter("name", "value"); // adds to POST or URL querystring based on Method
            //request.AddUrlSegment("id", "123"); // replaces matching token in request.Resource

            //return path + filename;
        }

       

        private void deleteFile(PrintJob job)
        {
            if(File.Exists(job.url))
            {
                File.Delete(job.url);

                RestClient client = new RestClient(appURL);
                client.UserAgent = "PrintServer";
                var request = new RestRequest(appURL + updateURL, Method.POST);
                request.AddParameter("username", user);
                request.AddParameter("password", password);
                request.AddParameter("docid", job.document_id);
                request.AddParameter("jobid", job.id);
                //request.AddHeader("x-token", "1234567890");
                var printJob = client.Execute(request);


            }
        }

        private void printWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            PrintWorkingState = true;
            string filename = "";
            PrintJob job = (PrintJob)e.Argument;
            RestClient client = new RestClient(appURL + job.url);
            client.UserAgent = "PrintServer";
           // var request = new RestRequest(appURL + listURL, Method.POST);
            filename = Guid.NewGuid().ToString() + ".pdf";
            
            var request = new RestRequest( Method.GET);
            byte[] file = client.DownloadData(request);

            if (file.Count() > 0)
            {
                filename = path + filename;
                file.SaveAs(filename);
                printfile(filename);
                job.url = filename;
                e.Result = job;


            }
            Console.WriteLine("Download "+job.id);


        }

        private void printWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            PrintWorkingState = false;
            if (e.Error == null && e.Cancelled == false)
            {
                PrintJob job = (PrintJob)e.Result;
                deleteFile(job);
                Console.WriteLine("print "+job.id);
            }


        }
    }
    public class PrintJob
    {
        public string document_id { get; set; }
        public string user_id { get; set; }
        public string url { get; set; }
        public DateTime created_at { get; set; }
        public int status { get; set; }
        public string id { get; set; }
       

    }
}
