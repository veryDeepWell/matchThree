using System;
using UnityEngine;

namespace Content.Scripts.Board
{
    public class Board : MonoBehaviour
    {
        [SerializeField] private int gridHeight;
        [SerializeField] private int gridWidth;
        [SerializeField] private GameObject gridTilePrefab;
        private BackgroundTile[,] allTiles;

        private void Start()
        {
            allTiles = new BackgroundTile[gridWidth, gridHeight];

            Setup();
            MoveDumbass();
        }

        private void Setup()
        {
            for (int i = 0; i < gridWidth; i++)
            {
                for (int j = 0; j < gridHeight; j++)
                {
                    Vector2 tempPosition = new Vector2(i, j);
                    GameObject backgroundTile = Instantiate(gridTilePrefab, tempPosition, Quaternion.identity);
                    backgroundTile.transform.SetParent(transform, false);
                    backgroundTile.name = "Tile (" + i + "-" + j + ")";
                }
            }
        }

        private void MoveDumbass()
        {
            transform.position += new Vector3(-gridWidth / 2, -gridHeight / 2, 0);
        }
    }
}