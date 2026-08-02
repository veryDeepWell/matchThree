using System.Collections.Generic;
using UnityEngine;

public abstract class SpecialItemEffect : ScriptableObject
{
    [Header("Visual")]
    public Sprite Icon;
    public Color Color = Color.white;
    
    [Header("Effects")]
    public GameObject ActivationEffect;
    public AudioClip ActivationSound;
    
    [Header("Settings")]
    public bool TriggerOtherSpecialItems = true;
    
    public abstract void Execute(Board board, int column, int row);
    
    protected void AddTarget(Board board, int x, int y, HashSet<Item> items, HashSet<SpecialCell> cells, bool ignoreSpecialCells = false)
    {
        if (x < 0 || x >= board.Width || y < 0 || y >= board.Height) return;
        if (!board.IsCellActive(x, y)) return;
        
        if (!ignoreSpecialCells)
        {
            var cell = board.GetSpecialCell(x, y);
            if (cell != null && cell.IsDestroyableBySpecial())
            {
                cells.Add(cell);
                return;
            }
        }
        
        var item = board.Items[x, y];
        if (item != null)
            items.Add(item);
    }
    
    protected void RemoveTargets(Board board, HashSet<Item> items, HashSet<SpecialCell> cells)
    {
        foreach (var item in items)
        {
            if (item != null)
            {
                // Если спец-предмет и разрешено триггерить — активируем
                if (TriggerOtherSpecialItems && !string.IsNullOrEmpty(item.SpecialItemId))
                {
                    item.GetComponent<ISpecialItem>()?.TriggerSpecialItem();
                }
                else
                {
                    board.SetItemId(item.Column, item.Row, "");
                    board.Items[item.Column, item.Row] = null;
                    Destroy(item.gameObject);
                }
            }
        }
        
        foreach (var cell in cells)
        {
            if (cell != null && cell.IsDestroyableBySpecial())
                cell.TakeDamage(999);
        }
    }
}