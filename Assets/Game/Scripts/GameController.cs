using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.UI;

public class GameController : MonoBehaviour
{
    [SerializeField]
    private TextAsset _problemSource;

    [SerializeField]
    private Image _boardImage;

    [SerializeField]
    private RectTransform _boardRectTransform;

    [SerializeField]
    private Transform _palette;

    private Board _board;

    private Transform _selectedColor = null;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _board = new Board(_problemSource.bytes);
        //Debug.Log(_board);

        Apply();
    }

    // Update is called once per frame
    void Update()
    {
        if (_selectedColor == null)
        {
            return;
        }

        if(IsPointerDown() == false)
        {
            // 離した
            _selectedColor.GetChild(0).localPosition = Vector2.zero;
            _selectedColor = null;

            // Board上で離したか
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _boardRectTransform,
                GetPointerPosition(),
                null,
                out Vector2 localPosition
            );

            if (_boardRectTransform.rect.Contains(localPosition) == false)
            {
                // Board外で離した
                Debug.Log($"out");
                return;
            }

            // Board上で離した
            int x = (int)(localPosition.x + _boardRectTransform.rect.width / 2.0f);
            int y = (int)(_boardRectTransform.rect.height - (localPosition.y + _boardRectTransform.rect.height / 2.0f));

            Debug.Log($"( {x}, {y} )");
        }
        else
        {
            Transform cursorTransform = _selectedColor.GetChild(0);
            RectTransform cursorRectTransform = cursorTransform.GetComponent<RectTransform>();
            Vector2 cursorSize = cursorRectTransform.rect.size;
            cursorTransform.position = GetPointerPosition() + new Vector2(cursorSize.x * 0.5f + 16.0f, cursorSize.y * 0.5f + 16.0f);
        }
    }

    public void Apply()
    {
        Texture2D texture = new Texture2D(_board.Width, _board.Height, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;

        Color32[] pixels = new Color32[_board.Width * _board.Height];

        for (int y = 0; y < _board.Height; y++)
        {
            for (int x = 0; x < _board.Width; x++)
            {
                byte paletteIndex = _board.Matrix[y][x];

                // UnityのTexture2Dは左下原点なので上下反転
                int textureY = _board.Height - 1 - y;

                pixels[textureY * _board.Width + x] = _board.Palette[paletteIndex];
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply();

        Sprite sprite = Sprite.Create(
            texture,
            new Rect(0, 0, _board.Width, _board.Height),
            new Vector2(0.5f, 0.5f), 100f
        );

        _boardImage.sprite = sprite;
        _boardImage.SetNativeSize();
    }

    public void OnPointerDownPalette(BaseEventData eventData)
    {
        PointerEventData pointerEventData = (PointerEventData)eventData;

        _selectedColor = pointerEventData.pointerEnter.transform.parent;
    }

    private void RefreshColor(Transform colorTransform)
    {

    }

    public static bool IsPointerDown()
    {
        // タッチ優先
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
        {
            return true;
        }

        // マウス
        if (Mouse.current != null && Mouse.current.leftButton.isPressed)
        {
            return true;
        }

        return false;
    }

    public static Vector2 GetPointerPosition()
    {
        // タッチ優先
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
        {
            return Touchscreen.current.primaryTouch.position.ReadValue();
        }

        // マウス
        if (Mouse.current != null)
        {
            return Mouse.current.position.ReadValue();
        }

        return Vector2.zero;
    }
}