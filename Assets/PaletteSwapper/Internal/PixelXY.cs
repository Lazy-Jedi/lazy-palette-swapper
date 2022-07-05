using System;

namespace Uee.PaletteSwapper
{
    [Serializable]
    public struct PixelXY
    {
        #region VARIABLES

        public int X;
        public int Y;

        #endregion

        #region CONSTRUCTORS

        public PixelXY(int x, int y)
        {
            X = x;
            Y = y;
        }

        #endregion

        #region METHODS

        public PixelXY Clone()
        {
            return new PixelXY
            {
                X = this.X,
                Y = this.Y
            };
        }

        #endregion
    }
}