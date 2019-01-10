using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace GetDotNetSDKConsole
{
    class Program
    {
        //populated by dotnet.exe --version
        static string currentSDKVersion = "";
        static string hostURL = "https://www.microsoft.com/net/download/visual-studio-sdks";

        /// <summary>
        /// event handler to receive process output
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        static void versionReceiver(object sender, DataReceivedEventArgs args)
        {
            if (!string.IsNullOrEmpty(args.Data)) currentSDKVersion = args.Data;
        }

        static void Main(string[] args)
        {
            //get the currently installed sdk version
            Process process = new Process();
            process.StartInfo.FileName = "dotnet.exe";
            process.StartInfo.Arguments = "--version";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardInput = true;
            process.StartInfo.RedirectStandardOutput = true;
            process.OutputDataReceived += new DataReceivedEventHandler(versionReceiver);
            process.Start();
            process.BeginOutputReadLine();
            process.WaitForExit();
            process.CancelOutputRead();

            Dictionary<int, string> menu = new Dictionary<int, string>();

            var doc = new HtmlWeb().Load(hostURL);
            string pattern = @"(?<=dotnet-sdk-)(.*?)(?=-windows-x64-installer)";
            Console.WriteLine("Choose version to download and install:");
            int cnt = 0;
            foreach (HtmlNode link in doc.DocumentNode.SelectNodes("//a[@href]"))
            {
                string href = link.GetAttributeValue("href", string.Empty);
                if (href.Contains("dotnet-sdk-") && href.Contains("windows-x64-installer"))
                {
                    //                    Console.WriteLine(href);
                    Regex rx = new Regex(pattern, RegexOptions.IgnoreCase);
                    Match m = rx.Match(href);
                    if (m.Success)
                    {
                        menu.Add(cnt++, href);
                        string Current = "";
                        if (m.Value == currentSDKVersion) Current = "(Installed)";
                        Console.WriteLine($"[{cnt}] {m.Value} {Current}");
                    }
                }
            }
            Console.WriteLine("Choice ? ([1]/2/#/...)");
            var choice = Console.ReadLine();
            if (string.IsNullOrEmpty(choice)) choice = "1";

            Regex rx2 = new Regex(pattern, RegexOptions.IgnoreCase);
            Match m2 = rx2.Match(menu[int.Parse(choice) - 1]);
            string veronly = "";
            if (m2.Success)
            {
                veronly = m2.Value;
                //Console.WriteLine(veronly);
                var dotnetcoresdk_url = $"https://dotnet.microsoft.com{menu[int.Parse(choice) - 1]}";
                var dotnetcoresdk_exe = $"./dotnet-sdk-{veronly}-sdk-win-x64.exe";
                Console.WriteLine($"Downloading ({dotnetcoresdk_url}) ...");

                var pg = new HtmlWeb().Load(dotnetcoresdk_url);
                foreach (HtmlNode link in pg.DocumentNode.SelectNodes("//a[@href]"))
                {
                    if (link.OuterHtml.Contains("Try again"))
                    {
                        var reallink = link.GetAttributeValue("href", string.Empty);
                        int read = DownloadFile(reallink, dotnetcoresdk_exe);

                        Process proc = new Process();
                        proc.StartInfo.WorkingDirectory = ".";
                        proc.StartInfo.FileName = dotnetcoresdk_exe;
                        proc.StartInfo.UseShellExecute = false;
                        proc.StartInfo.RedirectStandardError = true;
                        proc.Start();
                        break;
                    }
                }
            }


        }

        public static int DownloadFile(String remoteFilename,
                               String localFilename)
        {
            // Function will return the number of bytes processed
            // to the caller. Initialize to 0 here.
            int bytesProcessed = 0;

            // Assign values to these objects here so that they can
            // be referenced in the finally block
            Stream remoteStream = null;
            Stream localStream = null;
            WebResponse response = null;

            // Use a try/catch/finally block as both the WebRequest and Stream
            // classes throw exceptions upon error
            try
            {
                // Create a request for the specified remote file name
                WebRequest request = WebRequest.Create(remoteFilename);
                if (request != null)
                {
                    // Send the request to the server and retrieve the
                    // WebResponse object 
                    response = request.GetResponse();
                    if (response != null)
                    {
                        // Once the WebResponse object has been retrieved,
                        // get the stream object associated with the response's data
                        remoteStream = response.GetResponseStream();

                        // Create the local file
                        localStream = File.Create(localFilename);

                        // Allocate a 1k buffer
                        byte[] buffer = new byte[1024];
                        int bytesRead;

                        // Simple do/while loop to read from stream until
                        // no bytes are returned
                        do
                        {
                            // Read data (up to 1k) from the stream
                            bytesRead = remoteStream.Read(buffer, 0, buffer.Length);

                            // Write the data to the local file
                            localStream.Write(buffer, 0, bytesRead);

                            // Increment total bytes processed
                            bytesProcessed += bytesRead;
                        } while (bytesRead > 0);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            finally
            {
                // Close the response and streams objects here 
                // to make sure they're closed even if an exception
                // is thrown at some point
                if (response != null) response.Close();
                if (remoteStream != null) remoteStream.Close();
                if (localStream != null) localStream.Close();
            }

            // Return total bytes processed to caller.
            return bytesProcessed;
        }
    }
}
