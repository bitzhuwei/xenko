﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.

// Copyright (c) 2010-2012 SharpDX - Alexandre Mutel
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Diagnostics;
using System.IO;
using NUnit.Framework;
using SiliconStudio.Core;
using SiliconStudio.Core.Diagnostics;
using SiliconStudio.Core.IO;
using SiliconStudio.Xenko.Engine;
using SiliconStudio.Xenko.Graphics.Regression;

namespace SiliconStudio.Xenko.Graphics.Tests
{
    /// <summary>
    /// Tests for <see cref="Texture"/>
    /// </summary>
    [TestFixture]
    [Description("Tests for Graphics.Image")]
    public class TestImage : GameTestBase
    {
        /// <summary>
        /// Tests Image 1D.
        /// </summary>
        [Test]
        public void TestImage1D()
        {
            const int Size = 256; // must be > 256 to work
            int bufferCount = (int)Math.Log(Size, 2) + 1;
            int ExpectedSizePerArraySlice = Size * 2 - 1;

            var source = Image.New1D(Size, true, PixelFormat.R8_UNorm);

            // 9 buffers: 256 + 128 + 64 + 32 + 16 + 8 + 4 + 2 + 1
            Assert.AreEqual(source.TotalSizeInBytes, ExpectedSizePerArraySlice);
            Assert.AreEqual(source.PixelBuffer.Count, bufferCount);

            // Check with array size
            var dest = Image.New1D(Size, true, PixelFormat.R8_UNorm, 6);
            Assert.AreEqual(dest.TotalSizeInBytes, ExpectedSizePerArraySlice * 6);
            Assert.AreEqual(dest.PixelBuffer.Count, bufferCount * 6);

            ManipulateImage(source, dest, 5, 0, 0);

            // Dispose images
            source.Dispose();
            dest.Dispose();
        }

        /// <summary>
        /// Tests Image 2D.
        /// </summary>
        [Test]
        public void TestImage2D()
        {
            const int Size = 256; // must be > 256 to work
            int bufferCount = (int)Math.Log(Size, 2) + 1;
            int expectedSizePerArraySlice = ((int)Math.Pow(4, bufferCount) - 1) / 3;

            var source = Image.New2D(Size, Size, true, PixelFormat.R8_UNorm);

            // 9 buffers: 256 + 128 + 64 + 32 + 16 + 8 + 4 + 2 + 1
            Assert.AreEqual(source.TotalSizeInBytes, expectedSizePerArraySlice);
            Assert.AreEqual(source.PixelBuffer.Count, bufferCount);

            // Check with array size
            var dest = Image.New2D(Size, Size, true, PixelFormat.R8_UNorm, 6);
            Assert.AreEqual(dest.TotalSizeInBytes, expectedSizePerArraySlice * 6);
            Assert.AreEqual(dest.PixelBuffer.Count, bufferCount * 6);

            ManipulateImage(source, dest, 5, 0, 0);

            // Dispose images
            source.Dispose();
            dest.Dispose();
        }

        /// <summary>
        /// Tests Image 2D.
        /// </summary>
        [Test]
        public void TestImage3D()
        {
            const int Size = 64; // must be > 256 to work
            int bufferCount = (int)Math.Log(Size, 2) + 1;
            // BufferSize(x) = 1/7 (8^(x+1)-1)
            int expectedTotalBufferCount = (int)Math.Pow(2, bufferCount) - 1;
            int expectedSizePerArraySlice = ((int)Math.Pow(8, bufferCount) - 1) / 7;

            var source = Image.New3D(Size, Size, Size, true, PixelFormat.R8_UNorm);

            // 9 buffers: 256 + 128 + 64 + 32 + 16 + 8 + 4 + 2 + 1
            Assert.AreEqual(source.TotalSizeInBytes, expectedSizePerArraySlice);
            Assert.AreEqual(source.PixelBuffer.Count, expectedTotalBufferCount);

            var dest = Image.New3D(Size, Size, Size, true, PixelFormat.R8_UNorm);

            ManipulateImage(source, dest, 0, 5, 0);

            // Dispose images
            source.Dispose();
            dest.Dispose();
        }

        private void ManipulateImage(Image source, Image dest, int arrayIndex, int zIndex, int mipIndex)
        {
            // Use Set Pixel
            var fromPixelBuffer = source.PixelBuffer[0];
            var toPixelBuffer = dest.PixelBuffer[arrayIndex, zIndex, mipIndex];

            fromPixelBuffer.SetPixel(0, 0, (byte)255);
            fromPixelBuffer.SetPixel(16, 0, (byte)128);
            fromPixelBuffer.CopyTo(toPixelBuffer);

            Assert.True(Utilities.CompareMemory(fromPixelBuffer.DataPointer, toPixelBuffer.DataPointer, fromPixelBuffer.BufferStride));

            // Use Get Pixels
            var fromPixels = fromPixelBuffer.GetPixels<byte>();
            Assert.AreEqual(fromPixels.Length, source.Description.Width * source.Description.Height);

            // Check values
            Assert.AreEqual(fromPixels[0], 255);
            Assert.AreEqual(fromPixels[16], 128);

            // Use Set Pixels
            fromPixels[0] = 1;
            fromPixels[16] = 2;
            fromPixelBuffer.SetPixels(fromPixels);

            // Use Get Pixel
            Assert.AreEqual(fromPixelBuffer.GetPixel<byte>(0, 0), 1);
            Assert.AreEqual(fromPixelBuffer.GetPixel<byte>(16, 0), 2);
        }

        [Test]
        [Ignore]
        public void TestPerfLoadSave()
        {
            Image image;
            using (var stream = File.Open("map.bmp", FileMode.Open))
                image = Image.Load(stream);

            const int Count = 100; // Change this to perform memory benchmarks
            var types = new[]
            {
                ImageFileType.Dds,
            };
            var clock = Stopwatch.StartNew();
            foreach (var imageFileType in types)
            {
                clock.Restart();
                for (int i = 0; i < Count; i++)
                {
                    using (var stream = File.Open("map2.bin", FileMode.Create))
                        image.Save(stream, imageFileType);
                }
                Log.Info("Save [{0}] {1} in {2}ms", Count, imageFileType, clock.ElapsedMilliseconds);
            }

            image.Dispose();
        }

        private long testMemoryBefore;

        [Test]
        public void TestLoadAndSave()
        {
            foreach (ImageFileType sourceFormat in Enum.GetValues(typeof(ImageFileType)))
            {
                foreach (ImageFileType intermediateFormat in Enum.GetValues(typeof(ImageFileType)))
                {
                    if (sourceFormat == ImageFileType.Wmp) // no input image of this format.
                        continue;

                    if (Platform.Type == PlatformType.Android && (
                        intermediateFormat == ImageFileType.Bmp || // TODO remove this when Save method is implemented for the bmp format
                        intermediateFormat == ImageFileType.Gif || // TODO remove this when Save method is implemented for the gif format
                        intermediateFormat == ImageFileType.Tiff || // TODO remove this when Save method is implemented for the tiff format
                        sourceFormat == ImageFileType.Bmp || // TODO remove this when Load method is fixed for the bmp format
                        sourceFormat == ImageFileType.Tiff)) // TODO remove this when Load method is fixed for the tiff format
                        continue; 

                    if (intermediateFormat == ImageFileType.Wmp || sourceFormat == ImageFileType.Wmp ||
                        intermediateFormat == ImageFileType.Tga || sourceFormat == ImageFileType.Tga) // TODO remove this when Load/Save methods are implemented for those types.
                        continue;

                    PerformTest(
                        game =>
                        {
                            ProcessFiles(game, sourceFormat, intermediateFormat);
                        });
                }
            }
        }

        [Test]
        public void TestLoadPremultiplied()
        {
            var sourceFormat = ImageFileType.Png;
            var intermediateFormat = ImageFileType.Png;

            PerformTest(
                game =>
                {
                    // Load an image from a file and dispose it.
                    var fileName = "debug" + "Image";
                    var filePath = "ImageTypes/" + fileName;
                    Image image;

                    // Load an image from a buffer
                    byte[] buffer;
                    using (var inStream = game.Asset.OpenAsStream(filePath, StreamFlags.None))
                    {
                        var bufferSize = inStream.Length;
                        buffer = new byte[bufferSize];
                        inStream.Read(buffer, 0, (int)bufferSize);
                    }

                    using (image = Image.Load(buffer))
                    {
                        // Write this image to a memory stream using DDS format.
                        var tempStream = new MemoryStream();
                        image.Save(tempStream, intermediateFormat);
                        tempStream.Position = 0;
                        
                        // Reload the image from the memory stream.
                        var image2 = Image.Load(tempStream);
                        CompareImage(image, image2, false, 0, fileName);
                        image2.Dispose();
                    }
                });
        }

        private void ProcessFiles(Game game, ImageFileType sourceFormat, ImageFileType intermediateFormat)
        {
            Log.Info("Testing {0}", intermediateFormat);
            Console.Out.Flush();
            var imageCount = 0;
            var clock = Stopwatch.StartNew();

            // Load an image from a file and dispose it.
            var fileName = sourceFormat.ToFileExtension().Substring(1) + "Image";
            var filePath = "ImageTypes/" + fileName;
            Image image;
            using (var inStream = game.Asset.OpenAsStream(filePath, StreamFlags.None))
                image = Image.Load(inStream);
            image.Dispose();

            // Load an image from a buffer
            byte[] buffer;
            using (var inStream = game.Asset.OpenAsStream(filePath, StreamFlags.None))
            {
                var bufferSize = inStream.Length;
                buffer = new byte[bufferSize];
                inStream.Read(buffer, 0, (int)bufferSize);
            }

            using (image = Image.Load(buffer))
            {
                // Write this image to a memory stream using DDS format.
                var tempStream = new MemoryStream();
                image.Save(tempStream, intermediateFormat);
                tempStream.Position = 0;

                // Save to a file on disk
                var extension = intermediateFormat.ToFileExtension();
                using (var outStream = VirtualFileSystem.ApplicationCache.OpenStream(fileName + extension, VirtualFileMode.Create, VirtualFileAccess.Write))
                    image.Save(outStream, intermediateFormat);

                if (intermediateFormat == ImageFileType.Xenko || intermediateFormat == ImageFileType.Dds || (sourceFormat == intermediateFormat 
                    && intermediateFormat != ImageFileType.Gif)) // TODO: remove this when Giff compression/decompression is fixed
                {
                    int allowSmallDifferences;
                    switch (intermediateFormat)
                    {
                        case ImageFileType.Tiff:// TODO: remove this when tiff encryption implementation is stable
                        case ImageFileType.Png: // TODO: remove this when png  encryption implementation is stable
                            allowSmallDifferences = 1;
                            break;
                        case ImageFileType.Jpg: // TODO: remove this when jepg encryption implementation is stable
                            allowSmallDifferences = 30;
                            break;
                        default:
                            allowSmallDifferences = 0;
                            break;
                    }

                    // Reload the image from the memory stream.
                    var image2 = Image.Load(tempStream);
                    CompareImage(image, image2, false, allowSmallDifferences, fileName);
                    image2.Dispose();
                }
            }

            imageCount++;

            var time = clock.ElapsedMilliseconds;
            clock.Stop();

            GC.Collect();
            GC.WaitForPendingFinalizers();
            var testMemoryAfter = GC.GetTotalMemory(true);
            Log.Info("Loaded {0} and convert to (Dds, Jpg, Png, Gif, Bmp, Tiff) image from DirectXSDK test Memory: {1} bytes, in {2}ms", imageCount, testMemoryAfter - testMemoryBefore, time);
        }

        internal static void CompareImage(Image from, Image to, bool ignoreAlpha, int allowedDifference = 0, string file = null)
        {
            // Check that description is identical to original image loaded from the disk.
            Assert.AreEqual(from.Description, to.Description, "Image description is different for image [{0}]", file);

            // Check that number of buffers are identical.
            Assert.AreEqual(from.PixelBuffer.Count, to.PixelBuffer.Count, "PixelBuffer size is different for image [{0}]", file);

            // Compare each pixel buffer
            for (int j = 0; j < from.PixelBuffer.Count; j++)
            {
                var srcPixelBuffer = from.PixelBuffer[j];
                var dstPixelBuffer = to.PixelBuffer[j];

                // Check only row and slice pitchs
                Assert.AreEqual(srcPixelBuffer.RowStride, dstPixelBuffer.RowStride, "RowPitch are different for index [{0}], image [{1}]", j, file);
                Assert.AreEqual(srcPixelBuffer.BufferStride, dstPixelBuffer.BufferStride, "SlicePitch are different for index [{0}], image [{1}]", j, file);

                var isSameBuffer = CompareImageData(srcPixelBuffer.DataPointer, dstPixelBuffer.DataPointer, srcPixelBuffer.BufferStride, ignoreAlpha, allowedDifference);
                //if (!isSameBuffer)
                //{
                //    var stream = new FileStream("test_from.dds", FileMode.Create, FileAccess.Write, FileShare.Write);
                //    stream.Write(srcPixelBuffer.DataPointer, 0, srcPixelBuffer.BufferStride);
                //    stream.Close();
                //    stream = new FileStream("test_to.dds", NativeFileMode.Create, NativeFileAccess.Write, NativeFileShare.Write);
                //    stream.Write(dstPixelBuffer.DataPointer, 0, dstPixelBuffer.BufferStride);
                //    stream.Close();
                //}

                Assert.True(isSameBuffer, "Content of PixelBuffer is different for index [{0}], image [{1}]", j, file);
            }
        }

        public static unsafe bool CompareImageData(IntPtr from, IntPtr against, int sizeToCompare, bool ignoreAlpha, int allowedDifference = 0)
        {
            var pSrc = (byte*)from;
            var pDst = (byte*)against;

            // Compare remaining bytes.
            var pixelCount = sizeToCompare >> 2;
            while (pixelCount > 0)
            {
                for (int i = 0; i < 4; i++)
                {
                    var originalValue = *pSrc;
                    var newValue = *pDst;
                    if ((i != 3 || !ignoreAlpha) && Math.Abs(originalValue - newValue) > allowedDifference)
                        return false;

                    pSrc++;
                    pDst++;
                }

                pixelCount -= 4;
            }

            return true;
        }
    }
}