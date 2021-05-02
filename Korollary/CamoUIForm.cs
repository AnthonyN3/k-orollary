using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Korollary
{
    public partial class CamoUIForm : Form
    {
        //Path of uploaded image
        private string imagePath = "";
        private string haskellProgramPath = @".\Haskell Programs";
        private string haskellProgramName = "Kmeans.exe";
        //private string haskellProgramArguments = "";
        //Stores the image in this byte array.
        private byte[] rgbValues;
        private int bytesInPixels;

        private BindingList<String> outputModeList = new BindingList<String>(Enum.GetNames(typeof(OutputMode)));
        private int currentOutputMode;
        private int currentKClusters;

        enum OutputMode
        {
            Image,
        }

        public CamoUIForm()
        {
            InitializeComponent();
            button2.Enabled = false;
            button3.Enabled = false;
            currentKClusters = (int)numericUpDown1.Value;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            pictureBox1.ImageLocation = null;
            //String imagePath = "";
            try
            {
                OpenFileDialog dialog = new OpenFileDialog();
                dialog.Filter = "Image files (*.jpg, *.jpeg, *.jpe, *.jfif, *.png, *.bmp, *.tiff) | *.jpg; *.jpeg; *.jpe; *.jfif; *.png; *.bmp; *.tiff";
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    Cursor.Current = Cursors.WaitCursor;
                    this.Enabled = false;
                    imagePath = dialog.FileName;
                    System.Diagnostics.Debug.WriteLine(imagePath);
                    pictureBox1.ImageLocation = imagePath;
                    pictureBox2.Image = null;
                    button3.Enabled = false;
                    this.Enabled = true;
                    Cursor.Current = Cursors.Default;
                }

            }
            catch (Exception)
            {
                System.Windows.Forms.MessageBox.Show($"Invalid image file \"{imagePath}\"");
            }
            button2.Enabled = true;
        }

        //Compute onClick: Haskell Call
        private void button2_Click(object sender, EventArgs e)
        {
            Cursor.Current = Cursors.WaitCursor;
            this.Enabled = false;
            PixelConvert2();
            ExternalProgramCall(haskellProgramPath, haskellProgramName, currentKClusters.ToString());
            this.Enabled = true;
            Cursor.Current = Cursors.Default;
            pictureBox2.Image = CreateImageFromByteArray();
            button3.Enabled = true;
        }

        private void ExternalProgramCall(string programPath, string programName, string arguments)
        {
            string filePath = Path.Combine(programPath, programName);

            System.Diagnostics.ProcessStartInfo processInfo = new System.Diagnostics.ProcessStartInfo(filePath, arguments);
            processInfo.UseShellExecute = false;
            processInfo.CreateNoWindow = true;
            processInfo.RedirectStandardOutput = true;
            processInfo.RedirectStandardInput = true;

            System.Diagnostics.Process externalProccess = new System.Diagnostics.Process { StartInfo = processInfo };

            externalProccess.Start();

            int pixelsSent = 0;
            for (int i = 0; i < rgbValues.Length; i += bytesInPixels)
            {
                if (bytesInPixels != 4 || rgbValues[i + 3] != 0)
                {
                    string rgbpixel = $"{rgbValues[i]} {rgbValues[i + 1]} {rgbValues[i + 2]}";
                    externalProccess.StandardInput.WriteLine(rgbpixel);

                    ++pixelsSent;
                }
            }

            externalProccess.StandardInput.Close();

            int index = 0;
            int pixelsReceived = 0;
            while (!externalProccess.StandardOutput.EndOfStream)
            {
                string line = externalProccess.StandardOutput.ReadLine();
                string[] pixelValues = line.Trim().Split(' ');

                //RGB format back
                if (bytesInPixels == 4)
                {
                    while (rgbValues[index + 3] == 0)
                    {
                        index += bytesInPixels;
                    }
                }
                rgbValues[index] = byte.Parse(pixelValues[0]);
                rgbValues[index + 1] = byte.Parse(pixelValues[1]);
                rgbValues[index + 2] = byte.Parse(pixelValues[2]);

                index += bytesInPixels;
                ++pixelsReceived;
            }

            System.Diagnostics.Debug.WriteLine($"Received {pixelsReceived} of {pixelsSent} pixels");
        }

        private void PixelConvert2()
        {
            Bitmap bmp = new Bitmap(imagePath);
            int width = bmp.Width;
            int height = bmp.Height;
            int maxPointer = width * height * 4; //There are 4 bytes per pixels
            //int stride = w * 4;

            //is it rgba or rgb (3 or 4)
            bytesInPixels = (bmp.PixelFormat == PixelFormat.Format32bppArgb || bmp.PixelFormat == PixelFormat.Format32bppRgb) ? 4 : 3;
            Rectangle rect = new Rectangle(0, 0, width, height);
            BitmapData bmpData = bmp.LockBits(rect, ImageLockMode.ReadWrite, bmp.PixelFormat); //Canonical : The default pixel format of 32 bits per pixel. The format specifies 24-bit color depth and an 8-bit alpha channel.


            //byte* scan0 = (byte*)bmpData.Scan0.ToPointer();
            IntPtr ptr = bmpData.Scan0;

            int bytes = bytesInPixels * width * height;

            rgbValues = new byte[bytes];
            System.Runtime.InteropServices.Marshal.Copy(ptr, rgbValues, 0, bytes);

            System.Diagnostics.Debug.WriteLine("The Type: " + bmp.GetType());

            bmp.UnlockBits(bmpData);
        }

        //Save Image onClick:   
        private void button3_Click(object sender, EventArgs e)
        {
            Image currentImage = pictureBox2.Image;
            ImageFormat imageFormat = currentImage.RawFormat;

            //Stream myStream;
            SaveFileDialog saveFileDialog1 = new SaveFileDialog();

            saveFileDialog1.Filter = "Image files (*.jpg, *.jpeg, *.gif, *.png, *.bmp, *.tiff) | *.jpg; *.jpeg; *.gif; *.png; *.bmp; *.tiff";
            saveFileDialog1.FilterIndex = 1;
            saveFileDialog1.RestoreDirectory = true;
            saveFileDialog1.DefaultExt = imageFormat.ToString();
            saveFileDialog1.FileName = "result";

            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                string fileExtension = System.IO.Path.GetExtension(saveFileDialog1.FileName);
                switch (fileExtension)
                {
                    case ".jpg":
                        imageFormat = ImageFormat.Jpeg;
                        break;
                    case ".jpeg":
                        imageFormat = ImageFormat.Jpeg;
                        break;
                    case ".gif":
                        imageFormat = ImageFormat.Gif;
                        break;
                    case ".png":
                        imageFormat = ImageFormat.Png;
                        break;
                    case ".bmp":
                        imageFormat = ImageFormat.Bmp;
                        break;
                    case ".tiff":
                        imageFormat = ImageFormat.Tiff;
                        break;
                    default:
                        imageFormat = ImageFormat.Jpeg;
                        break;
                }

                currentImage.Save(saveFileDialog1.FileName, imageFormat);
            }
        }

        private Image CreateImageFromByteArray()
        {
            Bitmap newImage = new Bitmap(imagePath);
            Rectangle area = new Rectangle(0, 0, newImage.Width, newImage.Height);
            BitmapData newImageData = newImage.LockBits(area, ImageLockMode.ReadWrite, newImage.PixelFormat);

            unsafe
            {
                byte* ptr = (byte*)newImageData.Scan0;

                for (int i = 0; i < rgbValues.Length; ++i)
                {
                    ptr[i] = rgbValues[i];
                }
            }
            newImage.UnlockBits(newImageData);

            return newImage;
        }


        private void numericUpDown1_ValueChanged_1(object sender, EventArgs e)
        {
            //K Means Clusters Change Value
            Type currentType = sender.GetType();

            if (currentType.Equals(typeof(NumericUpDown)))
            {
                NumericUpDown temp = (NumericUpDown)sender;
                currentKClusters = (int)temp.Value;
            }
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            //Output Mode Change
            Type currentType = sender.GetType();

            if (currentType.Equals(typeof(ListBox)))
            {
                ListBox temp = (ListBox)sender;
                currentOutputMode = (int)temp.SelectedIndex;
            }
        }
    }
}
