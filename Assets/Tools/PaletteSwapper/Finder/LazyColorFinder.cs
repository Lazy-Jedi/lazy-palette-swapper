using System;
using System.Collections.Generic;
using System.Drawing;
using UnityEngine;

namespace Uee.PaletteSwapper
{
    public static class LazyColorFinder
    {
        #region VARIABLES

        private const float TRANSPARENT_ALPHA = 0.0f;

        #endregion

        #region MAP BITMAP PALETTE

        public static List<List<PixelXY>> GetColorMap(string texturePath, out List<Color32> sourceColors, byte ignoreColorsWithAlpha = 0)
        {
            if (string.IsNullOrEmpty(texturePath)) throw new Exception("Texture Path is not valid!");
            if (ignoreColorsWithAlpha < 0) ignoreColorsWithAlpha = 0;

            using (Bitmap source = new Bitmap(texturePath))
            {
                if (source == null) throw new Exception("Source Bitmap does not exist.");

                float width = source.Width;
                float height = source.Height;

                List<List<PixelXY>> map = new List<List<PixelXY>>();
                sourceColors = new List<Color32>();

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        Color32 pixelColor = source.GetPixel(x, y).ToColor32();
                        if (pixelColor.a <= ignoreColorsWithAlpha) continue;
                        int index = sourceColors.IndexOf(pixelColor);

                        if (index > -1)
                        {
                            map[index].Add(new PixelXY(x, y));
                        }
                        else
                        {
                            sourceColors.Add(pixelColor);
                            map.Add(new List<PixelXY>
                            {
                                new PixelXY()
                                {
                                    X = x,
                                    Y = y
                                }
                            });
                        }
                    }
                }

                return map;
            }
        }

        #endregion
    }
}