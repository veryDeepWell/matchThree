using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MatchesHandler : MonoBehaviour
{
    [SerializeField] private float _moveDuration = 0.15f;

    [Header("Animation Delays")] [SerializeField]
    private float _matchDelay = 0.15f; // Задержка перед удалением матчей

    [SerializeField] private float _dropDelay = 0.1f; // Задержка перед падением
    [SerializeField] private float _postDropDelay = 0.15f; // Задержка после падения
    [SerializeField] private float _bombExplosionDelay = 0.3f;

    public float GetBombExplosionDelay() => _bombExplosionDelay;

    public HashSet<int> FindMatches(Board board)
    {
        if (board?.Data == null) return new HashSet<int>();

        var data = board.Data;
        for (int x = 0; x < data.Width; x++)
        {
            for (int y = 0; y < data.Height; y++)
            {
                var item = board.Items[x, y];
                if (item != null && !string.IsNullOrEmpty(item.ItemId) && string.IsNullOrEmpty(item.SpecialItemId))
                    data.SetItem(x, y, item.ItemId);
                else
                    data.SetItem(x, y, "");
            }
        }

        return MatchFinder.FindMatches(data);
    }

    public void ProcessMatches(Board board)
    {
        if (board == null) return;
        StartCoroutine(ProcessMatchesCoroutine(board));
    }

    private IEnumerator ProcessMatchesCoroutine(Board board)
    {
        var matches = FindMatches(board);

        if (matches.Count == 0) yield break;

        // 1. Проверяем специальные предметы (4+ в ряд)
        CheckForSpecialItems(board, matches);

        // 2. Удаляем обычные предметы
        RemoveItems(board, matches);
        yield return new WaitForSeconds(_matchDelay);

        // 3. Падение
        DropItems(board);
        yield return new WaitForSeconds(_dropDelay + _postDropDelay);

        // 4. Рекурсивная проверка
        board.CheckMatches();
    }

    private void RemoveItems(Board board, HashSet<int> matches)
    {
        var data = board.Data;
        int w = data.Width;

        foreach (int idx in matches)
        {
            int x = idx % w;
            int y = idx / w;
            var item = board.Items[x, y];
            if (item)
            {
                if (!string.IsNullOrEmpty(item.SpecialItemId))
                {
                    Debug.Log($"[RemoveItems] Skipping special item '{item.SpecialItemId}' at ({x},{y})");
                    continue;
                }

                data.SetItem(x, y, "");
                board.Items[x, y] = null;
                Destroy(item.gameObject);
            }
        }
    }

    public void DropItems(Board board)
    {
        var data = board.Data;
        int w = data.Width;
        int h = data.Height;
        var generator = FindObjectOfType<ItemGenerator>();
        var handler = FindObjectOfType<ItemHandler>();

        ItemRegistry registry = null;
        if (handler != null)
        {
            registry = handler.GetRegistry();
        }

        for (int x = 0; x < w; x++)
        {
            int empty = 0;
            for (int y = 0; y < h; y++)
            {
                int idx = data.GetIndex(x, y);

                if (!data.ActiveCells[idx])
                {
                    empty = 0;
                    continue;
                }

                var currentItem = board.Items[x, y]; // ← переименовал с item на currentItem

                bool hasItem = currentItem != null &&
                               (!string.IsNullOrEmpty(currentItem.ItemId) ||
                                !string.IsNullOrEmpty(currentItem.SpecialItemId));

                if (!hasItem)
                {
                    empty++;
                }
                else if (empty > 0)
                {
                    int newY = y - empty;
                    int newIdx = data.GetIndex(x, newY);

                    if (data.ActiveCells[newIdx])
                    {
                        board.Items[x, y] = null;
                        board.Items[x, newY] = currentItem;
                        currentItem.Row = newY;

                        // Находим родительский Tile для новой позиции
                        Transform newParent = board.transform.Find($"Tile({x},{newY})");
                        if (newParent != null)
                        {
                            currentItem.transform.parent = newParent;
                        }

                        board.StartCoroutine(currentItem.MoveToPosition(x, newY));

                        data.Items[newIdx] = data.Items[idx];
                        data.Items[idx] = "";
                    }
                    else
                    {
                        empty = 0;
                    }
                }
            }

            int emptyCount = 0;
            for (int y = 0; y < h; y++)
            {
                int idx = data.GetIndex(x, y);
                if (!data.ActiveCells[idx]) continue;

                var checkItem = board.Items[x, y]; // ← переименовал
                bool hasItem = checkItem != null &&
                               (!string.IsNullOrEmpty(checkItem.ItemId) ||
                                !string.IsNullOrEmpty(checkItem.SpecialItemId));

                if (!hasItem) emptyCount++;
            }

            if (emptyCount > 0 && generator != null && handler != null && registry != null)
            {
                for (int y = h - emptyCount; y < h; y++)
                {
                    int idx = data.GetIndex(x, y);
                    if (!data.ActiveCells[idx]) continue;

                    var existingItem = board.Items[x, y];
                    bool hasItem = existingItem != null &&
                                   (!string.IsNullOrEmpty(existingItem.ItemId) ||
                                    !string.IsNullOrEmpty(existingItem.SpecialItemId));

                    if (hasItem) continue;

                    string type = registry.GetRandomNormalId();
                    if (string.IsNullOrEmpty(type)) continue;

                    Vector2 targetPos = board.GetWorldPosition(x, y);

                    // НАХОДИМ ИЛИ СОЗДАЁМ TILE
                    Transform tileParent = board.transform.Find($"Tile({x},{y})");
                    if (tileParent == null)
                    {
                        GameObject tile = Instantiate(generator.GetTilePrefab(), targetPos, Quaternion.identity,
                            board.transform);
                        tile.name = $"Tile({x},{y})";
                        tileParent = tile.transform;
                    }

                    // Создаём предмет ВНУТРИ TILE
                    Vector2 startPos = board.GetWorldPosition(x, h + 1);
                    GameObject go = handler.CreateItem(type, startPos, tileParent);
                    if (go == null) continue;

                    var newItem = go.GetComponent<Item>(); // ← переименовал с item на newItem
                    if (newItem == null) continue;

                    newItem.Column = x;
                    newItem.Row = y;
                    newItem.Board = board;
                    newItem.ItemId = type;

                    board.StartCoroutine(newItem.MoveToPosition(x, y));

                    board.Items[x, y] = newItem;
                    data.SetItem(x, y, type);
                }
            }
        }

        // Второй проход для заполнения пустот
        for (int x = 0; x < w; x++)
        {
            for (int y = 0; y < h; y++)
            {
                int idx = data.GetIndex(x, y);
                if (!data.ActiveCells[idx]) continue;

                var checkItem = board.Items[x, y];
                bool hasItem = checkItem != null &&
                               (!string.IsNullOrEmpty(checkItem.ItemId) ||
                                !string.IsNullOrEmpty(checkItem.SpecialItemId));

                if (!hasItem)
                {
                    string type = registry.GetRandomNormalId();
                    if (string.IsNullOrEmpty(type)) continue;

                    Vector2 pos = board.GetWorldPosition(x, y);

                    Transform tileParent = board.transform.Find($"Tile({x},{y})");
                    if (tileParent == null)
                    {
                        GameObject tile = Instantiate(generator.GetTilePrefab(), pos, Quaternion.identity,
                            board.transform);
                        tile.name = $"Tile({x},{y})";
                        tileParent = tile.transform;
                    }

                    GameObject go = handler.CreateItem(type, pos, tileParent);
                    if (go == null) continue;

                    var newItem = go.GetComponent<Item>();
                    if (newItem == null) continue;

                    newItem.Column = x;
                    newItem.Row = y;
                    newItem.Board = board;
                    newItem.ItemId = type;
                    board.Items[x, y] = newItem;
                    data.SetItem(x, y, type);
                }
            }
        }

        // В конце DropItems() после всех циклов:
        for (int x = 0; x < w; x++)
        {
            for (int y = 0; y < h; y++)
            {
                int idx = data.GetIndex(x, y);
                if (!data.ActiveCells[idx]) continue;

                var checkItem = board.Items[x, y];
                bool hasItem = checkItem != null &&
                               (!string.IsNullOrEmpty(checkItem.ItemId) ||
                                !string.IsNullOrEmpty(checkItem.SpecialItemId));

                if (!hasItem)
                {
                    // Заполняем пустоту
                    string type = registry.GetRandomNormalId();
                    if (string.IsNullOrEmpty(type)) continue;

                    Vector2 pos = board.GetWorldPosition(x, y);

                    Transform tileParent = board.transform.Find($"Tile({x},{y})");
                    if (tileParent == null)
                    {
                        // Если тайла нет — создаём
                        GameObject tile = Instantiate(generator.GetTilePrefab(), pos, Quaternion.identity,
                            board.transform);
                        tile.name = $"Tile({x},{y})";
                        tileParent = tile.transform;
                    }

                    GameObject go = handler.CreateItem(type, pos, tileParent);
                    if (go == null) continue;

                    var newItem = go.GetComponent<Item>();
                    if (newItem == null) continue;

                    newItem.Column = x;
                    newItem.Row = y;
                    newItem.Board = board;
                    newItem.ItemId = type;
                    board.Items[x, y] = newItem;
                    data.SetItem(x, y, type);
                }
            }
        }
    }

    private void CheckForSpecialItems(Board board, HashSet<int> matches)
    {
        if (board?.Data == null || matches == null) return;

        var (swapX, swapY) = board.GetLastSwapPosition();
        board.ClearLastSwapPosition();

        var data = board.Data;
        int w = data.Width;

        var horizontalMatches = new Dictionary<int, List<int>>();
        var verticalMatches = new Dictionary<int, List<int>>();

        foreach (int idx in matches)
        {
            int x = idx % w;
            int y = idx / w;

            if (!horizontalMatches.ContainsKey(y))
                horizontalMatches[y] = new List<int>();
            horizontalMatches[y].Add(x);

            if (!verticalMatches.ContainsKey(x))
                verticalMatches[x] = new List<int>();
            verticalMatches[x].Add(y);
        }

        // Горизонтальные матчи
        foreach (var kvp in horizontalMatches)
        {
            int y = kvp.Key;
            var xs = kvp.Value;
            xs.Sort();

            int consecutive = 1;
            int startX = xs[0];

            for (int i = 1; i < xs.Count; i++)
            {
                if (xs[i] == xs[i - 1] + 1)
                {
                    consecutive++;
                    if (consecutive >= 4)
                    {
                        // Определяем позицию для бомбы
                        int createX;
                        int createY = y;

                        // Если позиция свапа в этом ряду — создаём там!
                        if (swapX >= 0 && swapY == y && xs.Contains(swapX))
                        {
                            createX = swapX;
                            Debug.Log($"[CheckForSpecialItems] Bomb at swap position: ({createX},{createY})");
                        }
                        else
                        {
                            createX = startX + consecutive / 2;
                            Debug.Log($"[CheckForSpecialItems] Bomb at center: ({createX},{createY})");
                        }

                        CreateBomb(board, createX, createY);
                        consecutive = 1;
                        startX = xs[i];
                    }
                }
                else
                {
                    consecutive = 1;
                    startX = xs[i];
                }
            }
        }

        // Вертикальные матчи (аналогично)
        foreach (var kvp in verticalMatches)
        {
            int x = kvp.Key;
            var ys = kvp.Value;
            ys.Sort();

            int consecutive = 1;
            int startY = ys[0];

            for (int i = 1; i < ys.Count; i++)
            {
                if (ys[i] == ys[i - 1] + 1)
                {
                    consecutive++;
                    if (consecutive >= 4)
                    {
                        int createX = x;
                        int createY;

                        if (swapX == x && swapY >= 0 && ys.Contains(swapY))
                        {
                            createY = swapY;
                            Debug.Log($"[CheckForSpecialItems] Bomb at swap position: ({createX},{createY})");
                        }
                        else
                        {
                            createY = startY + consecutive / 2;
                            Debug.Log($"[CheckForSpecialItems] Bomb at center: ({createX},{createY})");
                        }

                        CreateBomb(board, createX, createY);
                        consecutive = 1;
                        startY = ys[i];
                    }
                }
                else
                {
                    consecutive = 1;
                    startY = ys[i];
                }
            }
        }
    }

    private void CreateBomb(Board board, int x, int y)
    {
        var generator = FindObjectOfType<ItemGenerator>();
        if (generator == null)
        {
            Debug.LogError("[CreateBomb] ItemGenerator not found!");
            return;
        }

        Debug.Log($"[CreateBomb] Creating bomb at ({x},{y})");
        generator.ReplaceWithSpecial(board, x, y, "bomb");
    }
}