namespace RecognitionType_Prediction.DAL
{
    public class TrainingData
    {
        public int ID { get; set; }
        public int YEAR { get; set; }
        public int MONTH { get; set; }
        public int DAY { get; set; }
        public int HOUR { get; set; }
        public string WEEKDAY { get; set; }
        public int COUNT { get; set; }
    }
}
