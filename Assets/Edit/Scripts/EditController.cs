using System;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EditController : MonoBehaviour
{
    [SerializeField]
    private TMP_Text _fileName;

    [SerializeField]
    private Image _sourceImage;

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
    private TMP_InputField _inputWidth;

    [SerializeField]
    private TMP_InputField _inputHeight;

    [SerializeField]
    private Image _resizedImage;

    [SerializeField]
    private Image _retouchedImage;

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

        Texture2D sourceTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false);

        if (!sourceTexture.LoadImage(fileBytes))
        {
            DestroyImmediate(sourceTexture);
            Debug.LogError("画像の読み込みに失敗しました: " + path);
            return;
        }

        _fileName.SetText(path);

        Debug.Log($"読み込み成功: {sourceTexture.width} x {sourceTexture.height}");

        EditUtility.SetImage(_sourceImage, sourceTexture);

        Retouch();
    }

    private void Retouch()
    {
        Texture2D retouchedTexture = EditUtility.Retouch(_sourceImage.sprite.texture, _brightnessSlider.value, _saturationSlider.value, _contrastSlider.value, _sharpnessSlider.value);

        EditUtility.SetImage(_retouchedImage, retouchedTexture);

        Resize();
    }

    private void Resize()
    {
        Texture2D resizedTexture = EditUtility.Resize(int.Parse(_inputWidth.text), int.Parse(_inputHeight.text), _retouchedImage.sprite.texture);

        EditUtility.SetImage(_resizedImage, resizedTexture);

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
            gameObject.GetComponent<Toggle>().group = _paletteToggleGroup;
            gameObject.transform.GetChild(0).GetComponent<Image>().color = color;
        }

        Reduction();
    }

    private void Reduction()
    {
        Color32[] palette = new Color32[_paletteTransform.childCount];

        for (int i = 0; i < _paletteTransform.childCount; i++)
        {
            palette[i] = _paletteTransform.GetChild(i).GetChild(0).GetComponent<Image>().color;
        }

        Texture2D reductionedTexture;

        if (_ditherToggle.isOn)
        {
            reductionedTexture = EditUtility.ReduceWithFloydSteinbergDither(_resizedImage.sprite.texture, palette);
        }
        else
        {
            reductionedTexture = EditUtility.ReduceWithoutDither(_resizedImage.sprite.texture, palette);
        }

        EditUtility.SetImage(_reductionedImage, reductionedTexture);
    }

    /// <summary>
    /// ///////////////////////////////////////////////////////////
    /// </summary>

    public void OnOpen()
    {
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
        float aspectRatio = (float)_sourceImage.sprite.texture.width / _sourceImage.sprite.texture.height;

        _inputHeight.SetTextWithoutNotify((int)(float.Parse(_inputWidth.text) / aspectRatio) + "");

        Resize();
    }

    public void OnChangeHeight()
    {
        float aspectRatio = (float)_sourceImage.sprite.texture.width / _sourceImage.sprite.texture.height;

        _inputWidth.SetTextWithoutNotify((int)(float.Parse(_inputHeight.text) * aspectRatio) + "");

        Resize();
    }

    public void OnChangePaletteCount()
    {
        CreatePalette();
    }

    public void OnChangeDither()
    {
        Reduction();
    }
}