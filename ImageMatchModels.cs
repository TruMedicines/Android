using Emgu.CV;
using Emgu.CV.Structure;

namespace OpenCVImageMatch
{
    public class ImageMatchResult
    {
        public double Percentage { get; set; }

        public int MatchedPointsCount { get; set; }

        public int TotalPoints { get; set; }

        public double MatchTime { get; set; }

        public ImageDetail SampleImage {get;set;}

        public ImageDetail RefImage { get; set; }
    }


    public class ImageDetail
    {
        public string Path { get; set; }

        public int Id { get; set; }
    }

    public class ModelImage
    {
        public Image<Gray, byte> Data { get; set; }

        public ImageDetail Image { get; set; }
    }
}
