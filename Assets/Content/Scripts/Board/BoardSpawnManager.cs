using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Content.Scripts.Board
{
    public class BoardSpawnManager : MonoBehaviour
    {
        [Flags]
        private enum DebugFlags
        {
            None = 0,
            Spawn = 1 << 0,
            Miscellaneous = 1 << 1,
            Everything = Spawn | Miscellaneous
        }
        [SerializeField] private DebugFlags debugFlags = DebugFlags.Everything;
        
        [SerializeField] private GameObject[] itemPrefabs;
        
        public bool SpawnItemOnTile(GameObject tileToSpawn)
        {
            if (HasFlag(DebugFlags.Spawn))
            {
                Debug.Log("BoardSpawnManager - SpawnItemOnTile - Spawning item began");
            }
            
            int randomItemNumber = Random.Range(0, itemPrefabs.Length);
            GameObject newItem = Instantiate(itemPrefabs[randomItemNumber], transform.position, Quaternion.identity);
            newItem.transform.SetParent(tileToSpawn.transform, false);
            newItem.name = "Item (" + itemPrefabs[randomItemNumber] + " ";

            if (newItem != null)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private bool HasFlag(DebugFlags flags)
        {
            return (debugFlags & flags) != 0;
        }
    }
}