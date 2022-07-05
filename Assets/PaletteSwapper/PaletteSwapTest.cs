using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Uee.PaletteSwapper
{
    public class PaletteSwapTest : MonoBehaviour
    {
        #region VARIABLES

        [Header("Buttons")]
        public bool GetPalette;
        public bool SwapColors;

        [Header("Source Texture")]
        public string SourcePath = "Assets/Resources/character-idle-big.png";

        [Header("Unity Color32")]
        public List<Color32> SourceColors32;
        public List<Color32> NewColors32;

        [Header("Color Map")]
        public List<List<PixelXY>> Map = new List<List<PixelXY>>();

        #endregion

        #region UNITY METHODS

        private async void OnValidate()
        {
            if (GetPalette)
            {
                GetPalette = false;

                Map = await Task.Run(() => LazyColorFinder.GetColorMap(SourcePath, out SourceColors32));
                print($"Source Palette Map Created - {Map != null}");

                if (Map == null) return;
                NewColors32 = new List<Color32>(SourceColors32);
            }

            if (SwapColors)
            {
                SwapColors = false;
                LazySwapper lazySwapper = new LazySwapper()
                {
                    ColorXYMap = Map
                };

                await Task.Run(() => lazySwapper.SwapColors(SourcePath, NewColors32));

#if UNITY_EDITOR
                UnityEditor.AssetDatabase.Refresh();
#endif
            }
        }

        #endregion
    }
}