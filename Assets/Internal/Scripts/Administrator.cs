using UnityEngine;

public class Administrator : MonoBehaviour
{
    public MatchesHandler matchesHandler;
    public ItemGenerator itemGenerator;
    public Board board;

    private void Start()
    {
        if (matchesHandler == null)
        {
            Debug.LogError($"{name}: No matchesHandler found!");
            matchesHandler = FindObjectOfType<MatchesHandler>();
        }

        if (itemGenerator == null)
        {
            Debug.LogError($"{name}: No itemGenerator found!");
            itemGenerator = FindObjectOfType<ItemGenerator>();
        }

        if (board == null)
        {
            Debug.LogError($"{name}: No board found!");
            board = FindObjectOfType<Board>();
        }
    }
}