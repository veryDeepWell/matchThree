using UnityEngine;

[CreateAssetMenu(fileName = "LevelData", menuName = "Game/Level Data")]
public class LevelData : ScriptableObject
{
    public int width = 8;
    public int height = 8;
    public ItemTypes[] items; // 1D массив для простоты сериализации
    
    public ItemTypes GetItem(int column, int row)
    {
        if (column < 0 || column >= width || row < 0 || row >= height)
            return ItemTypes.DotRed;
            
        int index = row * width + column;
        if (index >= items.Length)
            return ItemTypes.DotRed;
            
        return items[index];
    }
    
    public void SetItem(int column, int row, ItemTypes type)
    {
        int index = row * width + column;
        if (index < items.Length)
            items[index] = type;
    }
}