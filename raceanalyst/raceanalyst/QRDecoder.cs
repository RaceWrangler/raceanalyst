using System;
using System.Collections.Generic;
using System.Text;
using OpenCvSharp;
using OpenCvSharp.Extensions;

namespace raceanalyst
{
    class Record
    {
        public string Number { get; set; }
        public string Class { get; set; }
        public DateTime Timestamp { get; set; }
    }

    class QRDecoder
    {
        public QRDecoder() { }

        private string OpenCVDecode(Mat img)
        {
            var decoder = new QRCodeDetector();
            string rec = "";

            Point2f[] points;

            if (decoder.Detect(img, out points))
            {
                Console.WriteLine("QR Code detected!");
                rec = decoder.Decode(img, points);

                var pointList = new List<List<Point>>();
                pointList.Add(new List<Point>());
                foreach (var pnt in points)
                {
                    pointList[0].Add(new Point(pnt.X, pnt.Y));
                }

                img.Polylines(pointList, true, 255, 2);
            }

            return rec;
        }

        //private string ZBarDecode(Mat img)
        //{
        //    string rec = "";

        //    using (var scanner = new ImageScanner { Cache = false })
        //    {
        //        var scanned = scanner.Scan(img.ToBitmap());

        //        foreach(var sym in scanned)
        //        {
        //            var data = sym?.Data ?? string.Empty;
        //            Console.WriteLine("QR Code detected!");

        //            rec += data;
        //        }
        //    }

        //    return rec;
        //}

        public string DecodeImg(string imgName)
        {
            Mat img = new Mat(imgName, ImreadModes.Grayscale);

            Cv2.Threshold(img, img, 127, 255, ThresholdTypes.Binary);

            if(img.Empty())
            {
                Console.WriteLine($"The image {imgName} does not exist!");
                return "";
            }



            //Cv2.Resize(img, img, new Size(/*640, 480)); // */img.Width / 2, img.Height / 2));
            string rec = OpenCVDecode(img);

            //string rec = ZBarDecode(img);

            using (new Window(img))
            {
                Cv2.WaitKey();
            }

                return rec;
        }

        
    }
}
