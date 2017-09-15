using Emgu.CV;
using Emgu.CV.Structure;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace OpenCVImageMatch
{
    public class ImageMatch
    {
        /// <summary>
        /// Get Image match result (in percentage) for the given 2 images
        /// </summary>
        /// <param name="referenceImage"></param>
        /// <param name="sampleImage"></param>
        /// <returns></returns>
        public ImageMatchResult GetImageMatch(ImageDetail referenceImage, ImageDetail sampleImage)
        {
            using (Image<Gray, byte> modelImage = new Image<Gray, byte>(sampleImage.Path))
            using (Image<Gray, byte> observedImage = new Image<Gray, byte>(referenceImage.Path))
            {
                var matchResult = OpenCvMatch.FindMatch(observedImage, modelImage);
                matchResult.SampleImage = sampleImage;
                matchResult.RefImage = referenceImage;
                return matchResult;
            };
        }
        /// <summary>
        /// Get the image mact result for the given list of images with respect to the given image
        /// </summary>
        /// <param name="referenceImage"></param>
        /// <param name="sampleImages"></param>
        /// <param name="settings"></param>
        /// <returns></returns>

        public ImageMatchResult GetBestMatchingImage(ImageDetail referenceImage, List<ImageDetail> sampleImages, SettingsModel settings)
        {
            var modelImages = sampleImages.Select(image=> new ModelImage { Data = new Image<Gray, byte>(image.Path), Image=image });
            using (Image<Gray, byte> observedImage = new Image<Gray, byte>(referenceImage.Path))
            {
                long completeExecutionTime;
                var matchResults = OpenCvMatch.FindMatches(observedImage, modelImages, settings, out completeExecutionTime);
                var bestMatchingResult = matchResults.OrderByDescending(match => match.Percentage).FirstOrDefault();//Finding max.
                bestMatchingResult.RefImage = referenceImage;
                bestMatchingResult.MatchTime = completeExecutionTime;
                return bestMatchingResult;
            };
            
        }
        /// <summary>
        /// Returns best macthing images
        /// </summary>
        /// <param name="referenceImage"></param>
        /// <param name="sampleImages"></param>
        /// <param name="settings"></param>
        /// <returns></returns>
        public IEnumerable<ImageMatchResult> GetBestMatchingImages(ImageDetail referenceImage, List<ImageDetail> sampleImages,SettingsModel settings)
        {
            //WriiteLog("Starting GetBestMatchingImages");
                 var rotatedImagePaths = GetRotatedImagePaths(referenceImage.Path, settings);
            //WriiteLog("1. rotatedImagePaths referenceImage.Path " + referenceImage.Path);
            var matchResults = new List<ImageMatchResult>();
            var modelImages = new List<ImageDetail>();
            foreach (var sampleImage in sampleImages)
            {
                if (sampleImage != null && sampleImage.Path != null)
                {
                    if (File.Exists(sampleImage.Path))
                        modelImages.Add(sampleImage);
                }
            }
             var modelImagesFiltered = modelImages.Select(image =>  new ModelImage { Data = new Image<Gray, byte>(image.Path), Image = image });
            
           // WriiteLog("modelImages image.Path");
            foreach(var path in rotatedImagePaths)
            {
                if(File.Exists(path))
                using (Image<Gray, byte> observedImage = new Image<Gray, byte>(path))
                {
                    try
                    {
                        long completeExecutionTime;
                        matchResults.AddRange(OpenCvMatch.FindMatches(observedImage, modelImagesFiltered, settings, out completeExecutionTime));
                    }
                    catch(Exception ex)
                    {
                        WriiteLog(ex.Message);
                    }
                };
            }
            //Parallel.ForEach(rotatedImagePaths, path => {
                
            //});
            //WriiteLog("get result from matchResults using for each");
            var bestMatchedResults = new  List<ImageMatchResult>();
            matchResults.GroupBy(match => match.SampleImage.Id).ToList()
                .ForEach(result => 
                {
                    var bestMatch = result.OrderByDescending(r => r.Percentage).FirstOrDefault();
                    bestMatchedResults.Add(bestMatch);
                });
            //WriiteLog("matchResults GroupBy query executed ");
            //WriiteLog("End ");
            return bestMatchedResults;
        }
        /// <summary>
        /// returns path of given image after rotation
        /// </summary>
        /// <param name="imagePath"></param>
        /// <param name="settings"></param>
        /// <returns></returns>
        private List<string> GetRotatedImagePaths(string imagePath, SettingsModel settings)
        {
            var imagePathsRotated = new List<string> { imagePath };
            if (settings.Rotate)
            {
                var imageToRotate = new Bitmap(imagePath);

                var rotateAngle = (settings.RotateDegree > 5 ? settings.RotateDegree : 90);
                while (rotateAngle < 360)
                {
                    var directoryPath = Path.GetDirectoryName(imagePath);
                    var newPath = Path.Combine(directoryPath, Guid.NewGuid().ToString());
                    var rotatedImage = RotateImage(imageToRotate, rotateAngle);
                    rotatedImage.Save(newPath);
                    imagePathsRotated.Add(newPath);
                    rotateAngle += (settings.RotateDegree > 5 ? settings.RotateDegree : 90);
                }
            }
            return imagePathsRotated;
        }
        private Bitmap RotateImage(Bitmap bmp, float angle)
        {
            Bitmap rotatedImage = new Bitmap(bmp.Width, bmp.Height);
            using (Graphics g = Graphics.FromImage(rotatedImage))
            {
                g.TranslateTransform(bmp.Width / 2, bmp.Height / 2); //set the rotation point as the center into the matrix
                g.RotateTransform(angle); //rotate
                g.TranslateTransform(-bmp.Width / 2, -bmp.Height / 2); //restore rotation point into the matrix
                g.DrawImage(bmp, new Point(0, 0)); //draw the image on the new bitmap
            }

            return rotatedImage;
        }
        /// <summary>
        /// writes log to text file
        /// </summary>
        /// <param name="message"></param>
        public void WriiteLog(string message)
        {
            File.AppendAllText(AppDomain.CurrentDomain.BaseDirectory + @"\log1.txt", "\n"+message);

        }
    }

}
