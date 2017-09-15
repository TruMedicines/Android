using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageMatchHost
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.ServiceModel;
    using OpenCVImageMatch;
    using System.IO;

    namespace ImageMatchHost
    {
        [ServiceContract]
        public interface IimageMatchService
        {
            [OperationContract]
            ImageMatchResult GetBestMatchingImage(ImageDetail referenceImage, List<ImageDetail> sampleImages, SettingsModel settings);

            [OperationContract]
            IEnumerable<ImageMatchResult> GetBestMatchingImages(ImageDetail referenceImage, List<ImageDetail> sampleImages, SettingsModel settings);
        }

        public class ImageMatchService : IimageMatchService
        {
            /// <summary>
            /// Get Best matching image from given list of images
            /// </summary>
            /// <param name="referenceImage"></param>
            /// <param name="sampleImages"></param>
            /// <param name="settings"></param>
            /// <returns></returns>
            public ImageMatchResult GetBestMatchingImage(ImageDetail referenceImage, List<ImageDetail> sampleImages, SettingsModel settings)
            {
                var cvMatch = new ImageMatch();
                return cvMatch.GetBestMatchingImage(referenceImage, sampleImages,settings);
            }
            /// <summary>
            /// Get Best matching images from given list of images
            /// </summary>
            /// <param name="referenceImage"></param>
            /// <param name="sampleImages"></param>
            /// <param name="settings"></param>
            /// <returns></returns>
            public IEnumerable<ImageMatchResult> GetBestMatchingImages(ImageDetail referenceImage, List<ImageDetail> sampleImages, SettingsModel settings)
            {
                var cvMatch = new ImageMatch();
                try
                {
                    return cvMatch.GetBestMatchingImages(referenceImage, sampleImages, settings);
                }
                catch(Exception mes)
                {
                    string text = "Message : " + mes.Message;
                    if (mes.StackTrace != "")
                        text += "Stacktrace : " + mes.StackTrace;
                    if (mes.InnerException != null)
                        text += "InnerException :" + mes.InnerException.Message;
                    File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory+@"\log.txt", text);
                    return new List<ImageMatchResult>();
                }

                
            }

            
        }
    }      

    
       
    
}
