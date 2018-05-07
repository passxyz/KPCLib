﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Splat
{
    public enum CompressedBitmapFormat
    {
        Png, Jpeg,
    }

    /// <summary>
    /// Represents the platform-specific image loader class. Unless you are
    /// testing image loading, you don't usually need to implement this.
    /// </summary>
    public interface IBitmapLoader
    {
        /// <summary>
        /// Loads a bitmap from a byte stream
        /// </summary>
        /// <param name="sourceStream">The stream to load the image from.</param>
        /// <param name="desiredWidth">The desired width of the image.</param>
        /// <param name="desiredHeight">The desired height of the image.</param>
        /// <returns>A future result representing the loaded image</returns>
        Task<IBitmap> Load(Stream sourceStream, float? desiredWidth, float? desiredHeight);

        /// <summary>
        /// Loads from the application's resources (i.e. from bundle on Cocoa,
        /// from Pack URIs on Windows, etc)
        /// </summary>
        /// <param name="source">The source resource, as a relative path.</param>
        /// <param name="desiredWidth">Desired width.</param>
        /// <param name="desiredHeight">Desired height.</param>
        /// <returns>A future result representing the loaded image</returns>
        Task<IBitmap> LoadFromResource(string source, float? desiredWidth, float? desiredHeight);

        /// <summary>
        /// Creates an empty bitmap of the specified dimensions
        /// </summary>
        /// <param name="width">The width of the canvas</param>
        /// <param name="height">The height of the canvas</param>
        /// <returns>A new image. Use ToNative() to convert this to a native bitmap</returns>
        IBitmap Create(float width, float height);
    }

    /// <summary>
    /// Represents a bitmap image that was loaded via a ViewModel. Every platform
    /// provides FromNative and ToNative methods to convert this object to the
    /// platform-specific versions.
    /// </summary>
    public interface IBitmap : IDisposable
    {
        /// <summary>
        /// Width in pixel units (depending on platform)
        /// </summary>
        float Width { get; }

        /// <summary>
        /// Height in pixel units (depending on platform)
        /// </summary>
        float Height { get; }

        /// <summary>
        /// Saves an image to a target stream
        /// </summary>
        /// <param name="format">The format to save the image in.</param>
        /// <param name="quality">If JPEG is specified, this is a quality 
        /// factor between 0.0 and 1.0f where 1.0f is the best quality.</param>
        /// <param name="target">The target stream to save to.</param>
        /// <returns>A signal indicating the Save has completed.</returns>
        Task Save(CompressedBitmapFormat format, float quality, Stream target);
    }

    /// <summary>
    /// This class loads and creates bitmap resources in a platform-independent 
    /// way.
    /// </summary>
    public static class BitmapLoader
    {
        static BitmapLoader()
        {
            var platBitmapLoader = Xamarin.Forms.DependencyService.Get<IBitmapLoader>();

            // var platBitmapLoader = AssemblyFinder.AttemptToLoadType<IBitmapLoader>("Splat.PlatformBitmapLoader");
#if DEBUG
            if (platBitmapLoader == null)
                Debug.WriteLine("BitmapLoader: Cannot load the inteface.");
            else
                Debug.WriteLine("BitmapLoader: Loaded the inteface successfully.");
#endif
            _Current = platBitmapLoader;
        }

        // TODO: This needs to be improved once we move the "Detect in Unit Test 
        // Runner" code into Splat
        static IBitmapLoader _Current;

        public static IBitmapLoader Current {
            get {
                var ret = _Current;
                if (ret == null) {
                    throw new Exception("Could not find a default bitmap loader. This should never happen, your dependency resolver is broken");
                }

                return ret;
            }
            set { _Current = value; }
        }
    }
}
