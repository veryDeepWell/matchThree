using UnityEngine;

public class Board : MonoBehaviour
{
    public int width;
    public int height;
    private TileBackground[,] _allTiles;
    
    [SerializeField] private GameObject _tilePrefab;
    
    void Start()
    {
        _allTiles = new TileBackground[width, height];
        transform.position = new Vector3(width / -2, height / -2, 0);
        Setup();
    }

    private void Setup()
    {
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                Vector2 tilePos = new Vector2(i, j);
                GameObject newTile = Instantiate(_tilePrefab, tilePos, Quaternion.identity, transform);
                newTile.name = "(" + i + "," + j + ")";
            }
        }
    }
}
