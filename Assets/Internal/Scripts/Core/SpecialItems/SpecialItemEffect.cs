using System.Collections.Generic;
using UnityEngine;

public abstract class SpecialItemEffect : ScriptableObject
{
    [Header("Visual")] public Sprite Icon;
    public Color Color = Color.white;

    [Header("Effects")] public GameObject ActivationEffect;
    public AudioClip ActivationSound;

    [Header("Settings")] public bool TriggerOtherSpecialItems = true;

    public abstract void Execute(Board board, int column, int row);

    protected void AddTarget(Board board, int x, int y, HashSet<Item> items, HashSet<SpecialCell> cells,
        bool ignoreSpecialCells = false)
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
        // === ВИЗУАЛИЗАЦИЯ УДАЛЯЕМЫХ ПРЕДМЕТОВ (КРАСНЫЙ КВАДРАТ) ===
        foreach (var item in items)
        {
            if (item != null)
            {
                // Создаём красный квадрат поверх предмета
                GameObject debugSquare = GameObject.CreatePrimitive(PrimitiveType.Quad);
                debugSquare.transform.position = item.transform.position;
                debugSquare.transform.localScale = Vector3.one * 0.9f;
                debugSquare.transform.parent = item.transform;
                
                SpriteRenderer sr = debugSquare.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    // Создаём текстуру красного квадрата
                    Texture2D tex = new Texture2D(1, 1);
                    tex.SetPixel(0, 0, new Color(1f, 0f, 0f, 0.5f));
                    tex.Apply();
                    sr.sprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), Vector2.zero);
                    sr.sortingOrder = 999;
                }
                
                // Уничтожаем через 0.3 секунды
                Object.Destroy(debugSquare, 0.3f);
            }
        }

        // === УДАЛЕНИЕ ===
        foreach (var item in items)
        {
            if (item != null)
            {
                if (TriggerOtherSpecialItems && !string.IsNullOrEmpty(item.SpecialItemId))
                {
                    item.GetComponent<ISpecialItem>()?.TriggerSpecialItem();
                }
                else
                {
                    board.SetItemId(item.Column, item.Row, "");
                    board.Items[item.Column, item.Row] = null;
                    Object.Destroy(item.gameObject);
                }
            }
        }
    
        foreach (var cell in cells)
        {
            if (cell != null && cell.IsDestroyableBySpecial())
                cell.TakeDamage(999);
        }
    }

#if UNITY_EDITOR
    public virtual void DrawGizmos(Vector3 position) {}
#endif
}