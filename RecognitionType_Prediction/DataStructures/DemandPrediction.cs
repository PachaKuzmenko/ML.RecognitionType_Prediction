using Microsoft.ML.Data;

namespace RecognitionType_Prediction.DataStructures
{
    public class DemandPrediction
    {
        [ColumnName("Score")]
        public float PredictedCount;
        [ColumnName("ID")]
        public float RecognitionTypeID { get; set; }
    }
}
