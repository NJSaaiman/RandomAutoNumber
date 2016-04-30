using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Printing;
using System.Drawing.Text;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using AutoNumber.Properties;

namespace AutoNumber
{
    public delegate void UpdateUIStatus(string msg);
    public delegate void UpdateUIListView(string msg);
    public delegate void UpdateUIImage(Image image);

    public partial class Form1 : Form
    {
        private BackgroundWorker _workerThread;
        private bool _workerCompleted = true;
        private bool _formClosePending = false;

        ConcurrentDictionary<int, Tuple<string, Image>> _foundNumberStore = new ConcurrentDictionary<int, Tuple<string, Image>>();

        PrivateFontCollection pfc = new PrivateFontCollection();

        public Form1()
        {
            InitializeComponent();
            SetupFont();

        }

        private void SetupFont()
        {
            foreach (string file in Directory.GetFiles(Path.Combine(Application.StartupPath, "fonts")))
            {
                pfc.AddFontFile(file);
            }

        }


        public void UpdatelblStatus(string msg)
        {
            if (this.progressBar1.InvokeRequired)
            {
                UpdateUIStatus upd = UpdatelblStatus;
                if (!this.IsDisposed)
                {
                    try
                    {
                        this.Invoke(upd, msg);
                    }
                    catch
                    { }
                }
            }
            else
            {
                if (!this.IsDisposed)
                {
                    try
                    {
                        if (msg == "-1")
                        {
                            progressBar1.MarqueeAnimationSpeed = 0;
                        }
                        else
                        {
                            progressBar1.MarqueeAnimationSpeed  =30;
                           // progressBar1.Style = ProgressBarStyle.Marquee;

                        }
                        
                    }
                    catch
                    { }
                }

            }
        }

        public void UpdateListView(string msg)
        {
            if (this.listView1.InvokeRequired)
            {
                UpdateUIListView upd = UpdateListView;
                if (!this.IsDisposed)
                {
                    try
                    {
                        this.Invoke(upd, msg);
                    }
                    catch
                    { }
                }
            }
            else
            {
                if (!this.IsDisposed)
                {
                    try
                    {
                        this.listView1.Items.Add(msg);
                    }
                    catch
                    { }
                }
            }
        }

        public void UpdateCurrentResultDisplay(Image image)
        {
            if (this.CurrentResultDisplay.InvokeRequired)
            {
                UpdateUIImage upd = UpdateCurrentResultDisplay;
                if (!this.IsDisposed)
                {
                    try
                    {
                        this.Invoke(upd, image);
                    }
                    catch
                    { }
                }
            }
            else
            {
                if (!this.IsDisposed)
                {
                    try
                    {
                        this.CurrentResultDisplay.Image = image;
                    }
                    catch
                    { }
                }
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Settings.Default["nl"] = (int)txtNumberLength.Value;
            Settings.Default["minnl"] = (int)txtMinValue.Value;
            Settings.Default["maxnl"] = (int)txtMaxValue.Value;
            Settings.Default.Save();

            this.button1.Enabled = false;

            this.CurrentResultDisplay.SizeMode = PictureBoxSizeMode.StretchImage;


            _workerThread.RunWorkerAsync();


        }

        private void GenerateNumber(int min, int max)
        {
            Random random = new Random();
            
            
            UpdatelblStatus("1");




            int v = GetRandomNumber(random);
            int passes = 0;
            while (true)
            {
                if (_workerThread.CancellationPending)
                {
                    return;
                }

                
                passes++;
                int nl = (int)this.txtNumberLength.Value;

                if (v <= max && v >= min && !_foundNumberStore.ContainsKey(v))
                {
                    string vs = v.ToString(CultureInfo.CurrentUICulture).PadLeft(nl, '0');
                    Image tmpImage = CreateBitmapImage(vs.Substring(vs.Length - nl));
                    UpdateCurrentResultDisplay(tmpImage);
                    _foundNumberStore.TryAdd(_foundNumberStore.Count, new Tuple<string, Image>(vs, tmpImage));
                    UpdateListView(string.Format(@"{0}: {1}", _foundNumberStore.Count, vs.Substring(vs.Length - nl)));
                    UpdatelblStatus("-1");
                    break;
                }
                else
                {
                    v = GetRandomNumber(random);
                    string vs = v.ToString(CultureInfo.CurrentUICulture).PadLeft(nl, '0');
                    UpdateCurrentResultDisplay(CreateBitmapImage(vs.Substring(vs.Length - nl)));
                }
            }
        }

        private int GetRandomNumber(Random random)
        {
            int nl = (int) this.txtNumberLength.Value;
            int[] randomValueVolt = new int[nl];


            for (int i = 0; i < randomValueVolt.Length; i++)
            {
                int randomNumber = random.Next(0, 9);
                randomValueVolt[i] = randomNumber;
            }


            return int.Parse(string.Join("", randomValueVolt));
        }

        private Bitmap CreateBitmapImage(string sImageText)
        {
            Bitmap objBmpImage = new Bitmap(1, 1);

            int intWidth = 0;
            int intHeight = 0;

            // Create the Font object for the image text drawing.
            FontFamily font = null;
            foreach (var fnt in pfc.Families)
            {
                if (fnt.Name.ToLowerInvariant() == Config.GetDisplayFont.ToLowerInvariant())
                {
                    font = fnt;
                }
            }

            font = FontFamily.Families
                    .Where(x => x.Name.ToLowerInvariant() == Config.GetDisplayFont.ToLowerInvariant())
                    .DefaultIfEmpty(null)
                    .FirstOrDefault();

            if (font == null)
            {
                font = new FontFamily("Arial");
            }

            Font objFont = new Font(font, (this.CurrentResultDisplay.Width / 4) < 200 ? 200 : this.CurrentResultDisplay.Width / 4, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Pixel);

            // Create a graphics object to measure the text's width and height.
            Graphics objGraphics = Graphics.FromImage(objBmpImage);

            // This is where the bitmap size is determined.

            intWidth = (int)objGraphics.MeasureString(sImageText, objFont).Width;
            intHeight = (int)objGraphics.MeasureString(sImageText, objFont).Height;
            Size sz = getScaledImageDimensions(intWidth, intHeight, this.CurrentResultDisplay.Width, this.CurrentResultDisplay.Height);
            // Create the bmpImage again with the correct size for the text and font.
            objBmpImage = new Bitmap(objBmpImage, new Size(sz.Width, sz.Height));

            // Add the colors to the new bitmap.
            objGraphics = Graphics.FromImage(objBmpImage);

            // Set Background color
            objGraphics.Clear(Color.White);
            objGraphics.SmoothingMode = SmoothingMode.AntiAlias;
            objGraphics.TextRenderingHint = TextRenderingHint.AntiAlias;
            objGraphics.CompositingQuality = CompositingQuality.HighQuality;
            objGraphics.CompositingMode = CompositingMode.SourceOver;
            objGraphics.DrawString(sImageText, objFont, new SolidBrush(Color.FromArgb(0, 0, 0)), (objBmpImage.Size.Width / 2) - (intWidth / 2), (objBmpImage.Size.Height / 2) - (intHeight / 2));
            objGraphics.Flush();

            return (objBmpImage);
        }

        private Size getScaledImageDimensions(int currentImageWidth, int currentImageHeight, int desiredImageWidth, int desiredImageHeight)
        {
            /* First, we must calculate a multiplier that will be used
        
                        * to get the dimensions of the new, scaled image.
                        */

            double scaleImageMultiplier = 0;

            /* This multiplier is defined as the ratio of the
        
            * Desired Dimension to the Current Dimension.
            * Specifically which dimension is used depends on the larger
            * dimension of the image, as this will be the constraining dimension
            * when we fit to the window.
            */

            /* Determine if Image is Portrait or Landscape. */
            if (currentImageHeight > currentImageWidth)    /* Image is Portrait */
            {
                /* Calculate the multiplier based on the heights. */
                if (desiredImageHeight > desiredImageWidth)
                {
                    scaleImageMultiplier = (double)desiredImageWidth / (double)currentImageWidth;
                }

                else
                {
                    scaleImageMultiplier = (double)desiredImageHeight / (double)currentImageHeight;
                }
            }

            else /* Image is Landscape */
            {
                /* Calculate the multiplier based on the widths. */
                if (desiredImageHeight >= desiredImageWidth)
                {
                    scaleImageMultiplier = (double)desiredImageWidth / (double)currentImageWidth;
                }

                else
                {
                    scaleImageMultiplier = (double)desiredImageHeight / (double)currentImageHeight;
                }
            }

            /* Generate and return the new scaled dimensions.
        
            * Essentially, we multiply each dimension of the original image
            * by the multiplier calculated above to yield the dimensions
            * of the scaled image. The scaled image can be larger or smaller
            * than the original.
            */

            return new Size(
            (int)(currentImageWidth * scaleImageMultiplier),
            (int)(currentImageHeight * scaleImageMultiplier));
        }

        private void butReset_Click(object sender, EventArgs e)
        {
            listView1.Clear();
            UpdateCurrentResultDisplay(null);
            _foundNumberStore.Clear();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

            txtNumberLength.Value = (int)Settings.Default["nl"];
            txtMinValue.Value = (int)Settings.Default["minnl"];
            txtMaxValue.Value = (int)Settings.Default["maxnl"];
            _workerThread = new BackgroundWorker() { WorkerSupportsCancellation = true };
            _workerThread.DoWork += _workerThread_DoWork;
            _workerThread.RunWorkerCompleted += _workerThread_RunWorkerCompleted;

            this.progressBar1.Style = ProgressBarStyle.Marquee;
            progressBar1.MarqueeAnimationSpeed = 0;

        }

        void _workerThread_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            this.button1.Enabled = true;
            UpdatelblStatus("-1");
            _workerCompleted = true;
            if (_formClosePending) this.Close();
        }

        void _workerThread_DoWork(object sender, DoWorkEventArgs e)
        {
            _workerCompleted = false;
            int min = int.Parse(this.txtMinValue.Text);
            int max = int.Parse(this.txtMaxValue.Text);
            GenerateNumber(min, max);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (!_workerCompleted)
            {
                
                this.Text += Resources.Form1_Form1_FormClosing__Please_wait_while_process_is_being_canceled;
                _workerThread.CancelAsync();
                this.Enabled = false;
                e.Cancel = true;
                _formClosePending = true;
                return;
            }

            _workerThread.DoWork -= _workerThread_DoWork;
            _workerThread.RunWorkerCompleted -= _workerThread_RunWorkerCompleted;
            _workerThread.Dispose();

            _foundNumberStore = null;
            pfc.Dispose();
            base.OnFormClosing(e);
            
            
            

        }

        private void txtMaxValue_ValueChanged(object sender, EventArgs e)
        {
            txtMinValue.Maximum = txtMaxValue.Value - 1;
        }

        private void splitContainer4_Panel1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void butStop_Click(object sender, EventArgs e)
        {
            if (!_workerCompleted)
            {
                _workerThread.CancelAsync();
            }
        }


        private int _printIndex = 0;
        private void butPrint_Click(object sender, EventArgs e)
        {
            _printIndex = 0;
            PrintDocument pd = new PrintDocument();
            pd.PrintPage += PrintPage;
            //pd.Print();
            
            PrintPreviewDialog ppd = new PrintPreviewDialog();
            ppd.Document = pd;
            ppd.ShowDialog();

        }

        private void PrintPage(object o, PrintPageEventArgs e)
        {

            if (_foundNumberStore.Count > 0)
            {
                e.Graphics.DrawImage(_foundNumberStore[_printIndex].Item2,
                    new Point((e.PageBounds.Width/2) - (_foundNumberStore[_printIndex].Item2.Width/2),
                        (e.PageBounds.Height/2) - (_foundNumberStore[_printIndex].Item2.Height/2)));
                e.Graphics.DrawString("Page No: " + (_printIndex + 1), new Font("Arial", 18, FontStyle.Bold),
                    new SolidBrush(Color.Black), new PointF(100, 50));
                e.Graphics.DrawString("Position Drawn: " + (_printIndex + 1), new Font("Arial", 18, FontStyle.Bold),
                    new SolidBrush(Color.Black), new PointF(100, 80));
                e.Graphics.DrawString("Date: " + DateTime.Now, new Font("Arial", 18, FontStyle.Bold),
                    new SolidBrush(Color.Black), new PointF(100, 110));

                _printIndex++;
                if (_printIndex < _foundNumberStore.Count())
                {

                    e.HasMorePages = true;
                }
            }

        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.Close();
        }


    }
}
