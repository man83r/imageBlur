using System;
using System.Drawing;
using System.Linq;

namespace imageBlur
{
    public static class GaussProcessing
    {
        private static int maxProgressBar = 0;
        private static int currentProgress = 0;
        private static Boolean breakAction = false; //прервать вычисления, если true
        private static int radius;
        private static Bitmap secondPicture;

        public static void setBreakProgress(Boolean breakActionSet)
        {
            breakAction = breakActionSet;
        }

        public static Boolean getBreakProgress()
        {
            return breakAction;
        }

        public static int getMaxValue()
        {
            return maxProgressBar;
        }

        public static int getProgress()
        {
            return currentProgress;
        }

        public static void setProgress(int progress)
        {
            currentProgress = progress;
        }

        public static Bitmap getImage()
        {
            return secondPicture;
        } 

        public static void RunImageBlur(int radiusSet, Bitmap firstImageSet)
        {
            radius = radiusSet;
            Bitmap firstPicture = new Bitmap(firstImageSet);

            currentProgress = 0;
            breakAction = false;

            int height = firstPicture.Height;
            maxProgressBar = height;
            int width = firstPicture.Width;

            int diam = radius * 2 + 1;

            double[,] simpleMatrix = GetMatrix(); //матрица весов пикселей
            secondPicture = new Bitmap(width, height);

            Color myColor = new Color(); //цвет пикселя после размытия
            Color[,] spotPixelBlur = new Color[diam, diam]; //массив пикселей для преобразования
            

            int im, jm; //вычисляемые индексы для формирования пятна

            for (int i = 0; i < height; i++) //столбцы
            {
                for (int j = 0; j < width; j++) //строки
                {
                    //сформируем пятно размытия пикселя
                    for (int ki = 0; ki < diam; ki++)
                    {
                        for (int kj = 0; kj < diam; kj++)
                        {
                            if (breakAction)
                            {
                                breakAction = false;
                                secondPicture = null;

                                return;
                            }

                            jm = j - radius + kj;

                            //если мы вышли за пределы массива, то внесём изменения
                            if ((jm < 0) || (jm > width - 1))
                            {
                                jm = j + radius - kj;
                            }

                            im = i - radius + ki;
                            if ((im < 0) || (im > height - 1))
                            {
                                im = i + radius - ki;
                            }

                            spotPixelBlur[kj, ki] = firstPicture.GetPixel(jm, im);
                        }

                    }

                    myColor = ChangeColor(spotPixelBlur, simpleMatrix);

                    secondPicture.SetPixel(j, i, myColor);

                }
                currentProgress++;
            }

        }


        public static void RunFastImageBlur(int radiusSet, Bitmap firstImageSet)
        {
            radius = radiusSet;
            Bitmap firstPicture = new Bitmap(firstImageSet);

            currentProgress = 0;
            breakAction = false;

            int height = firstPicture.Height;
            int width = firstPicture.Width;
            maxProgressBar = height + width;

            int diam = radius * 2 + 1;

            double[] gaussLine = GetGaussLine(); //линия весов пикселей
            Bitmap tempPicture = new Bitmap(width, height);

            Color myColor = new Color(); //цвет пикселя после размытия
            Color[] linePixelBlur = new Color[diam]; //массив пикселей для преобразования

            int jm; //вычисляемый индексы для формирования линии размытия

            for (int i = 0; i < height; i++) //столбцы
            {
                for (int j = 0; j < width; j++) //строки
                {
                    //сформируем линию размытия пикселя
                    for (int kj = 0; kj < diam; kj++)
                    {
                            if (breakAction)
                            {
                                breakAction = false;
                                secondPicture = null;
                                return;
                            }

                            jm = j - radius + kj;

                            //если мы вышли за пределы массива, то внесём изменения
                            if ((jm < 0) || (jm > width - 1))
                            {
                                jm = j + radius - kj;
                            }

                            linePixelBlur[kj] = firstPicture.GetPixel(jm, i);

                    }

                    myColor = FastChangeColor(linePixelBlur, gaussLine);

                    tempPicture.SetPixel(j, i, myColor);

                }
                currentProgress++;
            }

            secondPicture = new Bitmap(width, height);

            for (int i = 0; i < width; i++) //строки
            {
                for (int j = 0; j < height; j++) //столбцы
                {
                    //сформируем линию размытия пикселя
                    for (int kj = 0; kj < diam; kj++)
                    {
                        if (breakAction)
                        {
                            breakAction = false;
                            secondPicture = null;

                            return;
                        }

                        jm = j - radius + kj;

                        //если мы вышли за пределы массива, то внесём изменения
                        if ((jm < 0) || (jm > height - 1))
                        {
                            jm = j + radius - kj;
                        }

                        linePixelBlur[kj] = tempPicture.GetPixel(i, jm);

                    }

                    myColor = FastChangeColor(linePixelBlur, gaussLine);

                    secondPicture.SetPixel(i, j, myColor);

                }
                currentProgress++;
            }

        }

        public static void RunByteImageBlur(int radiusSet, Bitmap firstImageSet)
        {
            radius = radiusSet;
            Bitmap firstPicture = new Bitmap(firstImageSet);

            currentProgress = 0;
            breakAction = false;

            int height = firstPicture.Height;
            int width = firstPicture.Width;
            maxProgressBar = height + width;

            //залочим биты
            Rectangle rect = new Rectangle(0, 0, firstPicture.Width, firstPicture.Height);
            System.Drawing.Imaging.BitmapData bmpData =
                firstPicture.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadWrite,
                firstPicture.PixelFormat);

            // Get the address of the first line.
            IntPtr ptr = bmpData.Scan0;

            // Declare an array to hold the bytes of the bitmap.
            int bytes = Math.Abs(bmpData.Stride) * height;
            byte[] byteArrayInnerPicture = new byte[bytes];

            // Copy the ARGB values into the array.
            System.Runtime.InteropServices.Marshal.Copy(ptr, byteArrayInnerPicture, 0, bytes);

            byte[] byteArrayTempPicture = byteArrayInnerPicture.ToArray();

            double[] gaussLine = GetGaussLine(); //линия весов пикселей
            double b, r, g, a;
            //размоем по горизонтали----------------------------------------------------
            for (int rowNumber = 1; rowNumber <= height; rowNumber++)
            {

                int startRow = (rowNumber - 1) * width * 4; //3 для RGB, 4 ARGB
                int endRow = startRow + width * 4 - 1;

                  b = r = g = a = 0;

                int jm; //вычисляемый индекс для перемножения пикселей на матрицу Гаусса
                for (int i = startRow; i <= endRow;) //пробежимся по всей строке
                {
                    for (int j = 0; j < gaussLine.Length; j++)
                    {
                        if (breakAction)
                        {
                            breakAction = false;
                            secondPicture = null;

                            return;
                        }

                        jm = i + (j - radius) * 4;

                        if ((jm < startRow) || (jm > endRow - 1))
                        {
                            jm = i + (radius - j) * 4;
                        }

                        b += byteArrayInnerPicture[jm] * gaussLine[j];
                        r += byteArrayInnerPicture[jm + 1] * gaussLine[j];
                        g += byteArrayInnerPicture[jm + 2] * gaussLine[j];
                        a += byteArrayInnerPicture[jm + 3] * gaussLine[j];
                    }

                    if (b > 255) b = 255;
                    if (r > 255) r = 255;
                    if (g > 255) g = 255;
                    if (a > 255) a = 255;

                    byteArrayTempPicture[i] = (byte)b;
                    byteArrayTempPicture[i + 1] = (byte)r;
                    byteArrayTempPicture[i + 2] = (byte)g;
                    byteArrayTempPicture[i + 3] = (byte)a;

                    i = i + 4;
                    r = g = b = 0;  
                }
                //Bitmap secondPicture = new Bitmap(blurImageModifire.getBlurImage());
                currentProgress++;
            }
            //конец размоем по горизонтали----------------------------------------------------

            //размоем по вертикали------------------------------------------------------------
            for (int colNumber = 1; colNumber <= width; colNumber++)
            {

                int startCol = (colNumber - 1) * 4; //3 для RGB, 4 ARGB
                int endCol = startCol + width * 4 * (height - 1);

                b = r = g = a = 0;

                int jm; //вычисляемый индекс для перемножения пикселей на матрицу Гаусса
                for (int i = startCol; i <= endCol;) //пробежимся по всему столбцу
                {
                    for (int j = 0; j < gaussLine.Length; j++)
                    {
                        if (breakAction)
                        {
                            breakAction = false;
                            secondPicture = null;

                            return;
                        }
                        jm = i + (j - radius) * 4 * width;

                        if ((jm < startCol) || (jm > endCol - 1))
                        {
                            jm = i + (radius - j) * 4 * width;
                        }

                        //используем промежуточный массив как исходник для повторного прохода
                        b += byteArrayTempPicture[jm] * gaussLine[j];
                        r += byteArrayTempPicture[jm + 1] * gaussLine[j];
                        g += byteArrayTempPicture[jm + 2] * gaussLine[j];
                        a += byteArrayTempPicture[jm + 3] * gaussLine[j];
                    }

                    if (b > 255) b = 255;
                    if (r > 255) r = 255;
                    if (g > 255) g = 255;
                    if (a > 255) a = 255;

                    //результат сохраним в исходный массив
                    byteArrayInnerPicture[i] = (byte)b;
                    byteArrayInnerPicture[i + 1] = (byte)r;
                    byteArrayInnerPicture[i + 2] = (byte)g;
                    byteArrayInnerPicture[i + 3] = (byte)a;

                    i = i + width * 4;
                    r = g = b = 0;
                }
                //Bitmap secondPicture = new Bitmap(blurImageModifire.getBlurImage());
                currentProgress++;
            }
            //конец размоем по вертикали------------------------------------------------------

            System.Runtime.InteropServices.Marshal.Copy(byteArrayInnerPicture, 0, ptr, bytes);

            // Unlock the bits.
            firstPicture.UnlockBits(bmpData);

            secondPicture = new Bitmap(firstPicture);
        }


        private static Color ChangeColor(Color[,] spotPixelBlur, double[,] simpleMatrix)
        {
            byte r, g, b;
            double rm, gm, bm;
            r = g = b = 0;
            rm = gm = bm = 0;
            int length = radius * 2 + 1;

            for (int i = 0; i < length; i++) //столбцы
            {
                for (int j = 0; j < length; j++) //строки
                {
                    rm = rm + spotPixelBlur[j, i].R * simpleMatrix[j, i];
                    gm = gm + spotPixelBlur[j, i].G * simpleMatrix[j, i];
                    bm = bm + spotPixelBlur[j, i].B * simpleMatrix[j, i];
                }
            }
            if (rm > 255) rm = 255;
            if (gm > 255) gm = 255;
            if (bm > 255) bm = 255;
            r = (byte)rm;
            g = (byte)gm;
            b = (byte)bm;

            Color color = new Color();
            color = Color.FromArgb(r, g, b);

            return color;
        }

        private static double[,] GetMatrix()
        {
            //double sigma = 1; //пока сигма везде 1
            int diam = 2 * radius + 1; //диаметр
            double sigma = (radius - 0.5) / 3;
            double[,] simplMatrix = new double[diam, diam];

            for (int i = 0; i < diam; i++) //столбцы
            {
                for (int j = 0; j < diam; j++)  //строки
                {
                    int x = j - radius;
                    int y = i - radius;

                    double stepen = -(x * x + y * y) / (2 * sigma * sigma); //степень функции
                    simplMatrix[j, i] = (1 / (2 * Math.PI * Math.Pow(sigma, 2))) * Math.Pow(Math.E, stepen);
                }
            }

            return simplMatrix;
        }

        private static double[] GetGaussLine()
        {
            int diam = 2 * radius + 1; //диаметр
            double sigma = (radius - 0.5) / 3;
            double[] gaussLine = new double[diam];

            for (int i = 0; i < diam; i++)
            {
                    int x = i - radius;

                    double stepen = -(x * x) / (2 * sigma * sigma); //степень функции
                    gaussLine[i] = (1 / (Math.Sqrt(2 * Math.PI * Math.Pow(sigma, 2)))) * Math.Pow(Math.E, stepen);
            }

            return gaussLine;
        }

        private static Color FastChangeColor(Color[] linePixelBlur, double[] gaussLine)
        {
            byte r, g, b;
            double rm, gm, bm;
            r = g = b = 0;
            rm = gm = bm = 0;
            int length = radius * 2 + 1;

            for (int i = 0; i < length; i++)
            {
                    rm = rm + linePixelBlur[i].R * gaussLine[i];
                    gm = gm + linePixelBlur[i].G * gaussLine[i];
                    bm = bm + linePixelBlur[i].B * gaussLine[i];
            }
            if (rm > 255) rm = 255;
            if (gm > 255) gm = 255;
            if (bm > 255) bm = 255;
            r = (byte)rm;
            g = (byte)gm;
            b = (byte)bm;

            Color color = new Color();
            color = Color.FromArgb(r, g, b);

            return color;
        }
    }
}
