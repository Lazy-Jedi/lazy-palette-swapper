using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using UnityEngine;

namespace Uee.PaletteSwapper
{
    public static class Palette
    {
        #region VARIABLES

        #endregion

        #region METHODS

        public static void ToPaletteStrip(List<Color32> colours, string palettePath)
        {
            if (colours == null || colours.Count == 0) throw new Exception("Invalid List of Colours.");

            using (Bitmap palette = new Bitmap(colours.Count, 1))
            {
                int coloursCount = colours.Count;
                for (int i = 0; i < coloursCount; i++)
                {
                    palette.SetPixel(i, 1, colours[i].ToSystemColor());
                }

                palette.Save(palettePath, ImageFormat.Png);
            }
        }

        #endregion
    }
}