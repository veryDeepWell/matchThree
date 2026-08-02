using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BombEffect", menuName = "Special Effects/Bomb")]
public class BombEffect : SpecialItemEffect
{
    [Header("Bomb Settings")]
    [SerializeField] private int _radius = 1; // 1 = 3x3, 2 = 5x5, 3 = 7x7
    
    public int Radius => _radius;
    
    public override void Execute(Board board, int column, int row)
    {
        if (board == null) return;
        
        var itemsToRemove = new HashSet<Item>();
        var cellsToRemove = new HashSet<SpecialCell>();
        
        // Взрыв NxN
        for (int x = column - _radius; x <= column + _radius; x++)
        {
            for (int y = row - _radius; y <= row + _radius; y++)
            {
                AddTarget(board, x, y, itemsToRemove, cellsToRemove);
            }
        }
        
        // Удаляем цели
        RemoveTargets(board, itemsToRemove, cellsToRemove);
    }
}