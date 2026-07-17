using System;
using UnityEngine;
using Random = UnityEngine.Random;

public class Board : MonoBehaviour
{
    public int width;
    public int height;
    public TileBackground[,] _allTiles;
    public Item[,] _allItems;
    
    [SerializeField] private GameObject _tilePrefab;
    [SerializeField] private GameObject[] _dotsPrefabs;
    
    void Start()
    {
        _allTiles = new TileBackground[width, height];
        _allItems = new Item[width, height];
        
        Setup();
        
        //transform.position = new Vector3(-width / 2, -height / 2, 0);
    }

    private void Setup()
    {
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                Vector2 tilePos = new Vector2(i, j);
                
                // Создаём фон
                GameObject newTile = Instantiate(_tilePrefab, tilePos, Quaternion.identity, transform);
                newTile.name = "Tile(" + i + "," + j + ")";
                
                // Создаём предмет
                int dotToUse = Random.Range(0, _dotsPrefabs.Length);
                GameObject newDot = Instantiate(_dotsPrefabs[dotToUse], tilePos, Quaternion.identity, newTile.transform);
                newDot.name = "Item(" + i + "," + j + ")";
                
                // Получаем компонент Item и устанавливаем координаты
                Item itemComponent = newDot.GetComponent<Item>();
                if (itemComponent != null)
                {
                    itemComponent.column = i;
                    itemComponent.row = j;
                    itemComponent.board = this;
                }
                else
                {
                    Debug.LogError($"Item component not found on {newDot.name}");
                }
                
                // Сохраняем в массив
                _allItems[i, j] = itemComponent;
            }
        }
    }
    
    // Метод для проверки совпадений (заглушка)
    public void CheckMatches(Item dotToCheck)
    {
        if (dotToCheck.column < 0 || dotToCheck.column >= width) { return; }
        
        Item leftDot1 = _allItems[dotToCheck.column - 1, dotToCheck.row].GetComponent<Item>();
        if (leftDot1._itemType != dotToCheck._itemType) { return; }
        
        Item leftDot2 = _allItems[dotToCheck.column - 2, dotToCheck.row].GetComponent<Item>();
        if (leftDot2._itemType != dotToCheck._itemType) { return; }
    }
}
