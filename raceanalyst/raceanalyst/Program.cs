using System;

namespace raceanalyst
{
    class Program
    {
        static void Main(string[] args)
        {
            string trainImage = args[0];

            string analyzeImage = args[1];

            Console.WriteLine($"Training image is {trainImage}");

            DataModel.GetDataModel().Train(trainImage);

            DataModel.GetDataModel().Analyze(analyzeImage);
        }
    }
}
