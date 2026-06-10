using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

// パレットを削除した後、生成しなおしてもパレット数がパレットカウントの通りにならない
// 生成しなおしたら履歴削除
// 現在のパレットで減色しなおしたい
// 色を拾いたい
// ならばペイントツールがあってもいい

public class EditController : MonoBehaviour
{
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void OpenImageFileDialog(string gameObjectName, string callbackMethodName);
#endif

    [SerializeField]
    private TMP_Text _fileName;

    [SerializeField]
    private Slider _brightnessSlider;

    [SerializeField]
    private TMP_Text _brightnessText;

    [SerializeField]
    private Slider _saturationSlider;

    [SerializeField]
    private TMP_Text _saturationText;

    [SerializeField]
    private Slider _contrastSlider;

    [SerializeField]
    private TMP_Text _contrastText;

    [SerializeField]
    private Slider _sharpnessSlider;

    [SerializeField]
    private TMP_Text _sharpnessText;

    [SerializeField]
    private Image _sourceImage;

    [SerializeField]
    private TMP_InputField _inputWidth;

    [SerializeField]
    private TMP_InputField _inputHeight;

    [SerializeField]
    private Slider _brightness2Slider;

    [SerializeField]
    private TMP_Text _brightness2Text;

    [SerializeField]
    private Slider _saturation2Slider;

    [SerializeField]
    private TMP_Text _saturation2Text;

    [SerializeField]
    private Slider _contrast2Slider;

    [SerializeField]
    private TMP_Text _contrast2Text;

    [SerializeField]
    private Slider _sharpness2Slider;

    [SerializeField]
    private TMP_Text _sharpness2Text;

    [SerializeField]
    private Image _resizedImage;

    [SerializeField]
    private TMP_InputField _inputPaletteCount;

    [SerializeField]
    private GameObject _colorPrefab;

    [SerializeField]
    private Transform _paletteTransform;

    [SerializeField]
    private ToggleGroup _paletteToggleGroup;

    [SerializeField]
    private Toggle _ditherToggle;

    [SerializeField]
    private Image _reductionedImage;

    [SerializeField]
    private ColorPicker _colorPicker;

    [SerializeField]
    private RectTransform _imageRectTransform;

    private Texture2D _sourceTexture = null;
    private Texture2D _indexTexture = null;

    private List<Board> _history = new List<Board>();
    private int _historyIndex = -1;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
    private void Open(string path)
    {
        byte[] fileBytes = File.ReadAllBytes(path);

        _sourceTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false);

        if (!_sourceTexture.LoadImage(fileBytes))
        {
            DestroyImmediate(_sourceTexture);
            Debug.LogError("画像の読み込みに失敗しました: " + path);
            return;
        }

        _fileName.SetText(path);

        Debug.Log($"読み込み成功: {_sourceTexture.width} x {_sourceTexture.height}");

        float aspectRatio = (float)_sourceTexture.width / _sourceTexture.height;
        _inputHeight.SetTextWithoutNotify((int)(float.Parse(_inputWidth.text) / aspectRatio) + "");

        Retouch();
    }

    public void OnFileLoaded(string dataUrl)
    {
        Debug.Log($"Length : {dataUrl.Length}");

        string head = dataUrl.Substring(0, Mathf.Min(100, dataUrl.Length));

        Debug.Log(head);
    }

    private void Retouch()
    {
        Texture2D retouchedTexture = EditUtility.Retouch(_sourceTexture, _brightnessSlider.value, _saturationSlider.value, _contrastSlider.value, _sharpnessSlider.value);

        EditUtility.SetImage(_sourceImage, retouchedTexture);

        Resize();
    }

    private void Resize()
    {
        Texture2D resizedTexture = EditUtility.Resize(int.Parse(_inputWidth.text), int.Parse(_inputHeight.text), _sourceImage.sprite.texture);
        Texture2D retouchedTexture = EditUtility.Retouch(resizedTexture, _brightness2Slider.value, _saturation2Slider.value, _contrast2Slider.value, _sharpness2Slider.value); ;

        EditUtility.SetImage(_resizedImage, retouchedTexture);

        CreatePalette();
    }

    private void CreatePalette()
    {
        Color32[] colors = EditUtility.GeneratePalette(_resizedImage.sprite.texture, int.Parse(_inputPaletteCount.text));

        for (int index = _paletteTransform.childCount - 1; index >= 0; index--)
        {
            Destroy(_paletteTransform.GetChild(index).gameObject);
        }

        foreach (Color32 color in colors)
        {
            GameObject gameObject = Instantiate(_colorPrefab, _paletteTransform);
            Toggle toggle = gameObject.GetComponent<Toggle>();
            toggle.group = _paletteToggleGroup;
            toggle.onValueChanged.AddListener(OnSelectPalette);
            gameObject.transform.GetChild(0).GetComponent<Image>().color = color;
        }

        Reduction(colors);
    }

    private void Reduction(Color32[] palette)
    {
        byte[,] matrix;

        if (_ditherToggle.isOn)
        {
            matrix = EditUtility.ReduceWithDither(_resizedImage.sprite.texture, palette);
        }
        else
        {
            matrix = EditUtility.ReduceWithoutDither(_resizedImage.sprite.texture, palette);
        }

        ClearHistory();
        AddHistory(new Board(matrix, palette));
    }

    public void Refresh()
    {
        Board board = _history[_historyIndex];

        Toggle toggle = _paletteToggleGroup.ActiveToggles().FirstOrDefault();
        int selectedIndex = toggle == null ? -1 : toggle.transform.GetSiblingIndex();

        int paletteCountDelta = board.Palette.Count() - _paletteTransform.childCount;
        if(0 < paletteCountDelta)
        {
            for(int i=0; i<paletteCountDelta; i++)
            {
                GameObject gameObject = Instantiate(_colorPrefab, _paletteTransform);
                gameObject.GetComponent<Toggle>().group = _paletteToggleGroup;
            }
        }
        else if(paletteCountDelta < 0)
        {
            for(int i=-1; paletteCountDelta <= i; i--)
            {
                Destroy(_paletteTransform.GetChild(_paletteTransform.childCount + i).gameObject);
            }
        }

        for(int i=0; i< board.Palette.Count(); i++)
        {
            Color32 color = board.Palette[i];
            _paletteTransform.GetChild(i).GetChild(0).GetComponent<Image>().color = color;
        }

        if(0 <= selectedIndex)
        {
            _paletteTransform.GetChild(selectedIndex).GetComponent<Toggle>().SetIsOnWithoutNotify(true);
        }

        board.Apply(ref _indexTexture);

        EditUtility.SetImage(_reductionedImage, _indexTexture);
    }

    /// <summary>
    /// ///////////////////////////////////////////////////////////
    /// </summary>

    public void OnOpen()
    {
#if UNITY_EDITOR
        string path = UnityEditor.EditorUtility.OpenFilePanelWithFilters(
            "画像を選択",
            "",
            new string[]
            {
                "Image Files", "png,jpg,jpeg"
            }
        );

        if (string.IsNullOrEmpty(path))
        {
            Debug.Log("キャンセル");
            return;
        }

        Debug.Log(path);

        Open(path);
#elif UNITY_WEBGL
        OpenImageFileDialog(gameObject.name, nameof(OnFileLoaded));
#endif
    }

    public void OnChangeBrightness()
    {
        _brightnessText.text = (int)(_brightnessSlider.value) + "";
        Retouch();
    }

    public void OnChangeSaturation()
    {
        _saturationText.text = (int)(_saturationSlider.value) + "";
        Retouch();
    }

    public void OnChangeContrast()
    {
        _contrastText.text = (int)(_contrastSlider.value) + "";
        Retouch();
    }

    public void OnChangeSharpness()
    {
        _sharpnessText.text = (int)(_sharpnessSlider.value) + "";
        Retouch();
    }

    public void OnChangeWidth()
    {
        float aspectRatio = (float)_sourceTexture.width / _sourceTexture.height;

        _inputHeight.SetTextWithoutNotify((int)(float.Parse(_inputWidth.text) / aspectRatio) + "");

        Resize();
    }

    public void OnChangeHeight()
    {
        float aspectRatio = (float)_sourceTexture.width / _sourceTexture.height;

        _inputWidth.SetTextWithoutNotify((int)(float.Parse(_inputHeight.text) * aspectRatio) + "");

        Resize();
    }

    public void OnChangeBrightness2()
    {
        _brightness2Text.text = (int)(_brightness2Slider.value) + "";
        Resize();
    }

    public void OnChangeSaturation2()
    {
        _saturation2Text.text = (int)(_saturation2Slider.value) + "";
        Resize();
    }

    public void OnChangeContrast2()
    {
        _contrast2Text.text = (int)(_contrast2Slider.value) + "";
        Resize();
    }

    public void OnChangeSharpness2()
    {
        _sharpness2Text.text = (int)(_sharpness2Slider.value) + "";
        Resize();
    }

    public void OnChangePaletteCount()
    {
        CreatePalette();
    }

    public void OnChangeDither()
    {
        CreatePalette();
    }

    public void OnSelectPalette(bool isOn)
    {
        if(isOn == false)
        {
            return;
        }

        Toggle toggle = _paletteToggleGroup.ActiveToggles().FirstOrDefault();
        if(toggle == null)
        {
            return;
        }

        Color32 color = toggle.transform.GetChild(0).GetComponent<Image>().color;
        _colorPicker.SetColor(color);
    }

    public void OnChangeColorPicker(Color32 color)
    {
        Toggle toggle = _paletteToggleGroup.ActiveToggles().FirstOrDefault();
        if (toggle != null)
        {
            int index = toggle.transform.GetSiblingIndex();

            Color32[] palette = (Color32[])_history[_historyIndex].Palette.Clone();
            byte[,] matrix = (byte[,])_history[_historyIndex].Matrix.Clone();

            palette[index] = color;

            AddHistory(new Board(matrix, palette));
        }
    }

    public void OnOverwiteColorPicker(Color32 color)
    {
        Toggle toggle = _paletteToggleGroup.ActiveToggles().FirstOrDefault();
        if (toggle != null)
        {
            int index = toggle.transform.GetSiblingIndex();

            Color32[] palette = (Color32[])_history[_historyIndex].Palette.Clone();
            byte[,] matrix = (byte[,])_history[_historyIndex].Matrix.Clone();

            palette[index] = color;

            OverwriteHistory(new Board(matrix, palette));
        }
    }

    public void OnPointerDown(BaseEventData eventData)
    {
        AddHistory(setPixcel(eventData));
    }

    public void OnDrag(BaseEventData eventData)
    {
        OverwriteHistory(setPixcel(eventData));
    }

    private Board setPixcel(BaseEventData eventData)
    {
        Toggle toggle = _paletteToggleGroup.ActiveToggles().FirstOrDefault();
        
        if (toggle == null)
        {
            return null;
        }

        int index = toggle.transform.GetSiblingIndex();

        Color32[] palette = (Color32[])_history[_historyIndex].Palette.Clone();
        byte[,] matrix = (byte[,])_history[_historyIndex].Matrix.Clone();

        PointerEventData pointerEventData = (PointerEventData)eventData;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _imageRectTransform,
            pointerEventData.position,
            pointerEventData.pressEventCamera,
            out Vector2 localPosition
        );

        Rect rect = _imageRectTransform.rect;

        float u = Mathf.Clamp01((localPosition.x - rect.xMin) / rect.width);
        float v = Mathf.Clamp01((localPosition.y - rect.yMin) / rect.height);

        int x = (int)(matrix.GetLength(1) * u);
        int y = (int)(matrix.GetLength(0) * (1.0f - v));

        //Debug.Log($"u:{u}, v:{v}, x:{x}, y:{y}");

        if (0 <= x && x < matrix.GetLength(1) && 0 <= y && y < matrix.GetLength(0))
        {
            matrix[y, x] = (byte)index;

            return new Board(matrix, palette);
        }

        return null;
    }

    public void OnPrevPalette()
    {
        Toggle toggle = _paletteToggleGroup.ActiveToggles().FirstOrDefault();

        if (toggle == null)
        {
            return;
        }

        byte selectedIndex = (byte)(toggle.transform.GetSiblingIndex());

        if(selectedIndex == 0)
        {
            return;
        }

        Color32[] palette = (Color32[])_history[_historyIndex].Palette.Clone();
        byte[,] matrix = (byte[,])_history[_historyIndex].Matrix.Clone();

        byte prevIndex = (byte)(selectedIndex - 1);

        for(int y=0; y< matrix.GetLength(0); y++)
        {
            for(int x=0; x< matrix.GetLength(1); x++)
            {
                if(matrix[y, x] == selectedIndex)
                {
                    matrix[y, x] = 255;
                }
            }
        }

        for (int y = 0; y < matrix.GetLength(0); y++)
        {
            for (int x = 0; x < matrix.GetLength(1); x++)
            {
                if (matrix[y, x] == prevIndex)
                {
                    matrix[y, x] = selectedIndex;
                }
            }
        }

        for (int y = 0; y < matrix.GetLength(0); y++)
        {
            for (int x = 0; x < matrix.GetLength(1); x++)
            {
                if (matrix[y, x] == 255)
                {
                    matrix[y, x] = prevIndex;
                }
            }
        }

        Color32 selectedColor = palette[selectedIndex];
        Color32 prevColor = palette[prevIndex];

        palette[selectedIndex] = prevColor;
        palette[prevIndex] = selectedColor;

        AddHistory(new Board(matrix, palette));

        _paletteTransform.GetChild(prevIndex).GetComponent<Toggle>().SetIsOnWithoutNotify(true);
    }

    public void OnNextPalette()
    {
        Toggle toggle = _paletteToggleGroup.ActiveToggles().FirstOrDefault();

        if (toggle == null)
        {
            return;
        }

        byte selectedIndex = (byte)(toggle.transform.GetSiblingIndex());

        Color32[] palette = (Color32[])_history[_historyIndex].Palette.Clone();
        byte[,] matrix = (byte[,])_history[_historyIndex].Matrix.Clone();

        if (palette.Count() - 1 == selectedIndex)
        {
            return;
        }

        byte nextIndex = (byte)(selectedIndex + 1);

        for (int y = 0; y < matrix.GetLength(0); y++)
        {
            for (int x = 0; x < matrix.GetLength(1); x++)
            {
                if (matrix[y, x] == selectedIndex)
                {
                    matrix[y, x] = 255;
                }
            }
        }

        for (int y = 0; y < matrix.GetLength(0); y++)
        {
            for (int x = 0; x < matrix.GetLength(1); x++)
            {
                if (matrix[y, x] == nextIndex)
                {
                    matrix[y, x] = selectedIndex;
                }
            }
        }

        for (int y = 0; y < matrix.GetLength(0); y++)
        {
            for (int x = 0; x < matrix.GetLength(1); x++)
            {
                if (matrix[y, x] == 255)
                {
                    matrix[y, x] = nextIndex;
                }
            }
        }

        Color32 selectedColor = palette[selectedIndex];
        Color32 nextColor = palette[nextIndex];

        palette[selectedIndex] = nextColor;
        palette[nextIndex] = selectedColor;

        AddHistory(new Board(matrix, palette));

        _paletteTransform.GetChild(nextIndex).GetComponent<Toggle>().SetIsOnWithoutNotify(true);
    }

    public void OnAddPalette()
    {
        Color32[] palette = (Color32[])_history[_historyIndex].Palette.Clone();
        byte[,] matrix = (byte[,])_history[_historyIndex].Matrix.Clone();

        Array.Resize(ref palette, palette.Length + 1);
        palette[palette.Length - 1] = Color.black;

        AddHistory(new Board(matrix, palette));
    }

    public void OnDeletePalette()
    {
        Toggle toggle = _paletteToggleGroup.ActiveToggles().FirstOrDefault();

        if (toggle == null)
        {
            return;
        }

        byte selectedIndex = (byte)(toggle.transform.GetSiblingIndex());

        if(selectedIndex == 0)
        {
            return;
        }

        Color32[] palette = (Color32[])_history[_historyIndex].Palette.Clone();
        byte[,] matrix = (byte[,])_history[_historyIndex].Matrix.Clone();

        List<Color32> paletteList = palette.ToList();

        paletteList.RemoveAt(selectedIndex);
        
        for(int y=0; y<matrix.GetLength(0); y++)
        {
            for(int x=0; x<matrix.GetLength(1); x++)
            {
                if(selectedIndex <= matrix[y, x])
                {
                    matrix[y, x] -= 1;
                }
            }
        }

        AddHistory(new Board(matrix, paletteList.ToArray()));

        _paletteToggleGroup.transform.GetChild(selectedIndex - 1).GetComponent<Toggle>().SetIsOnWithoutNotify(true);
    }

    public void AddHistory(Board board)
    {
        if(_history.Count != 0)
        {
            if (board == _history[_historyIndex])
            {
                return;
            }

            if (_historyIndex < _history.Count - 1)
            {
                _history.RemoveRange(_historyIndex + 1, _history.Count - _historyIndex - 1);
            }
        }
        
        _history.Add(board);
        _historyIndex = _history.Count - 1;

        Refresh();
    }

    public void OverwriteHistory(Board board)
    {
        if (_history.Count != 0)
        {
            _history[_historyIndex] = board;
        }

        Refresh();
    }

    public void ClearHistory()
    {
        _history.Clear();
        _historyIndex = -1;
    }

    public void OnBackHistory()
    {
        if(_historyIndex == 0)
        {
            return;
        }

        _historyIndex--;
        Refresh();
    }

    public void OnForwardHistory()
    {
        if(_historyIndex == _history.Count - 1)
        {
            return;
        }

        _historyIndex++;
        Refresh();
    }
}