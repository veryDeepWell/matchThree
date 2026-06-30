using System;
using UnityEngine;

namespace Content.Scripts.Board
{
    public class BackgroundTile : MonoBehaviour
    {
        private void Start()
        {
            BoardSpawnManager spawnManager = BoardSpawnerSingleton.Instance.mySingleton;
            spawnManager.SpawnItemOnTile(gameObject);
        }
    }
}