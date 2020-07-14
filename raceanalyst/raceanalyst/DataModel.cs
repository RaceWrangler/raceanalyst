using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Text;
using OpenCvSharp;
using OpenCvSharp.XImgProc;

namespace raceanalyst
{
    class DataModel
    {
        private DataModel() { }

        private static DataModel me = new DataModel();

        private List<Mat> chars = new List<Mat>();
        private List<int> labels = new List<int>();

        private OpenCvSharp.ML.SVM svm;

        public static DataModel GetDataModel()
        {
            return me;
        }

        private void Split2d(Mat img, OpenCvSharp.Size cell_size)
        {
            int height = img.Rows;
            int width = img.Cols;

            int sx = cell_size.Width;
            int sy = cell_size.Height;

            chars.Clear();

            for(int i = 0; i < height; i += sy)
            {
                for(int j = 0; j < width; j += sx)
                {
                    chars.Add(img[new Rect(j, i, sx, sy)]);
                }
            }
        }

        private List<double> bincount(Mat x, Mat weights, int min_length)
        {
            double max_x_val = 0;
            double min_val = 0;
            Cv2.MinMaxLoc(x, out min_val, out max_x_val);

            var bins = new double[(Math.Max((int)max_x_val, min_length))];

            for (int i = 0; i < x.Rows; i++)
            {
                for (int j = 0; j < x.Cols; j++)
                {
                    int idx = x.At<int>(i, j);
                    bins[idx] += weights.At<float>(i, j);
                }
            }

            return bins.ToList();
        }

        private Mat Preprocess_hog()
        {
            int bin_n = 16;
            int half_x = 30;
            int half_y = 15;
            double eps = 1e-7;

            var hog = new Mat(new OpenCvSharp.Size(4 * bin_n, chars.Count), MatType.CV_32F);

            for(int img_index = 0; img_index < chars.Count; img_index++)
            {
                var gx = new Mat();
                Cv2.Sobel(chars[img_index], gx, MatType.CV_32F, 1, 0);

                var gy = new Mat();
                Cv2.Sobel(chars[img_index], gy, MatType.CV_32F, 0, 1);

                var mag = new Mat();
                var ang = new Mat();

                Cv2.CartToPolar(gx, gy, mag, ang);

                var bin = new Mat(ang.Size(), MatType.CV_32F);

                for (int i = 0; i < ang.Rows; i++)
                {
                    for(int j = 0; j < ang.Cols; j++)
                    {
                        bin.At<int>(i, j) = (int)(bin_n * ang.At<float>(i, j) / (2 * Math.PI));
                    }
                }

                Mat[] bin_cells = {
                    bin[new Rect(0, 0, half_x, half_y)],
                    bin[new Rect(half_x, 0, half_x, half_y)],
                    bin[new Rect(0, half_y, half_x, half_y)],
                    bin[new Rect(half_x, half_y, half_x, half_y)]
                };
                Mat[] mag_cells = {
                    mag[new Rect(0, 0, half_x, half_y)],
                    mag[new Rect(half_x, 0, half_x, half_y)],
                    mag[new Rect(0, half_y, half_x, half_y)],
                    mag[new Rect(half_x, half_y, half_x, half_y)]
                };

                var hist = new List<double>(4 * bin_n);

                for(int i = 0; i < 4; i++)
                {
                    var partial_hist = bincount(bin_cells[i], mag_cells[i], bin_n);
                    hist.AddRange(partial_hist);
                }

                // Transform to Hellinger kernel
                double sum = 0;

                for(int i = 0; i < hist.Count; i++)
                {
                    sum += hist[i];
                }

                for (int i = 0; i < hist.Count; i++)
                {
                    hist[i] /= sum + eps;
                    hist[i] = Math.Sqrt(hist[i]);
                }

                double hist_norm = Cv2.Norm(OpenCvSharp.InputArray.Create(hist));


                for (int i = 0; i < hist.Count; i++)
                {
                    hog.At<float>((int)img_index, (int)i) = (float)(hist[i] / (hist_norm + eps));
                }
            }

            return hog;
        }

        

        public void Train(string trainingImage)
        {
            Console.WriteLine($"Loading {trainingImage} . . .");
            var char_img = new Mat(trainingImage, ImreadModes.Grayscale);

            Split2d(char_img, new OpenCvSharp.Size(60, 30));

            labels.Clear();

            int spc = (int)' ';
            int tld = (int)'~';
            int i = 0;
            foreach (var chr in chars)
            {
                labels.Add((spc + i));
                if (i == tld)
                {
                    i = 0;
                }
                else
                {
                    i++;
                }
            }

            Mat samples = Preprocess_hog();

            svm = OpenCvSharp.ML.SVM.Create();

            svm.Gamma = 5.383;
            svm.C = 2.67;
            svm.KernelType = OpenCvSharp.ML.SVM.KernelTypes.Rbf;
            svm.Type = OpenCvSharp.ML.SVM.Types.CSvc;

            svm.Train(samples, OpenCvSharp.ML.SampleTypes.RowSample, OpenCvSharp.InputArray.Create(labels));

            svm.Save("svm_model.dat");
        }

        internal void Analyze(string analyzeImage)
        {
            
        }
    }
}
