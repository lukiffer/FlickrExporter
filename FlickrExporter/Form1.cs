using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using FlickrNet;

namespace FlickrExporter
{
    public partial class Form1 : Form
    {
        private Flickr _flickr;
        const string API_KEY = "8ac7ab0ef52809cb5f3a9d8679f10d34";
        const string SECRET = "4e26a00c626d0440";
        
        public Form1()
        {
            InitializeComponent();
            this._flickr = new Flickr(API_KEY, SECRET);
        }

        private List<KeyValuePair<string, Photoset>> _sets = new List<KeyValuePair<string, Photoset>>();

        private void webBrowser1_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            if (webBrowser1.Url != null && webBrowser1.Url.DnsSafeHost.Equals("lukefritz.com", StringComparison.OrdinalIgnoreCase))
            {
                var frob = webBrowser1.Url.Query.Split('=')[1];
                var token = _flickr.AuthGetToken(frob).Token;

                webBrowser1.Hide();
                label2.Show();
                label3.Show();
                label4.Show();
                label5.Show();
                label6.Show();
                label7.Show();
                checkedListBox1.Show();
                comboBox1.Show();
                button1.Show();
                button2.Show();
                button3.Show();
                progressBar1.Show();

                _flickr = new Flickr(API_KEY, SECRET, token);
                
                var sets = _flickr.PhotosetsGetList();
                
                foreach (var set in sets)
                {
                    checkedListBox1.Items.Add(set.Title + " (" + set.PhotosetId + ")", true);
                    _sets.Add(new KeyValuePair<string, Photoset>(set.PhotosetId, set));
                }
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.Left = (Screen.PrimaryScreen.WorkingArea.Width / 2) - (this.Width / 2);
            this.Top = (Screen.PrimaryScreen.WorkingArea.Height / 2) - (this.Height / 2);

            label2.Text = Environment.CurrentDirectory;

            comboBox1.Items.Add("Original");
            comboBox1.Items.Add("Large");
            comboBox1.Items.Add("Medium");
            comboBox1.Items.Add("Small");
            comboBox1.SelectedItem = "Original";
            
            login();
        }

        private void login()
        {
            label2.Hide();
            label3.Hide();
            label4.Hide();
            label5.Hide();
            label6.Hide();
            label7.Hide();
            checkedListBox1.Hide();
            comboBox1.Hide();
            button1.Hide();
            button2.Hide();
            button3.Hide();
            progressBar1.Hide();
            
            webBrowser1.Show();
            webBrowser1.Width = this.Width;
            webBrowser1.Height = this.Height - 80;
            webBrowser1.Top = 80;
            webBrowser1.Left = 0;
            
            var prefrob = _flickr.AuthGetFrob();
            var url = _flickr.AuthCalcUrl(prefrob, AuthLevel.Read);
            webBrowser1.Navigate(url);
        }
        
        private void button1_Click(object sender, EventArgs e)
        {
            folderBrowserDialog1.ShowDialog();
            label2.Text = folderBrowserDialog1.SelectedPath;
        }
        
        private void button3_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            var allPhotos = new List<KeyValuePair<Photoset, List<Photo>>>();

            foreach (var item in checkedListBox1.CheckedItems)
            {
                var key = item.ToString().Substring(item.ToString().LastIndexOf('(') + 1);
                key = key.Substring(0, key.Length - 1);
                var set = _sets.Single(x => x.Key == key).Value;

                try
                {
                    var photos = _flickr.PhotosetsGetPhotos(set.PhotosetId, PhotoSearchExtras.OriginalUrl);

                    allPhotos.Add(new KeyValuePair<Photoset, List<Photo>>(set, photos.ToList()));
                }
                catch { }
            }

            var totalPhotoCount = allPhotos.SelectMany(x => x.Value).Count();

            progressBar1.Maximum = totalPhotoCount;
            progressBar1.Minimum = 0;

            var i = 1;

            foreach (var set in allPhotos)
            {
                label7.Text = "Starting set " + set.Key.Title;
                label7.Refresh();

                Directory.CreateDirectory(folderBrowserDialog1.SelectedPath + "\\" + set.Key.Title + "\\");

                foreach (var photo in set.Value)
                {
                    label7.Text = "Saving image " + i + " of " + totalPhotoCount + ", " + photo.Title;
                    label7.Refresh();

                    var path = folderBrowserDialog1.SelectedPath + "\\" + set.Key.Title + "\\" + photo.PhotoId + "_" + photo.Title + ".jpg";

                    var context = _flickr.PhotosGetContext(photo.PhotoId);

                    WebResponse objResponse;
                    WebRequest objRequest = System.Net.HttpWebRequest.Create(photo.OriginalUrl);
                    objResponse = objRequest.GetResponse();

                    File.WriteAllBytes(path, read(objResponse.GetResponseStream()));

                    progressBar1.Increment(1);
                    progressBar1.Refresh();
                    i++;
                }
            }

            label7.Text = "Export completed.";
        }

        private static byte[] read(Stream input)
        {
            byte[] buffer = new byte[16 * 1024];
            using (MemoryStream ms = new MemoryStream())
            {
                int read;
                while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ms.Write(buffer, 0, read);
                }
                return ms.ToArray();
            }
        }
    }
}
