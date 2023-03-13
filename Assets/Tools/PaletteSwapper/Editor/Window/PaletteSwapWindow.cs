#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using UnityEditor.Compilation;
using Color = UnityEngine.Color;
using Font = UnityEngine.Font;

namespace Uee.PaletteSwapper
{
    public class PaletteSwapWindow : EditorWindow
    {
        #region WINDOW

        public static PaletteSwapWindow Window;

        [MenuItem("Lazy-Jedi/Tools/Lazy Palette Swapper", priority = 400)]
        public static void OpenWindow()
        {
            Window = GetWindow<PaletteSwapWindow>(true, "Lazy Palette Swapper");
            Window.Show();
        }

        #endregion

        #region STYLING

        private GUIContent _sourceContent;

        private GUIStyle _titleLabel;
        private GUIStyle _headerLabel;
        private GUIStyle _centeredHelpBoxLabel;
        private GUIStyle _centeredLabel;

        #endregion

        #region VARIABLES

        private CancellationTokenSource _cancellationTokenSource;
        private CancellationToken _cancellationToken;

        private Task _getPalettesTask;
        private Task _swapColorsTask;

        private Texture2D _source;
        private string _sourcePath;

        private List<Color32> _sourceColors;
        private int _sourceColorsCount;

        private List<Color32> _newColors;
        private int _newColorsCount;

        private int _ignorePixelsWithAlpha = 0;

        private List<List<PixelXY>> _map = new List<List<PixelXY>>();

        private string _outputPath = string.Empty;
        private string _filename = string.Empty;
        private string _extension = string.Empty;

        private LazySwapper _lazySwapper;

        private bool _showAdvancedSettings = false;
        private bool _useAsync = true;

        #endregion

        #region UNITY METHODS

        private void OnEnable()
        {
            Initialization();
        }

        public void OnGUI()
        {
            DrawTitle();
            DrawSourceTexture();
            DrawAdvancedSettings();
            DrawPalettesSettings();
            DrawOutputSettings();
        }

        private void OnDestroy()
        {
            if (_cancellationTokenSource == null || !_useAsync) return;
            if (_getPalettesTask == null && _swapColorsTask == null) return;
            if (_getPalettesTask.IsCompleted && _swapColorsTask.IsCompleted) return;

            CompilationPipeline.RequestScriptCompilation();
            _cancellationTokenSource.Cancel();
            Debug.Log("Cancelling all Tasks - Requires Script Recompile");
        }

        #endregion

        #region METHODS

        private void DrawTitle()
        {
            EditorGUILayout.Space(32f);
            EditorGUILayout.LabelField("Lazy Palette Swapper", _titleLabel);
            EditorGUILayout.Space(20f);
            EditorGUILayout.LabelField("Warning - This tool was designed for Pixel Art and not for extremely detailed Images.\nYou can try, but you have been warned!", _centeredLabel);
        }

        private void DrawSourceTexture()
        {
            EditorGUILayout.Space(24f);
            EditorGUILayout.LabelField("Source Texture", _headerLabel);
            EditorGUILayout.Space(8f);
            using (EditorGUILayout.VerticalScope verticalScope = new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUI.BeginChangeCheck();

                _source = (Texture2D)EditorGUILayout.ObjectField(_sourceContent, _source, typeof(Texture2D), false);

                if (_source) EditorGUILayout.SelectableLabel(_sourcePath, _centeredHelpBoxLabel);

                if (EditorGUI.EndChangeCheck())
                {
                    _sourcePath = AssetDatabase.GetAssetPath(_source);

                    string sourcePathDirectory = Path.GetDirectoryName(_sourcePath);
                    _filename = $"{Path.GetFileNameWithoutExtension(_sourcePath)}-new-palette";
                    _extension = $"{Path.GetExtension(_sourcePath)}";
                    _outputPath = Path.Combine(sourcePathDirectory, $"{_filename}{_extension}");

                    ResetColorLists();
                }
            }
        }

        private void DrawAdvancedSettings()
        {
            EditorGUILayout.Space(16f);
            EditorGUILayout.LabelField("Advanced Settings", _headerLabel);

            _showAdvancedSettings = EditorGUILayout.ToggleLeft($"{(_showAdvancedSettings ? "Hide" : "Show")} Advanced Settings", _showAdvancedSettings);

            if (!_showAdvancedSettings) return;
            using (EditorGUILayout.VerticalScope verticalScope = new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                _useAsync = EditorGUILayout.ToggleLeft("Use Async?", _useAsync);
                EditorGUILayout.HelpBox("Use Async if your Sprite or Spritesheet is relatively big!", MessageType.Info, true);

                EditorGUILayout.Space(4f);
                _ignorePixelsWithAlpha = EditorGUILayout.IntSlider("Ignore Colors with Alpha:", _ignorePixelsWithAlpha, 0, 255);
                EditorGUILayout.HelpBox
                (
                    $"Adjust Ignore Colours with Alpha to skip any Pixels whose Alpha is Less than or Equal to - {_ignorePixelsWithAlpha}",
                    MessageType.Info,
                    true
                );
            }
        }

        private void DrawPalettesSettings()
        {
            EditorGUILayout.Space(16f);
            EditorGUILayout.LabelField("Palette Settings", _headerLabel);

            EditorGUILayout.Space(4f);
            using (EditorGUILayout.VerticalScope verticalScope = new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                GetPaletteButton();

                EditorGUILayout.Space(4f);
                using (EditorGUILayout.HorizontalScope horizontalScope = new EditorGUILayout.HorizontalScope())
                {
                    DrawPaletteHelper(_sourceColors, _sourceColorsCount, "Source Texture Palette");
                    EditorGUILayout.Space(4);
                    DrawPaletteHelper(_newColors, _newColorsCount, "Editable Palette", true);
                }

                EditorGUILayout.Space(4f);
                ResetPaletteButton();
            }
        }

        private async void GetPaletteButton()
        {
            if (GUILayout.Button("Get Palette", EditorStyles.miniButton))
            {
                if (_useAsync)
                {
                    _getPalettesTask = Task.Run(GetPalettes, _cancellationToken);
                    await _getPalettesTask;
                }
                else
                {
                    GetPalettes();
                }
            }
        }

        private async void ResetPaletteButton()
        {
            if (GUILayout.Button("Reset New Palette", EditorStyles.miniButton))
            {
                if (_source && _sourceColors == null)
                {
                    if (_useAsync)
                    {
                        _getPalettesTask = Task.Run(GetPalettes, _cancellationToken);
                        await _getPalettesTask;
                        Repaint();
                    }
                    else GetPalettes();
                }
                else
                {
                    _newColors = new List<Color32>(_sourceColors);
                }
            }
        }

        private void DrawOutputSettings()
        {
            EditorGUILayout.Space(16f);
            EditorGUILayout.LabelField("Output Settings", _headerLabel);

            EditorGUILayout.Space(4f);
            using (EditorGUILayout.VerticalScope verticalScope = new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                DrawOutputPath();
                SwapPaletteButton();
            }
        }

        private void DrawOutputPath()
        {
            using (EditorGUILayout.HorizontalScope horizontalScope = new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                _outputPath = EditorGUILayout.TextField("Output Path:", _outputPath);
                if (GUILayout.Button("Browse", GUILayout.Width(64f)))
                {
                    string newPath =
                        EditorUtility.SaveFilePanelInProject
                        (
                            "Save Image",
                            $"{_filename}",
                            "png",
                            string.Empty
                        );

                    if (string.IsNullOrEmpty(newPath)) return;
                    _outputPath = newPath;
                }
            }
        }

        private async void SwapPaletteButton()
        {
            if (GUILayout.Button($"{(_sourceColors == null ? "Get Palette" : "Swap Palette")}"))
            {
                if (!_source) throw new Exception("Source Texture is not valid!");

                if (_sourceColors == null)
                {
                    if (_useAsync)
                    {
                        _getPalettesTask = Task.Run(GetPalettes, _cancellationToken);
                        await _getPalettesTask;
                        Repaint();
                    }
                    else GetPalettes();

                    return;
                }

                if (_useAsync)
                {
                    _swapColorsTask = Task.Run(SwapPalette, _cancellationToken);
                    await _swapColorsTask;
                }
                else
                {
                    _lazySwapper = new LazySwapper
                    {
                        ColorXYMap = _map,
                        OutputPath = _outputPath
                    };

                    _lazySwapper.SwapColors(_sourcePath, _newColors);
                }

                AssetDatabase.Refresh();
            }
        }

        #endregion

        #region HELPER METHODS

        private void GetPalettes()
        {
            ResetColorLists();

            _map = LazyColorFinder.GetColorMap(_sourcePath, out _sourceColors, (byte)_ignorePixelsWithAlpha);

            _newColors = new List<Color32>(_sourceColors);
            _sourceColorsCount = _sourceColors.Count;
            _newColorsCount = _newColors.Count;
        }

        private void SwapPalette()
        {
            _lazySwapper = new LazySwapper
            {
                ColorXYMap = _map,
                OutputPath = _outputPath
            };

            _lazySwapper.SwapColors(_sourcePath, _newColors);
        }

        private void ResetColorLists()
        {
            _sourceColors = null;
            _sourceColorsCount = 0;

            _newColors = null;
            _newColorsCount = 0;
        }

        private void DrawPaletteHelper(List<Color32> colors, int colorCount, string label, bool isEditable = false)
        {
            using (EditorGUILayout.VerticalScope verticalScope = new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField(label, _centeredHelpBoxLabel);

                for (int i = 0; i < colorCount; i++)
                {
                    if (!isEditable) EditorGUILayout.ColorField(GUIContent.none, colors[i], false, true, false);
                    else colors[i] = EditorGUILayout.ColorField(GUIContent.none, colors[i], true, true, false);
                }
            }
        }

        #endregion

        #region INITIALIZATION METHOD

        private void Initialization()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            _cancellationToken = _cancellationTokenSource.Token;

            if (_sourceContent == null) _sourceContent = new GUIContent("Source Texture2D:");

            if (_titleLabel == null)
            {
                _titleLabel = new GUIStyle()
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 64,
                    font = Resources.Load<Font>("Fonts/kenney-fonts/MiniSquare_Editor")
                };
                _titleLabel.normal.textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black;
            }

            if (_headerLabel == null)
            {
                _headerLabel = new GUIStyle()
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 24,
                    font = Resources.Load<Font>("Fonts/kenney-fonts/MiniSquare_Editor")
                };
                _headerLabel.normal.textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black;
            }

            if (_centeredHelpBoxLabel == null)
            {
                _centeredHelpBoxLabel = new GUIStyle(EditorStyles.helpBox)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 12,
                };
                _centeredHelpBoxLabel.normal.textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black;
            }

            if (_centeredLabel == null)
            {
                _centeredLabel = new GUIStyle()
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 12,
                };
                _centeredLabel.normal.textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black;
            }
        }

        #endregion
    }
}
#endif