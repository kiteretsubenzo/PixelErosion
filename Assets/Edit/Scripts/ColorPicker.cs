using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[System.Serializable]
public class ColorPickerEvent : UnityEvent<Color32>
{
}

public class ColorPicker : MonoBehaviour
{
    [SerializeField]
    private Image _svImage;

    [SerializeField]
    private RectTransform _svRectTransform;

    [SerializeField]
    private RectTransform _svCursor;

    [SerializeField]
    private RectTransform _hueRectTransform;

    [SerializeField]
    private RectTransform _hueCursor;

    [SerializeField]
    private TMP_InputField _rInput;

    [SerializeField]
    private TMP_InputField _gInput;

    [SerializeField]
    private TMP_InputField _bInput;

    [SerializeField]
    private TMP_InputField _aInput;

    [SerializeField]
    private TMP_InputField _hexInput;

    [SerializeField]
    private ColorPickerEvent _onColorChanged;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void SetRGBAText(byte r, byte g, byte b, byte a)
    {
        _rInput.SetTextWithoutNotify(r + "");
        _gInput.SetTextWithoutNotify(g + "");
        _bInput.SetTextWithoutNotify(b + "");
        _aInput.SetTextWithoutNotify(a + "");
        _hexInput.SetTextWithoutNotify(ColorUtility.ToHtmlStringRGBA(new Color32(r, g, b, a)));
    }

    private void SetHueCursor(float h)
    {
        Rect hRect = _hueRectTransform.rect;

        float hy = Mathf.Lerp(hRect.yMin, hRect.yMax, h);

        _hueCursor.anchoredPosition = new Vector2(_hueCursor.anchoredPosition.x, hy);

        _svImage.color = Color.HSVToRGB(h, 1.0f, 1.0f);

        (float s, float v) sv = GetSV();
        Color32 color = Color.HSVToRGB(h, sv.s, sv.v);
        color.a = GetAlpha();
    }

    private void SetSVCursor(float s, float v)
    {
        Rect svRect = _svRectTransform.rect;

        float svx = Mathf.Lerp(-svRect.width * 0.5f, svRect.width * 0.5f, s);
        float svy = Mathf.Lerp(svRect.yMin, svRect.yMax, v);

        _svCursor.anchoredPosition = new Vector2(svx, svy);

        float h = GetHue();
        Color32 color = Color.HSVToRGB(h, s, v);
        color.a = GetAlpha();
    }

    private (byte r, byte g, byte b) GetRGB()
    {
        return (byte.Parse(_rInput.text), byte.Parse(_gInput.text), byte.Parse(_bInput.text));
    }

    private byte GetAlpha()
    {
        return byte.Parse(_aInput.text);
    }

    private (float s, float v) GetSV()
    {
        Rect rect = _svRectTransform.rect;

        Vector2 position = _svCursor.anchoredPosition;

        float normalizedX = Mathf.InverseLerp(
            -rect.width * 0.5f,
            rect.width * 0.5f,
            position.x
        );

        float normalizedY = Mathf.InverseLerp(
            rect.yMin,
            rect.yMax,
            position.y
        );

        return(normalizedX, normalizedY);
    }

    private float GetHue()
    {
        Rect rect = _hueRectTransform.rect;

        float normalizedY = Mathf.InverseLerp(
            rect.yMin,
            rect.yMax,
            _hueCursor.anchoredPosition.y
        );

        return Mathf.Clamp01(normalizedY);
    }

    public void OnChangeRGB()
    {
        if (byte.TryParse(_rInput.text, out byte r) == false)
        {
            r = 0;
        }

        if (byte.TryParse(_gInput.text, out byte g) == false)
        {
            g = 0;
        }

        if (byte.TryParse(_bInput.text, out byte b) == false)
        {
            b = 0;
        }

        SetRGBAText(r, g, b, GetAlpha());

        Color.RGBToHSV(new Color32(r, g, b, GetAlpha()), out float h, out float s, out float v);

        SetHueCursor(h);
        SetSVCursor(s, v);

        _onColorChanged?.Invoke(new Color32(r, g, b, GetAlpha()));
    }

    public void OnChangeAlpha()
    {
        if (byte.TryParse(_aInput.text, out byte a) == false)
        {
            a = 0;
        }

        (byte r, byte g, byte b) rgb = GetRGB();

        SetRGBAText(rgb.r, rgb.g, rgb.b, a);
        
        _onColorChanged?.Invoke(new Color32(rgb.r, rgb.g, rgb.b, a));
    }

    public void OnChangeHex()
    {
        Color32 color32 = Color.clear;

        string hex = _hexInput.text.Trim();

        if (!hex.StartsWith("#"))
        {
            hex = "#" + hex;
        }

        if (ColorUtility.TryParseHtmlString(hex, out Color color))
        {
            color32 = color;
        }

        SetRGBAText(color32.r, color32.g, color32.b, color32.a);

        Color.RGBToHSV(color32, out float h, out float s, out float v);

        SetHueCursor(h);
        SetSVCursor(s, v);

        _onColorChanged?.Invoke(color32);
    }

    public void OnChangeSV(BaseEventData eventData)
    {
        PointerEventData pointerEventData = (PointerEventData)eventData;

        GameObject target = pointerEventData.pointerPress != null ? pointerEventData.pointerPress : pointerEventData.pointerEnter;

        RectTransform rectTransform = target.GetComponent<RectTransform>();

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform,
            pointerEventData.position,
            pointerEventData.pressEventCamera,
            out Vector2 localPosition
        );

        Rect rect = rectTransform.rect;

        float u = Mathf.Clamp01((localPosition.x - rect.xMin) / rect.width);
        float v = Mathf.Clamp01((localPosition.y - rect.yMin) / rect.height);

        SetSVCursor(u, v);

        float h = GetHue();

        Color32 color = Color.HSVToRGB(h, u, v);
        color.a = GetAlpha();
        
        SetRGBAText(color.r, color.g, color.b, color.a);

        _onColorChanged?.Invoke(color);
    }

    public void OnChangeHue(BaseEventData eventData)
    {
        PointerEventData pointerEventData = (PointerEventData)eventData;

        GameObject target = pointerEventData.pointerPress != null ? pointerEventData.pointerPress : pointerEventData.pointerEnter;

        RectTransform rectTransform = target.GetComponent<RectTransform>();

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform,
            pointerEventData.position,
            pointerEventData.pressEventCamera,
            out Vector2 localPosition
        );

        Rect rect = rectTransform.rect;

        float hue = Mathf.Clamp01((localPosition.y - rect.yMin) / rect.height);

        SetHueCursor(hue);

        (float s, float v) sv = GetSV();

        Color32 color = Color.HSVToRGB(hue, sv.s, sv.v);
        color.a = GetAlpha();

        SetRGBAText(color.r, color.g, color.b, color.a);

        _onColorChanged?.Invoke(color);
    }

    public void SetColor(Color32 color)
    {
        SetRGBAText(color.r, color.g, color.b, color.a);

        Color.RGBToHSV(color, out float h, out float s, out float v);

        SetHueCursor(h);
        SetSVCursor(s, v);
    }
}
