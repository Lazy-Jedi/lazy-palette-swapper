using UnityEngine;

namespace Uee.PaletteSwapper
{
    public static class ColorExtensions
    {
        public static Color32 ToColor32(this System.Drawing.Color color)
        {
            return new Color32(color.R, color.G, color.B, color.A);
        }

        public static System.Drawing.Color ToSystemColor(this Color32 color)
        {
            return System.Drawing.Color.FromArgb(color.a, color.r, color.g, color.b);
        }
    }
}