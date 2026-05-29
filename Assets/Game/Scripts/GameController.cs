using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class GameController : MonoBehaviour
{
    [SerializeField]
    private TextAsset _problemSource;

    [SerializeField]
    private Image _boardImage;

    [SerializeField]
    private RectTransform _boardRectTransform;

    [SerializeField]
    private Transform _palettes;

    private Board _board;

    private Transform _selectedColor = null;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _board = new Board(_problemSource.bytes);
        //Debug.Log(_board);

        NewApply(_boardImage);

        RefreshColorAll();
    }

    // Update is called once per frame
    void Update()
    {
        if (_selectedColor == null)
        {
            return;
        }

        // 離した
        if (IsPointerDown() == false)
        {
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
            }
            else
            {
                // Board上で離した
                int x = (int)(localPosition.x + _boardRectTransform.rect.width / 2.0f);
                int y = (int)(_boardRectTransform.rect.height - (localPosition.y + _boardRectTransform.rect.height / 2.0f));

                Debug.Log($"( {x}, {y} )");

                /*
                byte[][] map = new byte[_board.Height][];
                for (int i = 0; i < _board.Height; i++)
                {
                    map[i] = new byte[_board.Width];
                }

                if (_board.CalcAreaMap(x, y, ref map))
                {
                    System.Text.StringBuilder stringBuilder = new System.Text.StringBuilder();
                    for (int yy = 0; yy < map.Length; yy++)
                    {
                        stringBuilder.AppendLine(string.Join(" ", map[yy]));
                    }
                    Debug.Log(stringBuilder.ToString());
                }
                else
                {
                    Debug.Log("null");
                }
                */

                // 塗る
                Palette palette = _selectedColor.GetComponent<Palette>();
                _board.PaintArea(x, y, (byte)(palette.Index));
                Apply(_boardImage);

                // 色更新
                RefreshColor(_selectedColor);
            }

            // 選択解除
            _selectedColor.GetChild(0).localPosition = Vector2.zero;
            _selectedColor = null;
        }
        else
        {
            Transform cursorTransform = _selectedColor.GetChild(0);
            RectTransform cursorRectTransform = cursorTransform.GetComponent<RectTransform>();
            Vector2 cursorSize = cursorRectTransform.rect.size;
            cursorTransform.position = GetPointerPosition() + new Vector2(cursorSize.x * 0.5f + 16.0f, cursorSize.y * 0.5f + 16.0f);
        }
    }

    public void NewApply(Image image)
    {
        Texture2D texture = new Texture2D(_board.Width, _board.Height, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;

        Sprite sprite = Sprite.Create(
            texture,
            new Rect(0, 0, _board.Width, _board.Height),
            new Vector2(0.5f, 0.5f), 100f
        );

        image.sprite = sprite;
        image.SetNativeSize();

        Apply(image);
    }

    public void Apply(Image image)
    {
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

        _boardImage.sprite.texture.SetPixels32(pixels);
        _boardImage.sprite.texture.Apply( false, false );
    }

    public void OnPointerDownPalette(BaseEventData eventData)
    {
        PointerEventData pointerEventData = (PointerEventData)eventData;

        _selectedColor = pointerEventData.pointerEnter.transform.parent;
    }

    private void RefreshColorAll()
    {
        List<byte> validIndices = _board.Matrix.SelectMany(row => row).Distinct().ToList();

        foreach(Transform paletteTransform in _palettes)
        {
            int randomIndex = Random.Range(0, validIndices.Count);
            byte index = validIndices[randomIndex];
            validIndices.RemoveAt(randomIndex);

            Palette palette = paletteTransform.GetComponent<Palette>();
            palette.Index = index;
            palette.Color = _board.Palette[index];
        }
    }

    private void RefreshColor(Transform paletteTransform)
    {
        List<byte> validIndices = _board.Matrix.SelectMany(row => row).Distinct().ToList();

        foreach (Transform transform in _palettes)
        {
            if(transform == paletteTransform)
            {
                continue;
            }

            validIndices.Remove((byte)transform.GetComponent<Palette>().Index);
        }

        if (validIndices.Count == 0)
        {
            Palette palette = paletteTransform.GetComponent<Palette>();
            palette.Index = 0;
            palette.Color = _board.Palette[0];
        }
        else
        {
            int randomIndex = Random.Range(0, validIndices.Count);
            byte index = validIndices[randomIndex];

            Palette palette = paletteTransform.GetComponent<Palette>();
            palette.Index = index;
            palette.Color = _board.Palette[index];
        }
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