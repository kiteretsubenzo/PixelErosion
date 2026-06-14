using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Palette : MonoBehaviour
{
    [SerializeField]
    private GameController _gameController;

    [SerializeField]
    private Image _baseColor;

    [SerializeField]
    private Image _cursor;

    [SerializeField]
    private RectTransform _cursorRectTransform;

    [SerializeField]
    private Animator _animator;

    public enum STATE
    {
        IDLE,
        REFRESH,
        GRAB,
        CANCEL,
        EMPTY
    }

    private STATE _state = STATE.IDLE;
    public STATE State
    {
        get { return _state; }
        set
        {
            switch (value)
            {
                case STATE.IDLE:
                    _baseColor.gameObject.SetActive(true);
                    _cursor.gameObject.SetActive(false);
                    break;
                case STATE.REFRESH:
                    _baseColor.gameObject.SetActive(true);
                    _cursor.gameObject.SetActive(false);
                    break;
                case STATE.GRAB:
                    _baseColor.gameObject.SetActive(false);
                    _cursor.gameObject.SetActive(true);
                    break;
                case STATE.CANCEL:
                    _baseColor.gameObject.SetActive(false);
                    _cursor.gameObject.SetActive(true);
                    _animationStartPosition = _cursor.transform.localPosition;
                    break;
                case STATE.EMPTY:
                    _baseColor.gameObject.SetActive(false);
                    _cursor.gameObject.SetActive(false);
                    break;
                default:
                    break;
            }

            _animationStartTime = Time.time;
            _state = value;
        }
    }

    private float _animationStartTime;
    private Vector2 _animationStartPosition;

    private int _index = -1;

    public int Index
    {
        get { return _index; }
    }

    public Color32 Color
    {
        get { return _baseColor.color; }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        switch(_state)
        {
            case STATE.IDLE:
                break;
            case STATE.REFRESH:
                {
                    float rate = Mathf.Min(1.0f, (Time.time - _animationStartTime) / 0.5f);
                    _baseColor.transform.localPosition = new Vector2(0.0f, -200.0f * (1.0f - rate));
                    if(0.999f < rate)
                    {
                        State = STATE.IDLE;
                    }
                }
                break;
            case STATE.GRAB:
                break;
            case STATE.CANCEL:
                {
                    float rate = Mathf.Min(1.0f, (Time.time - _animationStartTime) / 0.2f);
                    _cursor.transform.localPosition = _animationStartPosition * (1.0f - rate);
                    if (0.999f < rate)
                    {
                        State = STATE.IDLE;
                    }
                }
                break;
            case STATE.EMPTY:
                break;
            default:
                break;
        }
    }

    public void SetColor(int index, Color32 color)
    {
        _index = index;
        _baseColor.color = color;
        _cursor.color = color;

        State = STATE.REFRESH;
    }

    public void SetGrab()
    {
        State = STATE.GRAB;
    }

    public void CancelGrab()
    {
        State = STATE.CANCEL;
    }

    public void SetCursorPosition(Vector2 position)
    {
        Vector2 cursorSize = _cursorRectTransform.rect.size;
        _cursor.transform.position = position + new Vector2(cursorSize.x * 0.5f + 16.0f, cursorSize.y * 0.5f + 16.0f);
    }

    public Vector2 GetCursorPosition()
    {
        Vector2 cursorSize = _cursorRectTransform.rect.size;

        return (Vector2)_cursor.transform.position - new Vector2(cursorSize.x * 0.5f + 16.0f, cursorSize.y * 0.5f + 16.0f);
    }

    public void OnPointerDown(BaseEventData eventData)
    {
        if(_index < 0)
        {
            return;
        }

        _gameController.OnPointerDownPalette(eventData);
    }
}
