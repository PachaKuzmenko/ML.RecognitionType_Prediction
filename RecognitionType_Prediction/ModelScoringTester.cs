using Microsoft.ML;
using RecognitionType_Prediction.DataStructures;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace RecognitionType_Prediction
{
    public static class ModelScoringTester
    {
        public static void VisualizeSomePredictions(MLContext mlContext,
                                                    string modelName,
                                                    //string testDataLocation,
                                                    List<DemandObservation> testData,
                                                    PredictionEngine<DemandObservation, DemandPrediction> predEngine,
                                                    int numberOfPredictions)
        {
            //Make a few prediction tests 
            // Make the provided number of predictions and compare with observed data from the test dataset
            //var testData = ReadSampleDataFromCsvFile(testDataLocation, numberOfPredictions);

            for (int i = 0; i < numberOfPredictions; i++)
            {
                //Score
                var resultprediction = predEngine.Predict(testData[i]);

                Common.ConsoleHelper.PrintRegressionPredictionVersusObserved(resultprediction.PredictedCount.ToString(), 
                    resultprediction.RecognitionTypeID.ToString(), 
                    testData[i].COUNT.ToString(),
                    testData[i].ID.ToString());
            }

        }

        //This method is using regular .NET System.IO.File and LinQ to read just some sample data to test/predict with 
        public static List<DemandObservation> ReadSampleDataFromCsvFile(string dataLocation, int numberOfRecordsToRead)
        {
            return File.ReadLines(dataLocation)
                .Skip(1)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Split(','))
                .Select(x => new DemandObservation()
                {
                    ID = float.Parse(x[1], CultureInfo.InvariantCulture),
                    YEAR = float.Parse(x[2], CultureInfo.InvariantCulture),
                    MONTH = float.Parse(x[3], CultureInfo.InvariantCulture),
                    DAY = float.Parse(x[4], CultureInfo.InvariantCulture),
                    HOUR = float.Parse(x[5], CultureInfo.InvariantCulture),
                    WEEKDAY = float.Parse(x[6], CultureInfo.InvariantCulture),
                    COUNT = float.Parse(x[7], CultureInfo.InvariantCulture)
                })
                .Take(numberOfRecordsToRead)
                .ToList();
        }
    }
}
