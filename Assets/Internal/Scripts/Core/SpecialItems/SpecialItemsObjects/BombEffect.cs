using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BombEffect", menuName = "Special Effects/Bomb")]
public class BombEffect : SpecialItemEffect
{
    [Header("Bomb Settings")] [SerializeField]
    private int _radius = 1;

    public int Radius => _radius;

    public override void Execute(Board board, int column, int row)
    {
        if (board == null) return;

        var itemsToRemove = new HashSet<Item>();
        var cellsToRemove = new HashSet<SpecialCell>();

        Debug.Log($"[BombEffect] Explosion at ({column},{row}) with radius {_radius}");

        for (int x = column - _radius; x <= column + _radius; x++)
        {
            for (int y = row - _radius; y <= row + _radius; y++)
            {
                // Проверяем границы
                if (x < 0 || x >= board.Width || y < 0 || y >= board.Height)
                {
                    Debug.Log($"[BombEffect] Skipping ({x},{y}) - out of bounds");
                    continue;
                }

                // Проверяем активность ячейки
                if (!board.IsCellActive(x, y))
                {
                    Debug.Log($"[BombEffect] Skipping ({x},{y}) - inactive cell");
                    continue;
                }

                var item = board.Items[x, y];
                var cell = board.GetSpecialCell(x, y);

                if (cell != null && cell.IsDestroyableBySpecial())
                {
                    cellsToRemove.Add(cell);
                    Debug.Log($"[BombEffect] Cell at ({x},{y}) will be destroyed");
                }
                else if (item != null)
                {
                    itemsToRemove.Add(item);
                    Debug.Log(
                        $"[BombEffect] Item at ({x},{y}) will be destroyed - ID: {item.ItemId}, Special: {item.SpecialItemId}");
                }
                else
                {
                    Debug.Log($"[BombEffect] Nothing at ({x},{y})");
                }
            }
        }

        Debug.Log($"[BombEffect] Removing {itemsToRemove.Count} items and {cellsToRemove.Count} cells");
        RemoveTargets(board, itemsToRemove, cellsToRemove);
    }
}