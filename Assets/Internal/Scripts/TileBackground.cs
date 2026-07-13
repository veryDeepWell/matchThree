using System;
using UnityEngine;
using Random = UnityEngine.Random;

public class TileBackground : MonoBehaviour
{
    [SerializeField] private GameObject[] _dotsPrefabs;

    private void Start()
    {
        Initialize();
    }

    private void Initialize()
    {
        int dotToUse = Random.Range(0, _dotsPrefabs.Length);
        GameObject newDot = Instantiate(_dotsPrefabs[dotToUse], transform.position, Quaternion.identity, transform);
        newDot.name = this.gameObject.name + "_" + _dotsPrefabs[dotToUse].name;
    }
}
