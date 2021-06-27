using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;

using HtmlAgilityPack;
using SkiaSharp;
using Svg.Skia;

using Xunit;
using PassXYZLib;
using KeePassLib.Utility;

// Need to turn off test parallelization so we can validate the run order
[assembly: CollectionBehavior(DisableTestParallelization = true)]
[assembly: TestCollectionOrderer("KPCLib.xunit.Orderers.DisplayNameOrderer", "KPCLib.xunit")]

namespace KPCLib.xunit
{
    [Collection("Z. GfxUtil Collection")]
    public class GfxUtilTests
    {
        const string TEST_IMAGE = "../../../test.jpg";
        const string TEST_OUPTIMAGE = "test_output.png";

        void ScaleImage(string path, int w, int h)
        {
            try
            {
                if (!File.Exists(path))
                    throw new FileNotFoundException();

                byte[] pb = File.ReadAllBytes(path);
                GfxUtil.ScaleImage(GfxUtil.LoadImage(pb), w, h);
            }
            catch (ArgumentException)
            {
                Console.WriteLine("ArgumentException");
            }
            catch (System.Runtime.InteropServices.ExternalException)
            {
                Console.WriteLine("System.Runtime.InteropServices.ExternalException");
            }
            catch (Exception exImg)
            {
                Console.WriteLine($"Exception {exImg.Message}");
            }
        }

        void SaveScaledImage(string outputpath, int w, int h, string path = TEST_IMAGE)
        {
            try
            {
                if (!File.Exists(path))
                    throw new FileNotFoundException();

                byte[] pb = File.ReadAllBytes(path);
                GfxUtil.SaveImage(GfxUtil.ScaleImage(GfxUtil.LoadImage(pb), w, h), outputpath);
            }
            catch (ArgumentException)
            {
                Console.WriteLine("ArgumentException");
            }
            catch (System.Runtime.InteropServices.ExternalException)
            {
                Console.WriteLine("System.Runtime.InteropServices.ExternalException");
            }
            catch (Exception exImg)
            {
                Console.WriteLine($"Exception {exImg.Message}");
            }
        }

        [Fact]
        public void LoadImageNegativeTest()
        {
            Assert.Null(GfxUtil.LoadImage(null));
        }

        [Fact]
        public void LoadImageJunkTest()
        {
            var junkItems = new byte[] { 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20 };
            Assert.Null(GfxUtil.LoadImage(junkItems));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-100)]
        public void ScaleImageNegativeTest(int value)
        {
            Assert.Null(GfxUtil.ScaleImage(null, value, value));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-100)]
        public void ScaleImageWrongSizeTest(int value)
        {
            ScaleImage(TEST_IMAGE, value, value);
        }

        [Theory]
        [InlineData(100)]
        public void SaveImageWithWrongPath(int value)
        {
            SaveScaledImage(null, value, value);
        }

        [Fact]
        public void SaveImageWithWrongObject()
        {
            GfxUtil.SaveImage(null, TEST_OUPTIMAGE);
        }

        [Theory]
        [InlineData(32)]
        [InlineData(64)]
        [InlineData(96)]
        [InlineData(128)]
        [InlineData(256)]
        [InlineData(512)]
        public void ScaleMultipleImages(int value)
        {
            var resizedFile = "test" + value + ".png";
            SaveScaledImage(resizedFile, value, value);
        }

        [Theory]
        [InlineData("http://github.com")]
        [InlineData("http://www.baidu.com")]
        [InlineData("https://www.bing.com/")]
        [InlineData("http://www.youdao.com")]
        [InlineData("https://www.dell.com")]
        [InlineData("http://www.cmbchina.com")]
        public void GetIconTest(string url)
        {
            var faviconUrl = ItemExtensions.RetrieveFavicon(url);
            if(faviconUrl != null) 
            {
                var imageFolder = "images";
                try 
                {
                    DirectoryInfo di = new DirectoryInfo(imageFolder);
                    try
                    {
                        // Determine whether the directory exists.
                        if (!di.Exists)
                        {
                            di.Create();
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine("The process failed: {0}", e.ToString());
                    }

                    var uri = new Uri(faviconUrl);
                    WebClient myWebClient = new WebClient();
                    byte[] pb = myWebClient.DownloadData(faviconUrl);

                    if (faviconUrl.EndsWith(".ico") || faviconUrl.EndsWith(".png"))
                    {
                        GfxUtil.SaveImage(GfxUtil.ScaleImage(GfxUtil.LoadImage(pb), 128, 128), $"{imageFolder}/{uri.Host}.png");
                    }
                    else if (faviconUrl.EndsWith(".svg"))
                    {
                        GfxUtil.SaveImage(GfxUtil.LoadSvgImage(pb), $"{imageFolder}/{uri.Host}.png");
                    }
                    Debug.WriteLine($"{imageFolder}/{uri.Host}.png");
                }
                catch (System.Net.WebException ex)
                {
                    Debug.WriteLine($"{ex}");
                }
            }
            Assert.NotNull(faviconUrl);
        }

        [Theory]
        [InlineData("https://favicon.io/tutorials/what-is-a-favicon/")]
        public void NoFaviconTest(string url) 
        {
            Assert.Null(ItemExtensions.RetrieveFavicon(url));
        }

        [Fact]
        public void PrintImageFormat() 
        {
            Debug.WriteLine($"The image format is {SKEncodedImageFormat.Png.ToString()}.");
            Debug.WriteLine($"The image format is {SKEncodedImageFormat.Ico.ToString()}.");
        }
    }
}
