//----------------------------------------------------------------------------
//  Copyright (C) 2004-2013 by EMGU. All rights reserved.       
//----------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Features2D;

using Emgu.CV.Structure;
using Emgu.CV.Util;
using System.Threading.Tasks;
#if !IOS
using Emgu.CV.GPU;
#endif

namespace OpenCVImageMatch
{
    internal static class OpenCvMatch
    {
        /// <summary>
        /// Find matching metween two images
        /// </summary>
        /// <param name="observedImage"></param>
        /// <param name="modelImage"></param>
        /// <returns></returns>
        internal static ImageMatchResult FindMatch(Image<Gray, byte> observedImage, Image<Gray, byte> modelImage)
        {
            long matchTime; int nonofZeroCount; int totalPoints;
            int returnValue = 0;

            Stopwatch watch;
            HomographyMatrix homography = null;

            SURFDetector surfCPU = new SURFDetector(1500, true);
            VectorOfKeyPoint modelKeyPoints;
            VectorOfKeyPoint observedKeyPoints;
            Matrix<int> indices;

            Matrix<byte> mask;
            int k = 2;
            double uniquenessThreshold = 0.8;

            if (GpuInvoke.HasCuda)
            {
                GpuSURFDetector surfGPU = new GpuSURFDetector(surfCPU.SURFParams, 0.01f);
                using (GpuImage<Gray, Byte> gpuModelImage = new GpuImage<Gray, byte>(modelImage))
                //extract features from the object image
                using (GpuMat<float> gpuModelKeyPoints = surfGPU.DetectKeyPointsRaw(gpuModelImage, null))
                using (GpuMat<float> gpuModelDescriptors = surfGPU.ComputeDescriptorsRaw(gpuModelImage, null, gpuModelKeyPoints))
                using (GpuBruteForceMatcher<float> matcher = new GpuBruteForceMatcher<float>(DistanceType.L2))
                {
                    modelKeyPoints = new VectorOfKeyPoint();
                    surfGPU.DownloadKeypoints(gpuModelKeyPoints, modelKeyPoints);
                    watch = Stopwatch.StartNew();
                    
                    // extract features from the observed image
                    using (GpuImage<Gray, Byte> gpuObservedImage = new GpuImage<Gray, byte>(observedImage))
                    using (GpuMat<float> gpuObservedKeyPoints = surfGPU.DetectKeyPointsRaw(gpuObservedImage, null))
                    using (GpuMat<float> gpuObservedDescriptors = surfGPU.ComputeDescriptorsRaw(gpuObservedImage, null, gpuObservedKeyPoints))
                    using (GpuMat<int> gpuMatchIndices = new GpuMat<int>(gpuObservedDescriptors.Size.Height, k, 1, true))
                    using (GpuMat<float> gpuMatchDist = new GpuMat<float>(gpuObservedDescriptors.Size.Height, k, 1, true))
                    using (GpuMat<Byte> gpuMask = new GpuMat<byte>(gpuMatchIndices.Size.Height, 1, 1))
                    using (Stream stream = new Stream())
                    {
                        matcher.KnnMatchSingle(gpuObservedDescriptors, gpuModelDescriptors, gpuMatchIndices, gpuMatchDist, k, null, stream);
                        indices = new Matrix<int>(gpuMatchIndices.Size);
                        mask = new Matrix<byte>(gpuMask.Size);

                        //gpu implementation of voteForUniquess
                        using (GpuMat<float> col0 = gpuMatchDist.Col(0))
                        using (GpuMat<float> col1 = gpuMatchDist.Col(1))
                        {
                            GpuInvoke.Multiply(col1, new MCvScalar(uniquenessThreshold), col1, stream);
                            GpuInvoke.Compare(col0, col1, gpuMask, CMP_TYPE.CV_CMP_LE, stream);
                        }

                        observedKeyPoints = new VectorOfKeyPoint();
                        surfGPU.DownloadKeypoints(gpuObservedKeyPoints, observedKeyPoints);

                        //wait for the stream to complete its tasks
                        //We can perform some other CPU intesive stuffs here while we are waiting for the stream to complete.
                        stream.WaitForCompletion();

                        gpuMask.Download(mask);
                        gpuMatchIndices.Download(indices);

                        if (GpuInvoke.CountNonZero(gpuMask) >= 4)
                        {
                            int nonZeroCount = Features2DToolbox.VoteForSizeAndOrientation(modelKeyPoints, observedKeyPoints, indices, mask, 1.5, 20);
                            if (nonZeroCount >= 4)
                                homography = Features2DToolbox.GetHomographyMatrixFromMatchedFeatures(modelKeyPoints, observedKeyPoints, indices, mask, 2);

                            returnValue = nonZeroCount;
                        }

                        watch.Stop();
                    }
                }
            }
            else
            {
                //extract features from the object image
                modelKeyPoints = surfCPU.DetectKeyPointsRaw(modelImage, null);
                Matrix<float> modelDescriptors = surfCPU.ComputeDescriptorsRaw(modelImage, null, modelKeyPoints);

                watch = Stopwatch.StartNew();

                // extract features from the observed image
                observedKeyPoints = surfCPU.DetectKeyPointsRaw(observedImage, null);
                Matrix<float> observedDescriptors = surfCPU.ComputeDescriptorsRaw(observedImage, null, observedKeyPoints);
                BruteForceMatcher<float> matcher = new BruteForceMatcher<float>(DistanceType.L2);
                matcher.Add(modelDescriptors);

                indices = new Matrix<int>(observedDescriptors.Rows, k);
                using (Matrix<float> dist = new Matrix<float>(observedDescriptors.Rows, k))
                {
                    matcher.KnnMatch(observedDescriptors, indices, dist, k, null);
                    mask = new Matrix<byte>(dist.Rows, 1);
                    mask.SetValue(255);
                    Features2DToolbox.VoteForUniqueness(dist, uniquenessThreshold, mask);
                }

                int nonZeroCount = CvInvoke.cvCountNonZero(mask);
                if (nonZeroCount >= 4)
                {
                    nonZeroCount = Features2DToolbox.VoteForSizeAndOrientation(modelKeyPoints, observedKeyPoints, indices, mask, 1.5, 20);
                    if (nonZeroCount >= 4)
                        homography = Features2DToolbox.GetHomographyMatrixFromMatchedFeatures(modelKeyPoints, observedKeyPoints, indices, mask, 2);
                }

                returnValue = nonZeroCount;
                watch.Stop();
            }
            int p = 0;
            var maskEnumerator = mask.ManagedArray.GetEnumerator();
            while ((maskEnumerator.MoveNext()) && (maskEnumerator.Current != null))
            {
                if (maskEnumerator.Current.GetType().Equals(typeof(byte)))
                {
                    var thisVal = (byte)maskEnumerator.Current;
                    if (thisVal == 1) p++;
                }
            }

            totalPoints = modelKeyPoints.Size;

            matchTime = watch.ElapsedMilliseconds;

            nonofZeroCount = returnValue;

            return new ImageMatchResult()
            {
                Percentage = (nonofZeroCount * 100 / (double)totalPoints),
                MatchTime = matchTime,
                MatchedPointsCount = nonofZeroCount,
                TotalPoints = totalPoints
            };
        }
        /// <summary>
        /// Find image match between a list of images
        /// </summary>
        /// <param name="observedImage"></param>
        /// <param name="modelImages"></param>
        /// <param name="settings"></param>
        /// <param name="totalExecutionTime"></param>
        /// <returns></returns>
        internal static IEnumerable<ImageMatchResult> FindMatches(Image<Gray, byte> observedImage, IEnumerable<ModelImage> modelImages,SettingsModel settings, out long totalExecutionTime)
        {
            
            IList<ImageMatchResult> _result = new List<ImageMatchResult>();
            _result.Add(null);
            try
            {
                Stopwatch watchComplete;
                var surfCPU = new SURFDetector(settings.HessianThresh, false);
                int k = settings.ChannelsCount;
                double uniquenessThreshold = settings.UniquenessThreshold;

                var imageMatchResults = new List<ImageMatchResult>();
                watchComplete = Stopwatch.StartNew();
                if (GpuInvoke.HasCuda)
                {
                    GpuSURFDetector surfGPU = new GpuSURFDetector(surfCPU.SURFParams, 0.01f);
                    using (GpuImage<Gray, byte> gpuObservedImage = new GpuImage<Gray, byte>(observedImage))
                    //extract features from the object image
                    using (GpuMat<float> gpuObservedKeyPoints = surfGPU.DetectKeyPointsRaw(gpuObservedImage, null))
                    using (GpuMat<float> gpuObservedDescriptors = surfGPU.ComputeDescriptorsRaw(gpuObservedImage, null, gpuObservedKeyPoints))
                    using (GpuBruteForceMatcher<float> matcher = new GpuBruteForceMatcher<float>(DistanceType.L2))
                    {
                        var observedKeyPoints = new VectorOfKeyPoint();
                        surfGPU.DownloadKeypoints(gpuObservedKeyPoints, observedKeyPoints);

                        //foreach(var modelImage in modelImages)
                        Parallel.ForEach(modelImages, modelImage =>
                        {
                            var watch = Stopwatch.StartNew();
                            HomographyMatrix homography = null;
                            VectorOfKeyPoint modelKeyPoints;
                            Matrix<int> indices;

                            Matrix<byte> mask;
                            long matchTime; int nonofZeroCount; int totalPoints;
                            int returnValue = 0;
                        // extract features from the observed image
                        using (GpuImage<Gray, byte> gpuModelImage = new GpuImage<Gray, byte>(modelImage.Data))
                            using (GpuMat<float> gpuModelKeyPoints = surfGPU.DetectKeyPointsRaw(gpuModelImage, null))
                            using (GpuMat<float> gpuModelDescriptors = surfGPU.ComputeDescriptorsRaw(gpuModelImage, null, gpuModelKeyPoints))
                            using (GpuMat<int> gpuMatchIndices = new GpuMat<int>(gpuObservedDescriptors.Size.Height, k, 1, true))
                            using (GpuMat<float> gpuMatchDist = new GpuMat<float>(gpuObservedDescriptors.Size.Height, k, 1, true))
                            using (GpuMat<Byte> gpuMask = new GpuMat<byte>(gpuMatchIndices.Size.Height, 1, 1))
                            using (Stream stream = new Stream())
                            {
                                matcher.KnnMatchSingle(gpuObservedDescriptors, gpuModelDescriptors, gpuMatchIndices, gpuMatchDist, k, null, stream);
                                indices = new Matrix<int>(gpuMatchIndices.Size);
                                mask = new Matrix<byte>(gpuMask.Size);

                            //gpu implementation of voteForUniquess
                            using (GpuMat<float> col0 = gpuMatchDist.Col(0))
                                using (GpuMat<float> col1 = gpuMatchDist.Col(1))
                                {
                                    GpuInvoke.Multiply(col1, new MCvScalar(uniquenessThreshold), col1, stream);
                                    GpuInvoke.Compare(col0, col1, gpuMask, CMP_TYPE.CV_CMP_LE, stream);
                                }

                                modelKeyPoints = new VectorOfKeyPoint();
                                surfGPU.DownloadKeypoints(gpuModelKeyPoints, modelKeyPoints);

                            //wait for the stream to complete its tasks
                            //We can perform some other CPU intesive stuffs here while we are waiting for the stream to complete.
                            stream.WaitForCompletion();

                                gpuMask.Download(mask);
                                gpuMatchIndices.Download(indices);

                                if (GpuInvoke.CountNonZero(gpuMask) >= 4)
                                {
                                    int nonZeroCount = Features2DToolbox.VoteForSizeAndOrientation(modelKeyPoints, observedKeyPoints, indices, mask, 1.5, 20);
                                    if (nonZeroCount >= 4)
                                        homography = Features2DToolbox.GetHomographyMatrixFromMatchedFeatures(modelKeyPoints, observedKeyPoints, indices, mask, 2);

                                    returnValue = nonZeroCount;
                                }

                                watch.Stop();
                            }
                            int p = 0;
                            var maskEnumerator = mask.ManagedArray.GetEnumerator();
                            while ((maskEnumerator.MoveNext()) && (maskEnumerator.Current != null))
                            {
                                if (maskEnumerator.Current.GetType().Equals(typeof(byte)))
                                {
                                    var thisVal = (byte)maskEnumerator.Current;
                                    if (thisVal == 1) p++;
                                }
                            }

                            totalPoints = observedKeyPoints.Size;

                            matchTime = watch.ElapsedMilliseconds;

                            nonofZeroCount = returnValue;

                            imageMatchResults.Add(new ImageMatchResult()
                            {
                                Percentage = (p * 100 / (double)totalPoints),
                                MatchTime = matchTime,
                                MatchedPointsCount = p,
                                TotalPoints = totalPoints,
                                SampleImage = modelImage.Image
                            });
                        }
                        );
                    }
                }
                else
                {

                    //extract features from the object image
                    var observedKeyPoints = surfCPU.DetectKeyPointsRaw(observedImage, null);
                    Matrix<float> observedDescriptors = surfCPU.ComputeDescriptorsRaw(observedImage, null, observedKeyPoints);
                    //foreach (var modelImage in modelImages)
                    Parallel.ForEach(modelImages, modelImage =>
                    {
                        HomographyMatrix homography = null;
                        VectorOfKeyPoint modelKeyPoints;
                        Matrix<int> indices;

                        Matrix<byte> mask;
                        long matchTime; int nonofZeroCount; int totalPoints = 0;
                        int returnValue = 0;
                        var watch = Stopwatch.StartNew();

                        //extract features from the observed image
                        modelKeyPoints = surfCPU.DetectKeyPointsRaw(modelImage.Data, null);
                        Matrix<float> modelDescriptors = surfCPU.ComputeDescriptorsRaw(modelImage.Data, null, modelKeyPoints);
                        BruteForceMatcher<float> matcher = new BruteForceMatcher<float>(DistanceType.L2);
                        if (observedDescriptors != null && modelDescriptors != null)
                        {
                            matcher.Add(modelDescriptors);

                            indices = new Matrix<int>(observedDescriptors.Rows, k);
                            using (Matrix<float> dist = new Matrix<float>(observedDescriptors.Rows, k))
                            {
                                matcher.KnnMatch(observedDescriptors, indices, dist, k, null);
                                mask = new Matrix<byte>(dist.Rows, 1);
                                mask.SetValue(255);
                                Features2DToolbox.VoteForUniqueness(dist, uniquenessThreshold, mask);
                            }

                            int nonZeroCount = CvInvoke.cvCountNonZero(mask);
                            if (nonZeroCount >= 4)
                            {
                                nonZeroCount = Features2DToolbox.VoteForSizeAndOrientation(modelKeyPoints, observedKeyPoints, indices, mask, 1.5, 20);
                                if (nonZeroCount >= 4)
                                    homography = Features2DToolbox.GetHomographyMatrixFromMatchedFeatures(modelKeyPoints, observedKeyPoints, indices, mask, 2);
                            }
                            int p = 0;
                            var maskEnumerator = mask.ManagedArray.GetEnumerator();
                            while ((maskEnumerator.MoveNext()) && (maskEnumerator.Current != null))
                            {
                                if (maskEnumerator.Current.GetType().Equals(typeof(byte)))
                                {
                                    var thisVal = (byte)maskEnumerator.Current;
                                    if (thisVal == 1) p++;
                                }
                            }
                            totalPoints = observedKeyPoints.Size;

                            matchTime = watch.ElapsedMilliseconds;

                            returnValue = nonZeroCount;

                            nonofZeroCount = returnValue;

                            imageMatchResults.Add(new ImageMatchResult()
                            {
                                Percentage = (p * 100 / (double)totalPoints),
                                MatchTime = matchTime,
                                MatchedPointsCount = p,
                                TotalPoints = totalPoints,
                                SampleImage = modelImage.Image
                            });
                        }
                        watch.Stop();
                    }
                    );
                }

                watchComplete.Stop();
                totalExecutionTime = watchComplete.ElapsedMilliseconds;
                return imageMatchResults;
            }
            catch (Exception ex)
            {
                totalExecutionTime = 1000;
                return _result;
            }
        }
    }

}
