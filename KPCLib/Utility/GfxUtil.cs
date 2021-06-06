﻿/*
  KeePass Password Safe - The Open-Source Password Manager
  Copyright (C) 2003-2021 Dominik Reichl <dominik.reichl@t-online.de>

  This program is free software; you can redistribute it and/or modify
  it under the terms of the GNU General Public License as published by
  the Free Software Foundation; either version 2 of the License, or
  (at your option) any later version.

  This program is distributed in the hope that it will be useful,
  but WITHOUT ANY WARRANTY; without even the implied warranty of
  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
  GNU General Public License for more details.

  You should have received a copy of the GNU General Public License
  along with this program; if not, write to the Free Software
  Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA  02110-1301  USA
*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

#if KPCLib
using SkiaSharp;
using Image = SkiaSharp.SKBitmap;
using Svg.Skia;
#else
#if !KeePassUAP
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
#endif
#endif // KPCLib

namespace KeePassLib.Utility
{
	public static class GfxUtil
	{
#if (!KeePassLibSD && !KeePassUAP)
		private sealed class GfxImage
		{
			public byte[] Data;

			public int Width;
			public int Height;

			public GfxImage(byte[] pbData, int w, int h)
			{
				this.Data = pbData;
				this.Width = w;
				this.Height = h;
			}

#if DEBUG
			// For debugger display
			public override string ToString()
			{
				return (this.Width.ToString() + "x" + this.Height.ToString());
			}
#endif
		}
#endif

#if KPCLib
		public static Image LoadSvgImage(byte[] pb, int w = 128, int h = 128) 
		{
			if (pb == null) { return null; }

			using (var ms = new MemoryStream(pb, false))
			{
				var svg = new SKSvg();
				svg.Load(ms);

				var imageInfo = new SKImageInfo(w, h);
				using (var surface = SKSurface.Create(imageInfo))
				using (var canvas = surface.Canvas)
				{
					// calculate the scaling need to fit to screen
					var scaleX = w / svg.Picture.CullRect.Width;
					var scaleY = h / svg.Picture.CullRect.Height;
					var matrix = SKMatrix.CreateScale((float)scaleX, (float)scaleY);

					// draw the svg
					canvas.Clear(SKColors.Transparent);
					canvas.DrawPicture(svg.Picture, ref matrix);
					canvas.Flush();

					var data = surface.Snapshot();
					return SKBitmap.FromImage(data);
				}
			}
		}

		public static Image LoadImage(byte[] pb)
        {
            if(pb == null) { return null; }

            using (var ms = new MemoryStream(pb, false))
            {
                return Image.Decode(ms);
            }
        }

        public static Image ScaleImage(Image img, int w, int h)
        {
            if (img == null) { return null; }

            return img.Resize(new SKImageInfo(w, h), SKFilterQuality.High);
        }

        public static void SaveImage(Image img, string path)
        {
            if (img == null || path == null) { return; }
            SKImage image = SKImage.FromBitmap(img);
            SKData png = image.Encode(SKEncodedImageFormat.Png, 100);

            using (var filestream = File.OpenWrite(path))
            {
                png.SaveTo(filestream);
            }
        }
#else
#if KeePassUAP
		public static Image LoadImage(byte[] pb)
		{
			if(pb == null) throw new ArgumentNullException("pb");

			MemoryStream ms = new MemoryStream(pb, false);
			try { return Image.FromStream(ms); }
			finally { ms.Close(); }
		}
#else
		public static Image LoadImage(byte[] pb)
		{
			if(pb == null) throw new ArgumentNullException("pb");

#if !KeePassLibSD
			// First try to load the data as ICO and afterwards as
			// normal image, because trying to load an ICO using
			// the normal image loading methods can result in a
			// low resolution image
			try
			{
				Image imgIco = ExtractBestImageFromIco(pb);
				if(imgIco != null) return imgIco;
			}
			catch(Exception) { Debug.Assert(false); }
#endif

			MemoryStream ms = new MemoryStream(pb, false);
			try { return LoadImagePriv(ms); }
			finally { ms.Close(); }
		}

		private static Image LoadImagePriv(Stream s)
		{
			// Image.FromStream wants the stream to be open during
			// the whole lifetime of the image; as we can't guarantee
			// this, we make a copy of the image
			Image imgSrc = null;
			try
			{
#if !KeePassLibSD
				imgSrc = Image.FromStream(s);
				Bitmap bmp = new Bitmap(imgSrc.Width, imgSrc.Height,
					PixelFormat.Format32bppArgb);

				try
				{
					bmp.SetResolution(imgSrc.HorizontalResolution,
						imgSrc.VerticalResolution);
					Debug.Assert(bmp.Size == imgSrc.Size);
				}
				catch(Exception) { Debug.Assert(false); }
#else
				imgSrc = new Bitmap(s);
				Bitmap bmp = new Bitmap(imgSrc.Width, imgSrc.Height);
#endif

				using(Graphics g = Graphics.FromImage(bmp))
				{
					g.Clear(Color.Transparent);

#if !KeePassLibSD
					g.DrawImageUnscaled(imgSrc, 0, 0);
#else
					g.DrawImage(imgSrc, 0, 0);
#endif
				}

				return bmp;
			}
			finally { if(imgSrc != null) imgSrc.Dispose(); }
		}

#if !KeePassLibSD
		private static Image ExtractBestImageFromIco(byte[] pb)
		{
			List<GfxImage> l = UnpackIco(pb);
			if((l == null) || (l.Count == 0)) return null;

			long qMax = 0;
			foreach(GfxImage gi in l)
			{
				if(gi.Width == 0) gi.Width = 256;
				if(gi.Height == 0) gi.Height = 256;

				qMax = Math.Max(qMax, (long)gi.Width * (long)gi.Height);
			}

			byte[] pbHdrPng = new byte[] {
				0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A
			};
			byte[] pbHdrJpeg = new byte[] { 0xFF, 0xD8, 0xFF };

			Image imgBest = null;
			int bppBest = -1;

			foreach(GfxImage gi in l)
			{
				if(((long)gi.Width * (long)gi.Height) < qMax) continue;

				byte[] pbImg = gi.Data;
				Image img = null;
				try
				{
					if((pbImg.Length > pbHdrPng.Length) &&
						MemUtil.ArraysEqual(pbHdrPng,
						MemUtil.Mid<byte>(pbImg, 0, pbHdrPng.Length)))
						img = GfxUtil.LoadImage(pbImg);
					else if((pbImg.Length > pbHdrJpeg.Length) &&
						MemUtil.ArraysEqual(pbHdrJpeg,
						MemUtil.Mid<byte>(pbImg, 0, pbHdrJpeg.Length)))
						img = GfxUtil.LoadImage(pbImg);
					else
					{
						using(MemoryStream ms = new MemoryStream(pb, false))
						{
							using(Icon ico = new Icon(ms, gi.Width, gi.Height))
							{
								img = ico.ToBitmap();
							}
						}
					}
				}
				catch(Exception) { Debug.Assert(false); }

				if(img == null) continue;

				if((img.Width < gi.Width) || (img.Height < gi.Height))
				{
					Debug.Assert(false);
					img.Dispose();
					continue;
				}

				int bpp = GetBitsPerPixel(img.PixelFormat);
				if(bpp > bppBest)
				{
					if(imgBest != null) imgBest.Dispose();

					imgBest = img;
					bppBest = bpp;
				}
				else img.Dispose();
			}

			return imgBest;
		}

		private static List<GfxImage> UnpackIco(byte[] pb)
		{
			if(pb == null) { Debug.Assert(false); return null; }

			const int SizeICONDIR = 6;
			const int SizeICONDIRENTRY = 16;

			if(pb.Length < SizeICONDIR) return null;
			if(MemUtil.BytesToUInt16(pb, 0) != 0) return null; // Reserved, 0
			if(MemUtil.BytesToUInt16(pb, 2) != 1) return null; // ICO type, 1

			int n = MemUtil.BytesToUInt16(pb, 4);
			if(n < 0) { Debug.Assert(false); return null; }

			int cbDir = SizeICONDIR + (n * SizeICONDIRENTRY);
			if(pb.Length < cbDir) return null;

			List<GfxImage> l = new List<GfxImage>();
			int iOffset = SizeICONDIR;
			for(int i = 0; i < n; ++i)
			{
				int w = pb[iOffset];
				int h = pb[iOffset + 1];
				if((w < 0) || (h < 0)) { Debug.Assert(false); return null; }

				int cb = MemUtil.BytesToInt32(pb, iOffset + 8);
				if(cb <= 0) return null; // Data must have header (even BMP)

				int p = MemUtil.BytesToInt32(pb, iOffset + 12);
				if(p < cbDir) return null;
				if((p + cb) > pb.Length) return null;

				try
				{
					byte[] pbImage = MemUtil.Mid<byte>(pb, p, cb);
					GfxImage img = new GfxImage(pbImage, w, h);
					l.Add(img);
				}
				catch(Exception) { Debug.Assert(false); return null; }

				iOffset += SizeICONDIRENTRY;
			}

			return l;
		}

		private static int GetBitsPerPixel(PixelFormat f)
		{
			int bpp = 0;
			switch(f)
			{
				case PixelFormat.Format1bppIndexed:
					bpp = 1;
					break;

				case PixelFormat.Format4bppIndexed:
					bpp = 4;
					break;

				case PixelFormat.Format8bppIndexed:
					bpp = 8;
					break;

				case PixelFormat.Format16bppArgb1555:
				case PixelFormat.Format16bppGrayScale:
				case PixelFormat.Format16bppRgb555:
				case PixelFormat.Format16bppRgb565:
					bpp = 16;
					break;

				case PixelFormat.Format24bppRgb:
					bpp = 24;
					break;

				case PixelFormat.Format32bppArgb:
				case PixelFormat.Format32bppPArgb:
				case PixelFormat.Format32bppRgb:
					bpp = 32;
					break;

				case PixelFormat.Format48bppRgb:
					bpp = 48;
					break;

				case PixelFormat.Format64bppArgb:
				case PixelFormat.Format64bppPArgb:
					bpp = 64;
					break;

				default:
					Debug.Assert(false);
					break;
			}

			return bpp;
		}

		public static Image ScaleImage(Image img, int w, int h)
		{
			return ScaleImage(img, w, h, ScaleTransformFlags.None);
		}

		/// <summary>
		/// Resize an image.
		/// </summary>
		/// <param name="img">Image to resize.</param>
		/// <param name="w">Width of the returned image.</param>
		/// <param name="h">Height of the returned image.</param>
		/// <param name="f">Flags to customize scaling behavior.</param>
		/// <returns>Resized image. This object is always different
		/// from <paramref name="img" /> (i.e. they can be
		/// disposed separately).</returns>
		public static Image ScaleImage(Image img, int w, int h,
			ScaleTransformFlags f)
		{
			if(img == null) throw new ArgumentNullException("img");
			if(w < 0) throw new ArgumentOutOfRangeException("w");
			if(h < 0) throw new ArgumentOutOfRangeException("h");

			bool bUIIcon = ((f & ScaleTransformFlags.UIIcon) !=
				ScaleTransformFlags.None);

			// We must return a Bitmap object for UIUtil.CreateScaledImage
			Bitmap bmp = new Bitmap(w, h, PixelFormat.Format32bppArgb);
			using(Graphics g = Graphics.FromImage(bmp))
			{
				g.Clear(Color.Transparent);

				g.SmoothingMode = SmoothingMode.HighQuality;
				g.CompositingQuality = CompositingQuality.HighQuality;

				int wSrc = img.Width;
				int hSrc = img.Height;

				InterpolationMode im = InterpolationMode.HighQualityBicubic;
				if((wSrc > 0) && (hSrc > 0))
				{
					if(bUIIcon && ((w % wSrc) == 0) && ((h % hSrc) == 0))
						im = InterpolationMode.NearestNeighbor;
					// else if((w < wSrc) && (h < hSrc))
					//	im = InterpolationMode.HighQualityBilinear;
				}
				else { Debug.Assert(false); }
				g.InterpolationMode = im;

				RectangleF rSource = new RectangleF(0.0f, 0.0f, wSrc, hSrc);
				RectangleF rDest = new RectangleF(0.0f, 0.0f, w, h);
				AdjustScaleRects(ref rSource, ref rDest);

				g.DrawImage(img, rDest, rSource, GraphicsUnit.Pixel);
			}

			return bmp;
		}

		internal static void AdjustScaleRects(ref RectangleF rSource,
			ref RectangleF rDest)
		{
			// When enlarging images, apply a -0.5 offset to avoid
			// the scaled image being cropped on the top/left side;
			// when shrinking images, do not apply a -0.5 offset,
			// otherwise the image is cropped on the bottom/right
			// side; this applies to all interpolation modes
			if(rDest.Width > rSource.Width)
				rSource.X = rSource.X - 0.5f;
			if(rDest.Height > rSource.Height)
				rSource.Y = rSource.Y - 0.5f;

			// When shrinking, apply a +0.5 offset, such that the
			// scaled image is less cropped on the bottom/right side
			if(rDest.Width < rSource.Width)
				rSource.X = rSource.X + 0.5f;
			if(rDest.Height < rSource.Height)
				rSource.Y = rSource.Y + 0.5f;
		}

#if DEBUG
		public static Image ScaleTest(Image[] vIcons)
		{
			Bitmap bmp = new Bitmap(1024, vIcons.Length * (256 + 12),
				PixelFormat.Format32bppArgb);

			using(Graphics g = Graphics.FromImage(bmp))
			{
				g.Clear(Color.White);

				int[] v = new int[] { 16, 24, 32, 48, 64, 128, 256 };

				int x;
				int y = 8;

				foreach(Image imgIcon in vIcons)
				{
					if(imgIcon == null) { Debug.Assert(false); continue; }

					x = 128;

					foreach(int q in v)
					{
						using(Image img = ScaleImage(imgIcon, q, q,
							ScaleTransformFlags.UIIcon))
						{
							g.DrawImageUnscaled(img, x, y);
						}

						x += q + 8;
					}

					y += v[v.Length - 1] + 8;
				}
			}

			return bmp;
		}
#endif // DEBUG
#endif // !KeePassLibSD
#endif // KeePassUAP

		internal static string ImageToDataUri(Image img)
		{
			if(img == null) { Debug.Assert(false); return string.Empty; }

			byte[] pb = null;
			using(MemoryStream ms = new MemoryStream())
			{
				img.Save(ms, ImageFormat.Png);
				pb = ms.ToArray();
			}

			return StrUtil.DataToDataUri(pb, "image/png");
		}

		internal static ulong HashImage64(Image img)
		{
			Bitmap bmp = (img as Bitmap);
			if(bmp == null) { Debug.Assert(false); return 0; }

			BitmapData bd = null;
			try
			{
				int w = bmp.Width, h = bmp.Height;
				if((w <= 0) || (h <= 0)) { Debug.Assert(false); return 0; }

				bd = bmp.LockBits(new Rectangle(0, 0, w, h),
					ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

				if(bd.Stride != (w * 4)) { Debug.Assert(false); return 0; }

				Debug.Assert(Marshal.SizeOf(typeof(int)) == 4);
				int cp = w * h;
				int[] v = new int[cp];
				Marshal.Copy(bd.Scan0, v, 0, cp);

				ulong u = (ulong)w * 0x50EF39EB5BE34CA9;
				for(int i = 0; i < cp; ++i)
					u = (u ^ (uint)v[i]) * 0x6E18585D2D174BD5;
				return (u ^ (u >> 32));
			}
			catch(Exception) { Debug.Assert(false); }
			finally
			{
				if(bd != null) bmp.UnlockBits(bd);
			}

			return 0;
		}
#endif // KPCLib
	}
}
