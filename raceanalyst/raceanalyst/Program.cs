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
            else
            {
                string analyzeImage = args[0];
                DataModel.GetDataModel().Analyze(analyzeImage);
            }
        }
    }
}
