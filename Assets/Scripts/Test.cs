using System;
using UnityEngine;

public class Test : MonoBehaviour
{
    [SerializeField]
    private TextAsset _problemSource;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Board board = new Board(_problemSource.bytes);
        Debug.Log(board);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    
}
