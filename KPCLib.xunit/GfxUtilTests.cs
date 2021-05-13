using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;

using HtmlAgilityPack;
using SkiaSharp;
using Svg.Skia;

using Xunit;
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

        private static bool UrlExists(string url)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(url))
                {
                    // Check URL format
                    var uri = new Uri(url);
                    if(uri.Scheme.Contains("http") || uri.Scheme.Contains("https")) 
                    {
                        var webRequest = WebRequest.Create(url);
                        webRequest.Method = "HEAD";
                        var webResponse = (HttpWebResponse)webRequest.GetResponse();
                        return webResponse.StatusCode == HttpStatusCode.OK;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"{ex}");
            }
            return false;
        }

        private static string FormatUrl(string url, string baseUrl) 
        {
            if(url.StartsWith("//")) { return ("http:" + url); }
            else if (url.StartsWith("/")) { return (baseUrl + url); }

            return url;
        }

        public static string RetrieveFavicon(string url) 
        {
            string returnFavicon = null;

            // declare htmlweb and load html document
            HtmlWeb web = new HtmlWeb();
            var htmlDoc = web.Load(url);

            //1. 处理 apple-touch-icon 的情况
            var elementsAppleTouchIcon = htmlDoc.DocumentNode.SelectNodes("//link[contains(@rel, 'apple-touch-icon')]");
            if (elementsAppleTouchIcon != null && elementsAppleTouchIcon.Any())
            {
                var favicon = elementsAppleTouchIcon.First();
                var faviconUrl = FormatUrl(favicon.GetAttributeValue("href", null), url);
                if (UrlExists(faviconUrl))
                {
                    return faviconUrl;
                }
            }

            // 2. Try to get svg version
            var el = htmlDoc.DocumentNode.SelectSingleNode("/html/head/link[@rel='icon' and @href]");
            if (el != null)
            {
                try
                {
                    var faviconUrl = FormatUrl(el.Attributes["href"].Value, url);

                    if (UrlExists(faviconUrl))
                    {
                        return faviconUrl;
                    }
                }
                catch (System.Net.WebException ex)
                {
                    Debug.WriteLine($"{ex}");
                }
            }

            // 3. 从页面的 HTML 中抓取
            var elements = htmlDoc.DocumentNode.SelectNodes("//link[contains(@rel, 'icon')]");
            if (elements != null && elements.Any())
            {
                var favicon = elements.First();
                var faviconUrl = FormatUrl(favicon.GetAttributeValue("href", null), url);
                if (UrlExists(faviconUrl))
                {
                    return faviconUrl;
                }
            }

            // 4. 直接获取站点的根目录图标
            try
            {
                var uri = new Uri(url);
                if (uri.HostNameType == UriHostNameType.Dns)
                {
                    var faviconUrl = string.Format("{0}://{1}/favicon.ico", uri.Scheme == "https" ? "https" : "http", uri.Host);
                    if (UrlExists(faviconUrl))
                    {
                        return faviconUrl;
                    }
                }
            }
            catch (System.UriFormatException ex) 
            {
                Debug.WriteLine($"{ex}");
                return returnFavicon;
            }

            return returnFavicon;
        }

        [Theory]
        [InlineData("http://github.com")]
        [InlineData("http://www.baidu.com")]
        [InlineData("https://mailchimp.com")]
        [InlineData("http://www.youdao.com")]
        [InlineData("http://whatsapp.com")]
        [InlineData("https://soundcloud.com")]
        [InlineData("http://www.cmbchina.com")]
        // Test case 4: http://www.cmbchina.com
        // Test case 2: http://www.baidu.com
        // Test case 1: https://mailchimp.com
        // Test case 3: http://www.youdao.com
        // Test case 2: https://github.com
        // Test case 3: http://whatsapp.com
        public void GetIconTest(string url)
        {
            var faviconUrl = RetrieveFavicon(url);
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
            Assert.Null(RetrieveFavicon(url));
        }

        [Fact]
        public void PrintImageFormat() 
        {
            Debug.WriteLine($"The image format is {SKEncodedImageFormat.Png.ToString()}.");
            Debug.WriteLine($"The image format is {SKEncodedImageFormat.Ico.ToString()}.");
        }
    }
}
