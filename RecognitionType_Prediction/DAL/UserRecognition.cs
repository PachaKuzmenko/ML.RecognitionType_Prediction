using System;

namespace RecognitionType_Prediction.DAL
{
    public class UserRecognition
    {
        public int UserRecognitionID { get; set; }
        public int RecognitionTypeID { get; set; }
        public string UserRecognitionStatusCode { get; set; }
        public DateTime RecCreatedDT { get; set; }
    }
}
