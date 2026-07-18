using UnityEngine;

public class Administrator : MonoBehaviour
{
    public MatchesHandler matchesHandler;
    public ItemGenerator itemGenerator;
    public Board board;

    private void Awake()
    {
        InitializeDependencies();
    }

    private void InitializeDependencies()
    {
        if (matchesHandler == null)
        {
            matchesHandler = FindFirstObjectByType<MatchesHandler>();
            if (matchesHandler == null)
            {
                Debug.LogError($"{name}: No MatchesHandler found!");
            }
        }

        if (itemGenerator == null)
        {
            itemGenerator = FindFirstObjectByType<ItemGenerator>();
            if (itemGenerator == null)
            {
                Debug.LogError($"{name}: No ItemGenerator found!");
            }
        }

        if (board == null)
        {
            board = FindFirstObjectByType<Board>();
            if (board == null)
            {
                Debug.LogError($"{name}: No Board found!");
            }
        }
    }
}