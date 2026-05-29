using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Palette : MonoBehaviour
{
    [SerializeField]
    private GameController _gameController;

    [SerializeField]
    private Image _baseColor;

    private int _index = -1;

    public int Index
    {
        get { return _index; }
        set { _index = value; }
    }

    public Color32 Color
    {
        get { return _baseColor.color; }
        set { _baseColor.color = value; }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnPointerDown(BaseEventData eventData)
    {
        _gameController.OnPointerDownPalette(eventData);
    }
}
