using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MahApps.Metro.Controls;
using Microsoft.Win32;
using System.Configuration;
using System.Threading;
using System.Windows.Threading;
using System.ComponentModel;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Upload;
using Google.Apis.Util.Store;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
namespace Picus
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        static int skippedLines = 0; //dla wpisywania do textblocka
        string ConversionString="-i ";
        static string uploadFilePath="";
        static int fileDuration = 0;
        public MainWindow()
        {
            //wymagane dla frappera
            System.Configuration.ConfigurationManager.AppSettings.Set("ffmpeg:ExeLocation", AppDomain.CurrentDomain.BaseDirectory+"\\lib\\ffmpeg.exe");
            InitializeComponent();
            var list = new List<ComboBoxItem>
                {
                    new ComboBoxItem {DisplayText = "16:10", IsHeader = true},
                    new ComboBoxItem {DisplayText = "1920x1200", IsHeader = false},
                    new ComboBoxItem {DisplayText = "1680x1050", IsHeader = false},
                    new ComboBoxItem {DisplayText = "1440x900", IsHeader = false},
                    new ComboBoxItem {DisplayText = "1280×800", IsHeader = false},
                    new ComboBoxItem {DisplayText = "16:9", IsHeader = true},
                    new ComboBoxItem {DisplayText = "1920x1080", IsHeader = false},
                    new ComboBoxItem {DisplayText = "1600x900", IsHeader = false},
                    new ComboBoxItem {DisplayText = "1366x768", IsHeader = false},
                    new ComboBoxItem {DisplayText = "1366x768", IsHeader = false},
                    new ComboBoxItem {DisplayText = "1280x720", IsHeader = false},
                    new ComboBoxItem {DisplayText = "1024x576", IsHeader = false},
                    new ComboBoxItem {DisplayText = "960x540", IsHeader = false},
                    new ComboBoxItem {DisplayText = "640x360", IsHeader = false},
                    new ComboBoxItem {DisplayText = "4:3", IsHeader = true},
                    new ComboBoxItem {DisplayText = "2048x1536", IsHeader = false},
                    new ComboBoxItem {DisplayText = "1600x1200", IsHeader = false},
                    new ComboBoxItem {DisplayText = "1400x1050", IsHeader = false},
                    new ComboBoxItem {DisplayText = "1280x960", IsHeader = false},
                    new ComboBoxItem {DisplayText = "1024x768", IsHeader = false},
                    new ComboBoxItem {DisplayText = "800x600", IsHeader = false},
                    new ComboBoxItem {DisplayText = "640x480", IsHeader = false},
                };           
            DataContext = list;
           
        }
        public class ComboBoxItem
        {
            public string DisplayText { get; set; }
            public bool IsHeader { get; set; }
        }
        //klikniecie input
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            openFileDialog1.ValidateNames = true;         
            bool? userClickedOK = openFileDialog1.ShowDialog();          
            if (userClickedOK == true)
            {             
                InputTxt.Text = openFileDialog1.FileName;        
                 System.Diagnostics.Process process1;
            process1 = new System.Diagnostics.Process();
            process1.StartInfo.FileName = AppDomain.CurrentDomain.BaseDirectory + "\\lib\\ffprobe.exe";
            process1.EnableRaisingEvents = true;
            process1.StartInfo.UseShellExecute = false;
            process1.StartInfo.CreateNoWindow = true;
            process1.StartInfo.RedirectStandardOutput = true;
            process1.StartInfo.RedirectStandardError = true;
            string cmdline;
            cmdline = "-show_format -show_streams ";
            cmdline += InputTxt.Text;
            process1.StartInfo.Arguments = cmdline;
            process1.Start();
            string s = process1.StandardError.ReadToEnd();
            Information.Text = "Information : \n"+DeleteLines(s,11);
            string tempDuration = s.Substring(s.IndexOf("Duration: ") + ("Duration: ").Length, ("00:00:00.00").Length);
            string[] split0 = tempDuration.Split('.');
            string[] goodDuration = split0[0].Split(':');  //[0] godziny, [1] minuty, [2] sekundy
            fileDuration = Convert.ToInt32(goodDuration[1]) * 60 + Convert.ToInt32(goodDuration[2]);         
            }
        }
      //usuwa linie z poczatku stringa
        private static string DeleteLines(string input, int lines)
        {
            var result = input;
            for (var i = 0; i < lines; i++)
            {
                var idx = result.IndexOf('\n');
                if (idx < 0)
                {                  
                    return string.Empty;
                }
                result = result.Substring(idx + 1);
            }
            return result;
        }
        //start konwersji
        private void Button_Click_1(object sender, RoutedEventArgs e)
        {     
            buildBasicConversionString();            
                System.Diagnostics.Process process1;
                process1 = new System.Diagnostics.Process();
                process1.StartInfo.FileName = AppDomain.CurrentDomain.BaseDirectory + "\\lib\\ffmpeg.exe";
                process1.EnableRaisingEvents = true;
                process1.StartInfo.UseShellExecute = false;
                process1.StartInfo.CreateNoWindow = true;
                process1.StartInfo.RedirectStandardOutput = true;
                process1.StartInfo.RedirectStandardError = true;
                process1.StartInfo.Arguments = ConversionString;                              
                process1.ErrorDataReceived += process1_ErrorDataReceived;
                BackgroundWorker tempWorker = new BackgroundWorker(); //update UI musi sie odbyc na drugim watku, inaczej crash z powodu braku dostepu (watek konwertujacy nie moze zajmowac sie jednoczesnie wrzucaniem danych do textblocka)
                tempWorker.DoWork += delegate
                {
                    process1.Start();
                    process1.BeginOutputReadLine();
                    process1.BeginErrorReadLine();                    
                };
                tempWorker.RunWorkerAsync();           
            ConversionString = "-i "; //reset
        }

        void process1_ErrorDataReceived(object sender, System.Diagnostics.DataReceivedEventArgs e)
        {        
             Dispatcher.BeginInvoke((Action) delegate {
                 //string tempDuration = e.Data.Substring(e.Data.IndexOf("time=") + ("time=").Length, 8);
               //  MessageBox.Show(e.Data);
             //    string[] split0 = tempDuration.Split('.');
                // string[] goodDuration = split0[0].Split(':');  //[0] godziny, [1] minuty, [2] sekundy
               //  int actualDuration = Convert.ToInt32(goodDuration[1]) * 60 + Convert.ToInt32(goodDuration[2]);
                 ProgressTextBlock.Text += "\n" + e.Data;
                 TextScroll.ScrollToEnd();
              //   Progress.Value = fileDuration / actualDuration;
             });
           //  this.ProgressTextBlock.Text += e.Data;
           //  Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(delegate { this.UpdateLayout(); }));
        
         //   ProgressTextBlock.Update();
        }
        //zapis pliku wyjsciowego
        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog1 = new SaveFileDialog();
            saveFileDialog1.ValidateNames = true;          
            bool? userClickedOK = saveFileDialog1.ShowDialog();           
            if (userClickedOK == true)
            {
                OutputTxt.Text = saveFileDialog1.FileName;
               // MessageBox.Show(OutputTxt.Text);
                InputTxt_Upload.Text = saveFileDialog1.FileName;
            }
        }
        public void buildBasicConversionString()
        {
            string quality = "-q:v ";
            switch (QualityComboBox.SelectedIndex)
            {
                case 0:
                    quality = "-vcodec copy -acodec copy ";
                    break;
                case 1:
                    quality += "8 ";
                    break;
                case 2:
                    quality += "15 ";
                    break;
                case 3:
                    quality += "23 ";
                    break;
                case 4:
                    quality += "30 ";
                    break;
                default:
                    quality = "";
                    break;
            }
            string resolution = " -s "+ ResolutionComboBox.Text;
            string[] split = ExtensionComboBox.Text.Split(' ');
            string extension = split[0];
            string temp = InputTxt.Text +" "+quality +resolution + " "+OutputTxt.Text + "." + extension;
            if (CheckBoxShowDebug.IsChecked == true)
            { MessageBox.Show(temp); }
        //    string temp = InputTxt.Text + " -s 720x640 " + OutputTxt.Text + ".mp4";// +extension;
            ConversionString += temp;
        }
        //start uploadu do youtube
        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            uploadFilePath = InputTxt_Upload.Text;
            try
            {
                new MainWindow().UploadVideoYoutube().Wait();
            }
            catch (AggregateException ex)
            {
                foreach (var v in ex.InnerExceptions)
                {
                   MessageBox.Show("Error: " + v.Message);
                }
            }

        }
        private async Task UploadVideoYoutube()
    {
      UserCredential credential;
      using (var stream = new FileStream("client_secrets.json", FileMode.Open, FileAccess.Read))
      {
        credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
            GoogleClientSecrets.Load(stream).Secrets,
            // This OAuth 2.0 access scope allows an application to upload files to the
            // authenticated user's YouTube channel, but doesn't allow other types of access.
            new[] { YouTubeService.Scope.YoutubeUpload },
            "user",
            CancellationToken.None
        );
      }

      var youtubeService = new YouTubeService(new BaseClientService.Initializer()
      {
        HttpClientInitializer = credential,
        ApplicationName = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name
      });

      var video = new Video();
      video.Snippet = new VideoSnippet();
      video.Snippet.Title = VideoTitle.Text;
      video.Snippet.Description = VideoDescription.Text;
      video.Snippet.Tags = new string[] { "tag1", "tag2" };
      video.Snippet.CategoryId = "22"; // https://developers.google.com/youtube/v3/docs/videoCategories/list
      video.Status = new VideoStatus();
            switch(UploadType.SelectedIndex)
            {
                case 0:
                    video.Status.PrivacyStatus = "public";
                    break;
                case 1:
                    video.Status.PrivacyStatus = "unlisted";
                    break;
                case 2:
                    video.Status.PrivacyStatus = "private";
                    break;
            }
   //   video.Status.PrivacyStatus = "unlisted"; // or "private" or "public"
            var filePath = uploadFilePath; 

      using (var fileStream = new FileStream(filePath, FileMode.Open))
      {        
        var videosInsertRequest = youtubeService.Videos.Insert(video, "snippet,status", fileStream, "video/*");
        videosInsertRequest.ProgressChanged += videosInsertRequest_ProgressChanged;
        videosInsertRequest.ResponseReceived += videosInsertRequest_ResponseReceived;
        await videosInsertRequest.UploadAsync();
      }
    }

    void videosInsertRequest_ProgressChanged(Google.Apis.Upload.IUploadProgress progress)
    {
      switch (progress.Status)
      {
        case UploadStatus.Uploading:
          MessageBox.Show(Convert.ToString(progress.BytesSent));
          break;

        case UploadStatus.Failed:
          MessageBox.Show("An error prevented the upload from completing.\n"+ progress.Exception);
          break;
      }
    }

    void videosInsertRequest_ResponseReceived(Video video)
    {
      MessageBox.Show("Video id '{0}' was successfully uploaded.", video.Id);
    }

    private void VideoDescription_LostFocus(object sender, RoutedEventArgs e)
    {
        if(VideoDescription.Text=="")
        VideoDescription.Text = "Description";
    }

    private void VideoDescription_GotFocus(object sender, RoutedEventArgs e)
    {
        VideoDescription.Text = "";
    }

    private void VideoTitle_GotFocus(object sender, RoutedEventArgs e)
    {
        VideoTitle.Text = "";
    }

    private void VideoTitle_LostFocus(object sender, RoutedEventArgs e)
    {
        if (VideoTitle.Text == "")
            VideoTitle.Text = "Title";
    }
    //sciezka  do uploadu
    private void Button_Click_4(object sender, RoutedEventArgs e)
    {
        OpenFileDialog openFileDialog1 = new OpenFileDialog();
        openFileDialog1.ValidateNames = true;      
        bool? userClickedOK = openFileDialog1.ShowDialog();    
        if (userClickedOK == true)
        {        
            InputTxt_Upload.Text = openFileDialog1.FileName;
        }
    }
  }
    }

