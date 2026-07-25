using UnityEngine;

[DefaultExecutionOrder(-100)]
public class Administrator : MonoBehaviour
{
    public MatchesHandler matchesHandler;
    public ItemGenerator itemGenerator;
    public Board board;
    public ItemHandler itemHandler;
    public SpecialItemHandler specialItemHandler;
    public LevelManager levelManager;

    private void Awake()
    {
        InitializeDependencies();
    }

    private void InitializeDependencies()
    {
        if (matchesHandler == null)
            matchesHandler = FindFirstObjectByType<MatchesHandler>();
        if (itemGenerator == null)
            itemGenerator = FindFirstObjectByType<ItemGenerator>();
        if (board == null)
            board = FindFirstObjectByType<Board>();
        if (itemHandler == null)
            itemHandler = FindFirstObjectByType<ItemHandler>();
        if (specialItemHandler == null)
            specialItemHandler = FindFirstObjectByType<SpecialItemHandler>();
        if (levelManager == null)
            levelManager = FindFirstObjectByType<LevelManager>();
    }
}