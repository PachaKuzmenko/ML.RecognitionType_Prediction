using Microsoft.ML.Data;

namespace RecognitionType_Prediction.DataStructures
{
    public class DemandObservation
    {
        [LoadColumn(1)]
        public float ID { get; set; }
        [LoadColumn(2)]
        public float YEAR { get; set; }
        [LoadColumn(3)]
        public float MONTH { get; set; }
        [LoadColumn(4)]
        public float DAY { get; set; }
        [LoadColumn(5)]
        public float HOUR { get; set; }
        [LoadColumn(6)]
        public float WEEKDAY { get; set; }
        [LoadColumn(7)]
        [ColumnName("Label")]
        public float COUNT { get; set; }
    }

    public static class DemandObservationSample
    {
        public static DemandObservation SingleDemandSampleData => new DemandObservation()
        {
            ID = 2,
            YEAR = 2019,
            MONTH = 1,
            DAY = 1,
            HOUR = 4,
            WEEKDAY = 3,
            COUNT = 4
        };
    }
}
