using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class Board : MonoBehaviour
{
    public int width;
    public int height;

    private Administrator _administrator;
    public Item[,] _allItems;

    void Start()
    {
        _administrator = FindAnyObjectByType<Administrator>().GetComponent<Administrator>();
        
        _allItems = new Item[width, height];
    }

    public void CheckMatches()
    {
        _administrator.matchesHandler.ProcessMatches();
    }
}