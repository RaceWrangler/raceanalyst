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
        private struct character
        {
            public int ord;
            public int x;
            public int y;
        }

        private DataModel() { }

        private static DataModel me = new DataModel();

        private List<Mat> chars = new List<Mat>();
        private List<int> labels = new List<int>();

        private OpenCvSharp.ML.SVM model;

        private const string modelFile = "svm_model.dat";

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

        private Mat Deskew(Mat img)
        {
            Moments m = new Moments(img);
            Mat deskewed_img = new Mat();

            if(Math.Abs(m.Mu02) < 0.01)
            {
                deskewed_img = img.Clone();
                return deskewed_img;
            }

            float skew = (float)(m.Mu11 / m.Mu02);
            float[,] M_vals = new float[,] { { 1, skew, (float)( 0.5 * 60 * skew) }, { 0, 1, 0 } };
            Mat M = new Mat(new OpenCvSharp.Size(3, 2), MatType.CV_32F);

            for (int i = 0; i < M.Rows; i++)
            {
                for (int j = 0; j < M.Cols; j++)
                {
                    M.At<float>(i, j) = M_vals[i,j];
                }
            }

            Cv2.WarpAffine(img, deskewed_img, M, new OpenCvSharp.Size(60, 30), InterpolationFlags.WarpInverseMap | InterpolationFlags.Linear);

            return deskewed_img;
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

        private Mat Preprocess_hog(List<Mat> input)
        {
            int bin_n = 16;
            int half_x = 30;
            int half_y = 15;
            double eps = 1e-7;

            var hog = new Mat(new OpenCvSharp.Size(4 * bin_n, input.Count), MatType.CV_32F);

            for(int img_index = 0; img_index < input.Count; img_index++)
            {
                var gx = new Mat();
                Cv2.Sobel(input[img_index], gx, MatType.CV_32F, 1, 0);

                var gy = new Mat();
                Cv2.Sobel(input[img_index], gy, MatType.CV_32F, 0, 1);

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

            Mat samples = Preprocess_hog(chars);

            model = OpenCvSharp.ML.SVM.Create();

            model.Gamma = 5.383;
            model.C = 2.67;
            model.KernelType = OpenCvSharp.ML.SVM.KernelTypes.Rbf;
            model.Type = OpenCvSharp.ML.SVM.Types.CSvc;

            model.Train(samples, OpenCvSharp.ML.SampleTypes.RowSample, OpenCvSharp.InputArray.Create(labels));

            model.Save(modelFile);
        }

        internal void Analyze(string analyzeImage)
        {
            model = OpenCvSharp.ML.SVM.Load(modelFile);


            Console.WriteLine($"Loading {analyzeImage} . . .");
            var img = new Mat(analyzeImage, ImreadModes.Grayscale);

            Mat cnt_img = new Mat(img.Size(), MatType.CV_32F);


            Mat threshbin = new Mat();
            Cv2.AdaptiveThreshold(img, threshbin, 1.0, AdaptiveThresholdTypes.MeanC, ThresholdTypes.BinaryInv, 31, 10);

            Mat bin = new Mat();
            Cv2.MedianBlur(threshbin, bin, 3);

            HierarchyIndex[] heirs;
            OpenCvSharp.Point[][] contours;
            bin.FindContours(out contours, out heirs, RetrievalModes.List, ContourApproximationModes.ApproxNone);
                

            List<character> chrs = new List<character>();
            foreach (var contour in contours)
            {
                // Draw a rect around the contour!
                var boundingRect = Cv2.BoundingRect(contour);
                if (boundingRect.Height > 1 && boundingRect.Width > 1)
                {
                    // Let's analyze each contour!
                    var bin_norms = new List<Mat>();
                    bin_norms.Add(Deskew(new Mat(img, boundingRect)));


                    var sample = Preprocess_hog(bin_norms);

                    var ord1 = (int)model.Predict(sample);

                    chrs.Add(new character()
                    {
                        ord = ord1,
                        x = boundingRect.Left,
                        y = boundingRect.Top
                    });

                    if ((ord1 >= Convert.ToInt32(' '))
                        && (ord1 <  Convert.ToInt32('~')))
                    {
                        Cv2.Rectangle(img, boundingRect, 255);
                        Cv2.PutText(img, $"{Convert.ToChar(ord1)}", boundingRect.TopLeft, HersheyFonts.HersheyPlain, 2.0, 255);
                    }
                }
            }

            cnt_img.DrawContours(contours, -1, 255);

            var sorted_chars = from chr in chrs
                               orderby chr.y ascending
                               orderby chr.x ascending
                               select chr;

            using (new Window(cnt_img))
            using (new Window(img))
            {
                Cv2.WaitKey();
            }

        }
    }
}
