﻿/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See license-pdn.txt for full licensing and attribution details.             //
//                                                                             //
// Ported to Pinta by: Olivier Dufour <olivier.duff@gmail.com>                 //
/////////////////////////////////////////////////////////////////////////////////

using System;

namespace Pinta.ImageManipulation.Effects
{
	public class RadialBlurEffect : BaseEffect
	{
		private double angle;
		private PointD offset;
		private int quality;

		/// <summary>
		/// Creates a new effect that will apply a radial blur to an image.
		/// </summary>
		/// <param name="angle">Angle of the blur.</param>
		/// <param name="offset">Center point of the blur.</param>
		/// <param name="quality">Quality of the radial blur. Valid range is 1 - 5.</param>
		public RadialBlurEffect (double angle = 2, PointD offset = new PointD (), int quality = 2)
		{
			if (quality < 1 || quality > 5)
				throw new ArgumentOutOfRangeException ("quality");

			this.angle = angle;
			this.offset = offset;
			this.quality = quality;
		}

		#region Algorithm Code Ported From PDN
		protected unsafe override void RenderLine (ISurface src, ISurface dst, Rectangle rect)
		{
			if (angle == 0) {
				// Copy src to dest
				return;
			}

			int w = dst.Width;
			int h = dst.Height;
			int fcx = (w << 15) + (int)(offset.X * (w << 15));
			int fcy = (h << 15) + (int)(offset.Y * (h << 15));

			int n = (quality * quality) * (30 + quality * quality);

			int fr = (int)(angle * Math.PI * 65536.0 / 181.0);

			for (int y = rect.Top; y <= rect.Bottom; ++y) {
				ColorBgra* dstPtr = dst.GetPointAddress (rect.Left, y);
				ColorBgra* srcPtr = src.GetPointAddress (rect.Left, y);

				for (int x = rect.Left; x <= rect.Right; ++x) {
					int fx = (x << 16) - fcx;
					int fy = (y << 16) - fcy;

					int fsr = fr / n;

					int sr = 0;
					int sg = 0;
					int sb = 0;
					int sa = 0;
					int sc = 0;

					sr += srcPtr->R * srcPtr->A;
					sg += srcPtr->G * srcPtr->A;
					sb += srcPtr->B * srcPtr->A;
					sa += srcPtr->A;
					++sc;

					int ox1 = fx;
					int ox2 = fx;
					int oy1 = fy;
					int oy2 = fy;

					for (int i = 0; i < n; ++i) {
						Rotate (ref ox1, ref oy1, fsr);
						Rotate (ref ox2, ref oy2, -fsr);

						int u1 = ox1 + fcx + 32768 >> 16;
						int v1 = oy1 + fcy + 32768 >> 16;

						if (u1 > 0 && v1 > 0 && u1 < w && v1 < h) {
							ColorBgra* sample = src.GetPointAddress (u1, v1);

							sr += sample->R * sample->A;
							sg += sample->G * sample->A;
							sb += sample->B * sample->A;
							sa += sample->A;
							++sc;
						}

						int u2 = ox2 + fcx + 32768 >> 16;
						int v2 = oy2 + fcy + 32768 >> 16;

						if (u2 > 0 && v2 > 0 && u2 < w && v2 < h) {
							ColorBgra* sample = src.GetPointAddress (u2, v2);

							sr += sample->R * sample->A;
							sg += sample->G * sample->A;
							sb += sample->B * sample->A;
							sa += sample->A;
							++sc;
						}
					}

					if (sa > 0) {
						*dstPtr = ColorBgra.FromBgra (
							Utility.ClampToByte (sb / sa),
							Utility.ClampToByte (sg / sa),
							Utility.ClampToByte (sr / sa),
							Utility.ClampToByte (sa / sc));
					} else {
						dstPtr->Bgra = 0;
					}

					++dstPtr;
					++srcPtr;
				}
			}
		}

		private static void Rotate (ref int fx, ref int fy, int fr)
		{
			int cx = fx;
			int cy = fy;

			//sin(x) ~~ x
			//cos(x)~~ 1 - x^2/2
			fx = cx - ((cy >> 8) * fr >> 8) - ((cx >> 14) * (fr * fr >> 11) >> 8);
			fy = cy + ((cx >> 8) * fr >> 8) - ((cy >> 14) * (fr * fr >> 11) >> 8);
		}
		#endregion
	}
}
