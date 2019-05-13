using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.ML;
using RecognitionType_Prediction.DataStructures;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;

namespace RecognitionType_Prediction
{
    class Program
    {
        private static readonly string ConnectionString;
        private const string Takeda = "TakedaRecognition";
        private static string ModelsLocation = @"../../../../MLModels";

        static Program()
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("config.json", true, true)
                .Build();
            ConnectionString = config.GetConnectionString("STG");
        }

        static IEnumerable<DemandObservation> GetTrainingData()
        {
            using (IDbConnection connection = new SqlConnection(ConnectionString))
            {
                var getRT = $@"USE {Takeda}
                            SELECT	ur.RecognitionTypeID AS ID,
		                            YEAR(ur.RecCreatedDT) AS YEAR,
		                            MONTH(ur.RecCreatedDT) AS MONTH,
		                            DAY(ur.RecCreatedDT) AS DAY,
		                            DATEPART(WEEKDAY, ur.RecCreatedDT) AS WEEKDAY,
		                            DATEPART(HOUR, ur.RecCreatedDT) AS HOUR,
		                            COUNT(ur.RecognitionTypeID) AS COUNT		
                            FROM Recognition.UserRecognition AS ur  
                            INNER JOIN [Recognition].[RecognitionType] AS rt 
                            ON rt.RecognitionTypeID = ur.RecognitionTypeID
                            WHERE rt.RecognitionValueCode in ('M', 'B') AND ur.UserRecognitionStatusCode = 'Executed'
                            GROUP BY	ur.RecognitionTypeID, 
			                            YEAR(ur.RecCreatedDT), 
			                            MONTH(ur.RecCreatedDT), 
			                            DAY(ur.RecCreatedDT), 
			                            DATEPART(WEEKDAY, ur.RecCreatedDT), 
			                            DATEPART(HOUR, ur.RecCreatedDT)
                            ORDER BY DAY, MONTH, YEAR DESC";

                var result = connection.Query<DemandObservation>(getRT);
                Console.WriteLine($"Training data count: {result.Count()}");
                return result;
            }
        }

        static void Main(string[] args)
        {
            // Create MLContext to be shared across the model creation workflow objects 
            // Set a random seed for repeatable/deterministic results across multiple trainings.
            var mlContext = new MLContext(seed: 0);

            // 1. Common data loading configuration
            //var trainingDataView = mlContext.Data.LoadFromTextFile<DemandObservation>(path: TrainingDataLocation, hasHeader: true, separatorChar: ',');
            //var testDataView = mlContext.Data.LoadFromTextFile<DemandObservation>(path: TestDataLocation, hasHeader: true, separatorChar: ',');

            var trainingData = GetTrainingData();
            var trainingDataView = mlContext.Data.LoadFromEnumerable(trainingData);
            var testDataView = mlContext.Data.LoadFromEnumerable(trainingData);

            // Concatenate all the numeric columns into a single features column
            var dataProcessPipeline = mlContext.Transforms.Concatenate("Features",
                                                     nameof(DemandObservation.YEAR), nameof(DemandObservation.MONTH), 
                                                     nameof(DemandObservation.HOUR), nameof(DemandObservation.DAY), 
                                                     nameof(DemandObservation.ID), nameof(DemandObservation.WEEKDAY))
                                                     .AppendCacheCheckpoint(mlContext);
            // Use in-memory cache for small/medium datasets to lower training time. 
            // Do NOT use it (remove .AppendCacheCheckpoint()) when handling very large datasets.

            // (Optional) Peek data in training DataView after applying the ProcessPipeline's transformations  
            Common.ConsoleHelper.PeekDataViewInConsole(mlContext, trainingDataView, dataProcessPipeline, 10);
            Common.ConsoleHelper.PeekVectorColumnDataInConsole(mlContext, "Features", trainingDataView, dataProcessPipeline, 10);

            // Definition of regression trainers/algorithms to use
            //var regressionLearners = new (string name, IEstimator<ITransformer> value)[]
            (string name, IEstimator<ITransformer> value)[] regressionLearners =
            {
                ("FastTree", mlContext.Regression.Trainers.FastTree()),
                ("Poisson", mlContext.Regression.Trainers.LbfgsPoissonRegression()),
                ("SDCA", mlContext.Regression.Trainers.Sdca()),
                ("FastTreeTweedie", mlContext.Regression.Trainers.FastTreeTweedie()),
                //Other possible learners that could be included
                //...FastForestRegressor...
                //...GeneralizedAdditiveModelRegressor...
                //...OnlineGradientDescent... (Might need to normalize the features first)
            };

            // 3. Phase for Training, Evaluation and model file persistence
            // Per each regression trainer: Train, Evaluate, and Save a different model
            foreach (var trainer in regressionLearners)
            {
                Console.WriteLine("=============== Training the current model ===============");
                var trainingPipeline = dataProcessPipeline.Append(trainer.value);
                var trainedModel = trainingPipeline.Fit(trainingDataView);

                Console.WriteLine("===== Evaluating Model's accuracy with Test data =====");
                IDataView predictions = trainedModel.Transform(trainingDataView);
                var metrics = mlContext.Regression.Evaluate(data: predictions, labelColumnName: "Label", scoreColumnName: "Score");
                Common.ConsoleHelper.PrintRegressionMetrics(trainer.value.ToString(), metrics);

                //Save the model file that can be used by any application
                string modelRelativeLocation = $"{ModelsLocation}/{trainer.name}Model.zip";
                string modelPath = GetAbsolutePath(modelRelativeLocation);
                mlContext.Model.Save(trainedModel, trainingDataView.Schema, modelPath);
                Console.WriteLine("The model is saved to {0}", modelPath);
            }

            // 4. Try/test Predictions with the created models
            // The following test predictions could be implemented/deployed in a different application (production apps)
            // that's why it is seggregated from the previous loop
            // For each trained model, test 10 predictions           
            foreach (var learner in regressionLearners)
            {
                //Load current model from .ZIP file
                string modelRelativeLocation = $"{ModelsLocation}/{learner.name}Model.zip";
                string modelPath = GetAbsolutePath(modelRelativeLocation);
                ITransformer trainedModel = mlContext.Model.Load(modelPath, out var modelInputSchema);

                // Create prediction engine related to the loaded trained model
                var predEngine = mlContext.Model.CreatePredictionEngine<DemandObservation, DemandPrediction>(trainedModel);

                Console.WriteLine($"================== Visualize/test 10 predictions for model {learner.name}Model.zip ==================");
                //Visualize 10 tests comparing prediction with actual/observed values from the test dataset
                ModelScoringTester.VisualizeSomePredictions(mlContext, learner.name, trainingData.ToList(), predEngine, 10);
            }

            Common.ConsoleHelper.ConsolePressAnyKey();
        }

        public static string GetAbsolutePath(string relativePath)
        {
            FileInfo _dataRoot = new FileInfo(typeof(Program).Assembly.Location);
            string assemblyFolderPath = _dataRoot.Directory.FullName;

            string fullPath = Path.Combine(assemblyFolderPath, relativePath);

            return fullPath;
        }
    }
}
