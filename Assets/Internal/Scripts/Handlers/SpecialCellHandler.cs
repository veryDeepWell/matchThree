using System.Collections.Generic;
using UnityEngine;

public class SpecialCellHandler : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ItemRegistry _registry;
    [SerializeField] private GameObject _cellPrefab;

    [Header("Cell Data")]
    [SerializeField] private List<SpecialCellData> _cellDataList = new List<SpecialCellData>();

    private Dictionary<int, SpecialCellData> _dataByIndex;
    private Dictionary<int, ItemDefinition> _definitionByIndex;

    public List<SpecialCellData> GetAllCellData() => _cellDataList;

    private void Awake()
    {
        BuildCellDictionary();
    }

    public SpecialCellData GetCellData(int typeIndex)
    {
        EnsureDictionaries();
        return typeIndex > 0 && _dataByIndex.TryGetValue(typeIndex, out var data) ? data : null;
    }

    public ItemDefinition GetCellDefinition(int typeIndex)
    {
        EnsureDictionaries();
        return typeIndex > 0 && _definitionByIndex.TryGetValue(typeIndex, out var definition)
            ? definition
            : null;
    }

    public int GetCellTypeIndex(SpecialCellData data)
    {
        EnsureDictionaries();
        if (data == null) return 0;

        foreach (var pair in _dataByIndex)
        {
            if (pair.Value == data)
                return pair.Key;
        }

        return 0;
    }

    public GameObject CreateCell(int typeIndex, Vector2 position, Transform parent)
    {
        if (_cellPrefab == null)
        {
            Debug.LogError("[SpecialCellHandler] Cell prefab is not assigned.");
            return null;
        }

        if (GetCellData(typeIndex) == null)
        {
            Debug.LogError($"[SpecialCellHandler] Unknown special cell type index: {typeIndex}");
            return null;
        }

        var cellObject = Instantiate(_cellPrefab, position, Quaternion.identity, parent);
        cellObject.name = $"SpecialCell_{typeIndex}";
        return cellObject;
    }

    public SpecialCell InitializeCell(GameObject cellObject, int typeIndex, Board board, int column, int row)
    {
        if (cellObject == null) return null;

        var data = GetCellData(typeIndex);
        if (data == null) return null;

        var cell = cellObject.GetComponent<SpecialCell>() ?? cellObject.AddComponent<SpecialCell>();
        cell.Initialize(data, GetCellDefinition(typeIndex), typeIndex, board, column, row);
        return cell;
    }

    private void EnsureDictionaries()
    {
        if (_dataByIndex == null || _definitionByIndex == null)
            BuildCellDictionary();
    }

    private void BuildCellDictionary()
    {
        _dataByIndex = new Dictionary<int, SpecialCellData>();
        _definitionByIndex = new Dictionary<int, ItemDefinition>();

        if (_registry == null)
            _registry = Resources.Load<ItemRegistry>("ItemRegistry");

        if (_registry != null)
        {
            _registry.Initialize();
            if (_cellDataList.Count == 0)
            {
                foreach (var definition in _registry.GetSpecialCells())
                {
                    if (definition != null && definition.CellData != null)
                        _cellDataList.Add(definition.CellData);
                }
            }
        }

        for (int index = 0; index < _cellDataList.Count; index++)
        {
            var data = _cellDataList[index];
            if (data == null) continue;

            int typeIndex = index + 1;
            _dataByIndex[typeIndex] = data;

            if (_registry == null) continue;

            foreach (var definition in _registry.GetSpecialCells())
            {
                if (definition != null && definition.CellData == data)
                {
                    _definitionByIndex[typeIndex] = definition;
                    break;
                }
            }
        }
    }

    public void DamageAround(Board board, IEnumerable<int> affectedIndices, int damage = 1)
    {
        if (board == null || affectedIndices == null || damage <= 0)
            return;

        if (board.Width <= 0 || board.Height <= 0) return;

        var damagedCells = new HashSet<SpecialCell>();
        int width = board.Width;

        // Only the matched cell itself and its orthogonal neighbours.
        // Diagonal matches must NOT damage special cells.
        int[] offsetX = { 0, -1, 1, 0, 0 };
        int[] offsetY = { 0, 0, 0, -1, 1 };

        foreach (int index in affectedIndices)
        {
            if (index < 0 || index >= board.Width * board.Height)
                continue;

            int column = index % width;
            int row = index / width;

            for (int i = 0; i < offsetX.Length; i++)
            {
                var cell = board.GetSpecialCell(column + offsetX[i], row + offsetY[i]);
                if (cell != null && !cell.IsDestroyed)
                    damagedCells.Add(cell);
            }
        }

        foreach (var cell in damagedCells)
            cell.TakeDamage(damage);
    }
}
