using OpenCVImageMatch;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {

            Program pg = new Program();
            try
            {
                pg.RunMultiImagesTest();
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine("Make sure you have added all required opencv references to bin folder");
            }



            Console.ReadKey();
        }

        public void RunSingleImageTest()
        {
            var cvMatch = new ImageMatch();
            var refernceImage = new ImageDetail
            {
                Id = 1,
                Path = @"C:\Users\Athul\Desktop\Kwid_Side.jpg"
            };
            var sampleImage = new ImageDetail
            {
                Id = 2,
                Path = @"C:\Users\Athul\Desktop\good.png"
            };
            var result = cvMatch.GetImageMatch(refernceImage, sampleImage);

            Console.WriteLine("Percentage : " + result.Percentage);

            Console.WriteLine("MatchedPointsCount : " + result.MatchedPointsCount);

            Console.WriteLine("MatchTime (Milli Seconds): " + result.MatchTime);

            Console.WriteLine("RefImage : " + result.RefImage.Path);

            Console.WriteLine("SampleImage : " + result.SampleImage.Path);

            Console.WriteLine("Any key to exit");
        }

        public void RunMultiImagesTest()
        {
            var cvMatch = new ImageMatch();
            var refernceImage = new ImageDetail
            {
                Id = 1,
                Path = @"C:\Users\Athul\Desktop\Kwid_Side.jpg"
            };

            var sampleImages = new List<ImageDetail>
            {
                new ImageDetail
                {
                  Id = 2,
                  Path = @"C:\Users\Athul\Desktop\kwidsmall.jpg"
                },
                //new ImageDetail
                //{
                //  Id = 2,
                //  Path = @"C:\Users\Subin\Desktop\Inside.jpg"
                //},
                //new ImageDetail
                //{
                //  Id = 2,
                //  Path = @"C:\Users\Subin\Desktop\KwidInside.jpg"
                //},
                //new ImageDetail
                //{
                //  Id = 2,
                //  Path = @"C:\Users\Subin\Desktop\FigoInside.jpg"
                //},
                //new ImageDetail
                //{
                //  Id = 2,
                //  Path = @"C:\Users\Subin\Desktop\v-test.jpg"
                //},
                //new ImageDetail
                //{
                //  Id = 2,
                //  Path = @"C:\Users\Subin\Desktop\MatchOutSide.jpg"
                //},
                //new ImageDetail
                //{
                //  Id = 2,
                //  Path = @"C:\Users\Subin\Desktop\KeyInside.jpg"
                //},


            };
            
            
            var result = cvMatch.GetBestMatchingImage(refernceImage, sampleImages, new SettingsModel()
            {
                Rotate = true,
                ChannelsCount = 2,
                HessianThresh = 1500,
                UniquenessThreshold = 0.8
            });

            Console.WriteLine("Best Percentage : " + result.Percentage);

            Console.WriteLine("MatchedPointsCount : " + result.MatchedPointsCount);

            Console.WriteLine("MatchTime (Milli Seconds): " + result.MatchTime);

            Console.WriteLine("RefImage : " + result.RefImage.Path);

            Console.WriteLine("SampleImage : " + result.SampleImage.Path);

            Console.WriteLine("Any key to exit");
        }
    }
}
