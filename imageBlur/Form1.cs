using System;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Security.Cryptography; //для получения хэша открытого файла
using System.Drawing.Imaging;
using System.Threading;

namespace imageBlur
{
    public partial class Form1 : Form
    {
        int radius; //радиус размытия
        string hashOfFile;
        private readonly string CONFIG_PATH = $"{Application.StartupPath}\\imageBlur.cfg";
        private readonly string CACH_PATH = $"{Application.StartupPath}\\cach";
        Bitmap loadedImage;

        public Form1()
        {
            InitializeComponent();

            //проверяем файл настроек
            if (File.Exists(CONFIG_PATH))
            {
                try
                { 
                    radius = Convert.ToInt32(File.ReadAllText(CONFIG_PATH, Encoding.GetEncoding(1251)));
                    if (radius > 20 | radius < 2) radius = 2; //если некорректное значение - пересоздаём
                    fCreatCfgFile();
                }
                catch //если не инт пересоздаём
                {
                    radius = 2; 
                    fCreatCfgFile();
                }
            }
            else //если не существует - создаём
            {
                radius = 2;
                fCreatCfgFile();
            }

            //если нет директории cach - создадим её
            if (!Directory.Exists(CACH_PATH)) Directory.CreateDirectory(CACH_PATH);

            //включаем полосы прокрутки
            panel1.AutoScroll = true;
            pictureBox1.SizeMode = PictureBoxSizeMode.AutoSize;
            panel2.AutoScroll = true;
            pictureBox2.SizeMode = PictureBoxSizeMode.AutoSize;
            trackBar1.Value = radius;
            label1.Text = Convert.ToString(radius); //значение радиуса размытия
            SaveToolStripMenuItem.Enabled = false;  //выключаем пункт меню СОХРАНИТЬ
        }

        private void fCreatCfgFile()
        {
        File.WriteAllText(CONFIG_PATH, Convert.ToString(radius), Encoding.GetEncoding(1251));
        }


        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Images|*.jpg;*.bmp;*.png";
            if (ofd.ShowDialog() == DialogResult.OK)   //если вернулся ОК, то грузим картинки
            {
                using (FileStream fs = new FileStream(ofd.FileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    Bitmap imgsource = new Bitmap(fs);

                    loadedImage = imgsource.Clone(new Rectangle(0, 0, imgsource.Width, imgsource.Height),
        PixelFormat.Format24bppRgb);

                    pictureBox1.Image = loadedImage;
                }
                
                hashOfFile = ComputeMD5Checksum(ofd.FileName);

                //проверим есть ли в директории cach такой файл
                if (File.Exists(CACH_PATH + "\\" + hashOfFile)) //и загрузим его из кэша
                {
                    //закроем поток после чтения
                    using (FileStream fs = new FileStream(CACH_PATH + "\\" + hashOfFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        pictureBox2.Image = Image.FromStream(fs);
                    }

                    SaveToolStripMenuItem.Enabled = true;
                }
                else
                {
                    pictureBox2.Image = null;
                    SaveToolStripMenuItem.Enabled = false;
                }

            }
        }

        //вычислим хэш файла
        private string ComputeMD5Checksum(string path)
        {
            using (FileStream fs = File.OpenRead(path))
            {
                MD5 md5 = new MD5CryptoServiceProvider();
                byte[] fileData = new byte[fs.Length];
                fs.Read(fileData, 0, (int)fs.Length);
                byte[] checkSum = md5.ComputeHash(fileData);
                string result = BitConverter.ToString(checkSum).Replace("-", String.Empty);
                return result;
            }
        }

        private void SaveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "Images (*.jpg)| *.jpg|Images (*.bmp)| *.bmp|Images (*.png)| *.png";
            sfd.ShowDialog();
            if (sfd.FileName != "")
            {
                string fileName = sfd.FileName;
                string fileExt = fileName.Substring(fileName.Length-3,3);
                try
                {
                    if (String.Compare(fileExt,"jpg") == 0) pictureBox2.Image.Save(sfd.FileName, System.Drawing.Imaging.ImageFormat.Jpeg);
                    if (String.Compare(fileExt,"bmp") == 0) pictureBox2.Image.Save(sfd.FileName, System.Drawing.Imaging.ImageFormat.Bmp);
                    if (String.Compare(fileExt,"png") == 0) pictureBox2.Image.Save(sfd.FileName, System.Drawing.Imaging.ImageFormat.Png);
                }
                catch
                {
                    MessageBox.Show("Невозможно сохранить изображение", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        //выполнить преобразование
        private void button1_Click(object sender, EventArgs e)
        {
            if (pictureBox1.Image == null)
            {
                MessageBox.Show("Сначала откройте картинку: Файл-Открыть.");
                return;
            }

            GaussProcessing.setProgress(0);

            if (radioButton1.Checked)
            {
                Task.Run(() => {
                    GaussProcessing.RunImageBlur(radius, loadedImage);
                });
            }
            if (radioButton2.Checked)
            {   //быстрое преобразование
                Task.Run(() => {
                    GaussProcessing.RunFastImageBlur(radius, loadedImage);
                });
            }
            if (radioButton3.Checked)
            {   //быстрое преобразование Byte
                Task.Run(() => {
                    // GaussProcessing.RunFastImageBlur(radius, loadedImage);
                    GaussProcessing.RunByteImageBlur(radius, loadedImage);
                });
            }

            progressDialog progressDialogWindows = new progressDialog();
            progressDialogWindows.StartPosition = FormStartPosition.Manual; //разрешаем ручной ввод координат окна
            progressDialogWindows.Location = new Point(600, 400); //открываем окно по координатам
            progressDialogWindows.ShowDialog();

            Thread.Sleep(150);

            pictureBox2.Image = GaussProcessing.getImage();
            if (pictureBox2.Image == null) SaveToolStripMenuItem.Enabled = false;
            else
            {
                SaveToolStripMenuItem.Enabled = true;
                //закэшируем изображение
                pictureBox2.Image.Save(CACH_PATH + "\\" + hashOfFile, ImageFormat.Jpeg);
            }     
        }


        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            radius = trackBar1.Value;
            label1.Text = Convert.ToString(radius);
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            fCreatCfgFile();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            fCreatCfgFile();
            Application.Exit();
        }


        private void AboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            About aboutProgramm = new About();
            aboutProgramm.StartPosition = FormStartPosition.Manual; //разрешаем ручной ввод координат окна
            aboutProgramm.Location = new Point(400, 300); //открываем окно по координатам
            aboutProgramm.ShowDialog();
        }
    }
}
