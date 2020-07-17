using System;
using System.Collections.Generic;
using System.Text;
using OpenCvSharp;

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

        public string DecodeImg(string imgName)
        {
            string rec = ""; // = new Record();

            Mat img = new Mat(imgName, ImreadModes.Grayscale);

            if(img.Empty())
            {
                Console.WriteLine($"The image {imgName} does not exist!");
                return "";
            }

            //Cv2.Resize(img, img, new Size(/*640, 480)); // */img.Width / 2, img.Height / 2));

            var decoder = new QRCodeDetector();

            Point2f[] points;

            if(decoder.Detect(img, out points))
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

            using (new Window(img))
            {
                Cv2.WaitKey();
            }

                return rec;
        }
    }
}
