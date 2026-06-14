using System;
using System.Collections;
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
    private Image _boardSubImage;

    [SerializeField]
    private RectTransform _boardRectTransform;

    [SerializeField]
    private Transform _palettes;

    private Board _board;
    private Board _boardSub;

    private Transform _selectedColor = null;

    // アロケート回避用
    private static Color32[] _pixels = null;

    // ディゾルブ用
    private Material _boardSubMaterialInstance;
    private Coroutine _dissolveCoroutine = null;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (SceneTransitionManager.Instance.TryGet("board", out Board board))
        {
            _board = board;
        }
        else
        {
            _board = new Board(_problemSource.bytes);
        }
        _boardSub = new Board();
        //Debug.Log(_board);

        Apply(_board, _boardImage);

        _boardSubMaterialInstance = Instantiate(_boardSubImage.material);
        _boardSubImage.material = _boardSubMaterialInstance;

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

            if (_boardRectTransform.rect.Contains(localPosition) == false || _dissolveCoroutine != null)
            {
                // Board外で離したまたはアニメーション中
                _selectedColor.GetComponent<Palette>().State = Palette.STATE.CANCEL;
            }
            else
            {
                // Board上で離した
                int x = (int)(localPosition.x + _boardRectTransform.rect.width / 2.0f);
                int y = (int)(_boardRectTransform.rect.height - (localPosition.y + _boardRectTransform.rect.height / 2.0f));

                Debug.Log($"( {x}, {y} )");

                /*
                byte[,] map = new byte[_board.Height][];
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
                // バックアップとる
                _boardSub.Copy(_board);

                // 実際に塗る
                Palette palette = _selectedColor.GetComponent<Palette>();
                _board.PaintArea(x, y, (byte)(palette.Index));
                Apply(_board, _boardImage);

                // Mapを更新
                _board.CalcAreaMap(x, y);

                // Mapのとこだけapply
                Apply(_boardSub, _boardSubImage, _board.Map);
                //_boardSubImage.gameObject.SetActive(true);

                _dissolveCoroutine = StartCoroutine(DoDissolve(new Vector2((float)x / _board.Width, (float)y / _board.Height)));

                // 色更新
                RefreshColor(_selectedColor);
            }

            // 選択解除
            _selectedColor.GetChild(0).localPosition = Vector2.zero;
            _selectedColor = null;
        }
        else
        {
            /*
            Transform cursorTransform = _selectedColor.GetChild(0);
            RectTransform cursorRectTransform = cursorTransform.GetComponent<RectTransform>();
            Vector2 cursorSize = cursorRectTransform.rect.size;
            cursorTransform.position = GetPointerPosition() + new Vector2(cursorSize.x * 0.5f + 16.0f, cursorSize.y * 0.5f + 16.0f);
            */
            _selectedColor.GetComponent<Palette>().SetCursorPosition(GetPointerPosition());
        }
    }

    public static void Apply(Board board, Image image, in byte[,] map = null)
    {
        if(image.sprite == null || image.sprite.texture == null || image.sprite.texture.width != board.Width || image.sprite.texture.height != board.Height)
        {
            Texture2D texture = new Texture2D(board.Width, board.Height, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;

            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0, 0, board.Width, board.Height),
                new Vector2(0.5f, 0.5f), 100f
            );

            image.sprite = sprite;
            image.SetNativeSize();
        }

        if(_pixels == null || _pixels.Length != (board.Width * board.Height))
        {
            _pixels = new Color32[board.Width * board.Height];
        }

        for (int y = 0; y < board.Height; y++)
        {
            for (int x = 0; x < board.Width; x++)
            {
                byte paletteIndex = board.Matrix[y, x];

                // UnityのTexture2Dは左下原点なので上下反転
                int textureY = board.Height - 1 - y;

                if (map != null && map[y, x] == 0)
                {
                    _pixels[textureY * board.Width + x] = Color.clear;
                }
                else
                {
                    _pixels[textureY * board.Width + x] = board.Palette[paletteIndex];
                }
            }
        }

        image.sprite.texture.SetPixels32(_pixels);
        image.sprite.texture.Apply( false, false );
    }

    public void OnPointerDownPalette(BaseEventData eventData)
    {
        PointerEventData pointerEventData = (PointerEventData)eventData;


        Palette palette = pointerEventData.pointerEnter.transform.parent.GetComponent<Palette>();

        if(palette.State != Palette.STATE.IDLE)
        {
            return;
        }

        _selectedColor = palette.transform;
        palette.SetGrab();
    }

    private IEnumerator DoDissolve(Vector2 uv)
    {
        float startTime = Time.time;

        _boardSubImage.gameObject.SetActive(true);

        _boardSubImage.material.SetVector("_UV", uv);

        while (true)
        {
            float rate = Mathf.Min(1.0f, (Time.time - startTime) / 0.5f);

            _boardSubImage.material.SetFloat("_Emission", 1.0f - rate);

            if (0.99f < rate)
            {
                break;
            }

            yield return null;
        }

        _boardSubImage.gameObject.SetActive(false);

        _dissolveCoroutine = null;
    }

    private void RefreshColorAll()
    {
        HashSet<byte> validIndexHash = new();

        int height = _board.Matrix.GetLength(0);
        int width = _board.Matrix.GetLength(1);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                validIndexHash.Add(_board.Matrix[y, x]);
            }
        }

        List<byte> validIndexList = validIndexHash.ToList();

        foreach (Transform paletteTransform in _palettes)
        {
            int randomIndex = Random.Range(0, validIndexList.Count);
            byte index = validIndexList[randomIndex];
            validIndexList.RemoveAt(randomIndex);

            Palette palette = paletteTransform.GetComponent<Palette>();
            palette.SetColor(index, _board.Palette[index]);
        }
    }

    private void RefreshColor(Transform paletteTransform)
    {
        HashSet<byte> validIndexHash = new();

        int height = _board.Matrix.GetLength(0);
        int width = _board.Matrix.GetLength(1);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                validIndexHash.Add(_board.Matrix[y, x]);
            }
        }

        List<byte> validIndexList = validIndexHash.ToList();

        foreach (Transform transform in _palettes)
        {
            if(transform == paletteTransform)
            {
                continue;
            }

            validIndexList.Remove((byte)transform.GetComponent<Palette>().Index);
        }

        validIndexList.Remove((byte)paletteTransform.GetComponent<Palette>().Index);

        if (validIndexList.Count == 0)
        {
            Palette palette = paletteTransform.GetComponent<Palette>();
            palette.State = Palette.STATE.EMPTY;
        }
        else
        {
            int randomIndex = Random.Range(0, validIndexList.Count);
            byte index = validIndexList[randomIndex];

            Palette palette = paletteTransform.GetComponent<Palette>();
            palette.SetColor(index, _board.Palette[index]);
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

    public void OnBack()
    {
        SceneTransitionManager.Instance.PopScene();
    }

    public void OnDestroy()
    {
        if(_dissolveCoroutine != null)
        {
            StopCoroutine(_dissolveCoroutine);
            _dissolveCoroutine = null;
        }
    }
}