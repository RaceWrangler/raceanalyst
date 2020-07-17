using System;

namespace raceanalyst
{
    class Program
    {
        static void Main(string[] args)
        {
            if ("--train" == args[0])
            {
                string trainImage = args[1];

                Console.WriteLine($"Training image is {trainImage}");

                DataModel.GetDataModel().Train(trainImage);
            }
            else if ("--num" == args[0])
            {
                string analyzeImage = args[1];
                DataModel.GetDataModel().Analyze(analyzeImage);
            }
            else if ("--qr" == args[0])
            {
                string qrImage = args[1];
                QRDecoder qrd = new QRDecoder();

                string text = qrd.DecodeImg(qrImage);

                Console.WriteLine($"The QR code detected says: {text}");
            }
        }
    }
}
