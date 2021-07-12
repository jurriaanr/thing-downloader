using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace ThingDownloader
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private int FilesDownloadedCount = 0;
        private int FilesDownloadCount = 0;
        
        public MainWindow()
        {
            InitializeComponent();
        }

        private async void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            var lines = new StringCollection();
            var lineCount = Input.LineCount;
            for (var i = 0; i < lineCount; i++)
            {
                var txt = Input.GetLineText(i);
                if (!string.IsNullOrEmpty(txt) && Uri.IsWellFormedUriString(txt, UriKind.Absolute) && !lines.Contains(txt))
                {
                    lines.Add(txt);    
                }
            }

            Dispatcher.Invoke(new Action(() => 
            {
                Button.IsEnabled = false;
                MyConsole.Text = $"Downloading {lineCount} files.";
            }));
            
            await DownloadFiles(lines);
            
            FilesDownloadCount = 0;
            FilesDownloadedCount = 0;
            
            MyConsole.Text = "The downloads have finished, open the downloads folder in this directory";
            Button.IsEnabled = true;
        }

        private async Task DownloadFiles(StringCollection urls)
        {
            FilesDownloadCount = urls.Count;
            
            var path = Environment.CurrentDirectory + Path.DirectorySeparatorChar + "download";
            System.IO.Directory.CreateDirectory(path);
         
            var listOfTasks = new List<Task>();
            foreach (var url in urls)
            {
                Dispatcher.Invoke(new Action(() => 
                {   
                    MyConsole.Text = "Downloading " + url;
                }));
                listOfTasks.Add(DownloadFile(url, path));
                await Task.Delay(100);
            }
            
            await Task.WhenAll(listOfTasks);
        }
        
        private async Task DownloadFile(string url, string path)
        {
            try
            {
                var fileName = GetFileName(url);
                
                using (var wc = new WebClient())
                {
                    wc.DownloadFileCompleted += wc_DownloadFileCompleted;
                    await wc.DownloadFileTaskAsync(new Uri(url), path + Path.DirectorySeparatorChar + fileName);
                }
            }
            catch (Exception)
            {
                MessageBox.Show("Failed to download File: " + url);
            }
        }

        private void wc_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            if (e.Cancelled || e.Error != null)
            {
                MessageBox.Show("An error occurred while trying to download file: " + e.Error ?? "?");
                return;
            }
            
            FilesDownloadedCount += 1;
            Dispatcher.Invoke(new Action(() => 
            {   
                ProgressBar.Value = ((double)FilesDownloadedCount / (double)FilesDownloadCount) * 100;
                MyConsole.Text = $"Downloaded {FilesDownloadedCount} files.";
            }));
        }

        private static string GetFileName(string url)
        {
            var uri = new Uri(url);
            return System.IO.Path.GetFileName(uri.LocalPath);
        }

        private void Input_OnGotFocus(object sender, RoutedEventArgs e)
        {
            if (Input.Text == "Insert list of URL's")
            {
                Input.Text = "";
            }
        }
    }
}