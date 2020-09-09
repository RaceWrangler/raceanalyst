using ExifLib;
using Grpc.Core;
using raceanalyst.Data.Models;
using System;
using System.IO;
using System.Threading.Tasks;


namespace raceanalyst
{
    public  class LineCrossingAnalyzer : AnalyzeNewImage.AnalyzeNewImageBase
    {
        private readonly ApplicationDbContext _context;
        public Func<string, bool> AnalyzeFunction { get; set; }

        public LineCrossingAnalyzer(ApplicationDbContext context)
        {
            _context = context;
        }

        public override Task<NewImageResponse> AnalyzeNewImage(NewImageRequest nir, ServerCallContext context)
        {
            AnalyzeFunction(nir.ImageName);
            return Task.FromResult(new NewImageResponse());
        }

        // This function takes an un-analyzed image and pushes it to the database without any values.
        // Race Control will have to set the values for class and number manually.
        internal bool PublishImage(string imgName)
        {
            return PublishImage(imgName, "", "");
        }

        internal bool PublishImage(string imgName, string number, string cls)
        {
            string tmstmp;
            using (ExifReader er = new ExifReader(imgName))
            {
                bool success = er.GetTagValue(ExifTags.SubsecTime, out tmstmp);
                if(success)
                {
                    LineCrossing lc = new LineCrossing
                    {
                        FileName = imgName,
                        ClassName = cls,
                        Number = number,
                        TimeStamp = DateTime.Parse(tmstmp)
                    };

                    _context.LineCrossings.AddAsync(lc);

                    _context.SaveChangesAsync();

                }

            }

            return true;
        }


        // The following methods are basic stubs that do not work reliably yet.
        internal bool AnalyzeByNumbers(string imgName)
        {
            DataModel.GetDataModel().Analyze(imgName);

            return true;
        }

        internal bool AnalyzeByQRCode(string imgName)
        {
            QRDecoder qrd = new QRDecoder();

            string text = qrd.DecodeImg(imgName);

            if ("" != text)
            {
                Console.WriteLine($"The QR code detected says: {text}");
            }
            else
            {
                Console.WriteLine("No QR Code was detected!");
            }

            return true;
        }
    }
}