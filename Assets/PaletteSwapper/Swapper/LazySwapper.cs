using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using UnityEngine;

namespace Uee.PaletteSwapper
{
    public class LazySwapper
    {
        #region VARIABLES

        private List<List<PixelXY>> _colorXYMap;
        private string _outputPath = string.Empty;

        #endregion

        #region PROPERTIES

        public List<List<PixelXY>> ColorXYMap
        {
            set => _colorXYMap = value;
        }

        public string OutputPath
        {
            set => _outputPath = value;
        }

        #endregion

        #region METHODS

        public void SwapColors(string texturePath, List<Color32> newColors)
        {
            if (string.IsNullOrEmpty(texturePath)) throw new Exception("Texture Path is not valid!");
            if (newColors == null || newColors.Count == 0) throw new Exception("No new colours.");
            if (_colorXYMap == null || _colorXYMap.Count == 0) throw new Exception("Color XY Map does not exist.");

            using (Bitmap source = new Bitmap(texturePath))
            {
                if (source == null) throw new Exception("Source Bitmap does not exist.");
                int width = source.Width;
                int height = source.Height;
                
                using (Bitmap newTexture = new Bitmap(width, height))
                {
                    int colorsCount = newColors.Count;
                    for (int colorIndex = 0; colorIndex < colorsCount; colorIndex++)
                    {
                        List<PixelXY> pixelXYs = _colorXYMap[colorIndex];
                        int length = pixelXYs.Count;
                        for (int i = 0; i < length; i++)
                        {
                            newTexture.SetPixel(pixelXYs[i].X, pixelXYs[i].Y, newColors[colorIndex].ToSystemColor());
                        }
                    }

                    if (string.IsNullOrEmpty(_outputPath))
                    {
                        string sourcePathDirectory = Path.GetDirectoryName(texturePath);
                        string filename = Path.GetFileNameWithoutExtension(texturePath);
                        string extension = Path.GetExtension(texturePath);
                        newTexture.Save(Path.Combine(sourcePathDirectory, $"{filename}-new-pallete{extension}"), ImageFormat.Png);
                    }
                    else
                    {
                        newTexture.Save(_outputPath, ImageFormat.Png);
                    }
                }
            }
        }

        #endregion
    }
}