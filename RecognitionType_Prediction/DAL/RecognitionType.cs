using System;

namespace RecognitionType_Prediction.DAL
{
    public class RecognitionType
    {
        public int RecognitionTypeID { get; set; }

        public string StatusCode { get; set; }

        public string RecognitionValueCode { get; set; }

        public DateTime RecCreatedDT { get; set; }
    }
}
