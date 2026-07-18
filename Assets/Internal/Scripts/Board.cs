using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class Board : MonoBehaviour
{
    public int width;
    public int height;
    public Item[,] _allItems;
    


    void Start()
    {
        _allItems = new Item[width, height];
        
    }

    public void CheckMatches()
    {
        StartCoroutine(ProcessMatches());
    }
}