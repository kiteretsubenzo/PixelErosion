using UnityEngine;
using UnityEngine.EventSystems;

public class Palette : MonoBehaviour
{
    [SerializeField]
    private GameController _gameController;

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
