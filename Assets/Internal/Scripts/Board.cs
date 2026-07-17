using System;
using UnityEngine;
using Random = UnityEngine.Random;

public class Board : MonoBehaviour
{
    public int width;
    public int height;
    public TileBackground[,] _allTiles;
    public GameObject[,] _allItems;
    
    [SerializeField] private GameObject _tilePrefab;
    [SerializeField] private GameObject[] _dotsPrefabs;
    
    void Start()
    {
        _allTiles = new TileBackground[width, height];
        _allItems = new GameObject[width, height];
        
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
                _allItems[i, j] = newDot;
            }
        }
    }
    
    // Метод для проверки совпадений (заглушка)
    public void CheckMatches(Item dotToCheck)
    {
        GameObject leftDot1 = _allItems[dotToCheck.column - 1, dotToCheck.row];
        GameObject leftDot2 = _allItems[dotToCheck.column - 2, dotToCheck.row];
    }
}
