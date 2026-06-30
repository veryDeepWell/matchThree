using Content.Scripts.Board;
using UnityEngine;

public class BoardSpawnerSingleton : MonoBehaviour
{
    // Object itself
    [SerializeField] public BoardSpawnManager mySingleton;

    // Singleton
    public static BoardSpawnerSingleton Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(this);
        }
        else
        {
            Destroy(this);
        }
    }
}
