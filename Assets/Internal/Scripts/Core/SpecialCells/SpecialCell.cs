using UnityEngine;

public class SpecialCell : MonoBehaviour
{
    [Header("Runtime Data")]
    [SerializeField] private SpecialCellData _data;
    [SerializeField] private ItemDefinition _definition;
    [SerializeField] private int _currentHealth;
    [SerializeField] private int _column;
    [SerializeField] private int _row;
    [SerializeField] private int _typeIndex;

    private Board _board;
    private Item _occupant;
    private SpriteRenderer _spriteRenderer;

    public SpecialCellData Data => _data;
    public ItemDefinition Definition => _definition;
    public Item Occupant => _occupant;
    public int CurrentHealth => _currentHealth;
    public int MaxHealth => Mathf.Max(1, _definition != null ? _definition.SpecialCellStateCount : 1);
    public int Column => _column;
    public int Row => _row;
    public int TypeIndex => _typeIndex;
    public bool IsDestroyed => _currentHealth <= 0;

    public bool BlocksFalling() => !IsDestroyed && !CanFall();
    public bool CanBeSwappedByPlayer() => !IsDestroyed && (_data?.canBeSwappedByPlayer ?? false);
    public bool CanFall() => !IsDestroyed && (_data?.canFall ?? false);
    public bool CanItemStand() => !IsDestroyed;
    public bool IsDestroyableBySpecial() => !IsDestroyed && (_data?.isDestroyableBySpecial ?? true);

    public void Initialize(
        SpecialCellData data,
        ItemDefinition definition,
        int typeIndex,
        Board board,
        int column,
        int row)
    {
        _data = data;
        _definition = definition;
        _typeIndex = typeIndex;
        _board = board;
        _column = column;
        _row = row;
        _currentHealth = MaxHealth;

        _spriteRenderer = GetComponent<SpriteRenderer>();
        ConfigureVisual();
        SetGridPosition(column, row, false);
    }

    public void AttachItem(Item item)
    {
        _occupant = item;

        if (item == null)
            return;

        _board = item.Board != null ? item.Board : _board;
        SetGridPosition(item.Column, item.Row, false);
        SyncSortingLayer();
    }

    public void ClearOccupant(Item item = null)
    {
        if (item == null || _occupant == item)
            _occupant = null;
    }

    public void FollowItemTransform(Item item)
    {
        if (item != null)
            transform.position = item.transform.position;
    }

    public void SetGridPosition(int column, int row)
    {
        SetGridPosition(column, row, true);
    }

    public void TakeDamage(int damage = 1)
    {
        if (_data == null || IsDestroyed || damage <= 0)
            return;

        _currentHealth -= damage;

        if (_currentHealth <= 0)
            DestroyCell();
        else
            ConfigureVisual();
    }

    private void DestroyCell()
    {
        // Null-safe: only plays if the fields are assigned on SpecialCellData.
        if (_data != null)
            FxPlayer.Play(_data.breakEffect, _data.breakSound, transform.position);

        if (_board != null)
        {
            if (_board.GetSpecialCell(_column, _row) == this)
                _board.SetSpecialCell(_column, _row, null);

            _board.Data?.SetSpecialCell(_column, _row, 0);
        }

        // The occupant belongs to the board, not to the overlay.
        // Destroying the overlay must never destroy the item underneath it.
        _occupant = null;
        Destroy(gameObject);
    }

    private void ConfigureVisual()
    {
        if (_spriteRenderer == null)
            _spriteRenderer = GetComponent<SpriteRenderer>();

        if (_spriteRenderer == null)
            return;

        int stateIndex = Mathf.Clamp(MaxHealth - _currentHealth, 0, MaxHealth - 1);
        _spriteRenderer.sprite = _definition != null
            ? _definition.GetSpecialCellStateSprite(stateIndex)
            : null;
        _spriteRenderer.color = _definition != null
            ? _definition.SpecialCellOverlayColor
            : Color.white;

        // The item prefab uses its own sorting layer. SortingOrder alone cannot
        // bring the overlay above an item on another sorting layer.
        _spriteRenderer.sortingOrder = 10;
        _spriteRenderer.enabled = _spriteRenderer.sprite != null;

        var collider = GetComponent<Collider2D>();
        if (collider != null)
            collider.enabled = false;

        SyncSortingLayer();
    }

    private void SyncSortingLayer()
    {
        if (_spriteRenderer == null)
            return;

        var itemRenderer = _occupant != null
            ? _occupant.GetComponent<SpriteRenderer>()
            : null;

        if (itemRenderer != null)
            _spriteRenderer.sortingLayerID = itemRenderer.sortingLayerID;
    }

    private void SetGridPosition(int column, int row, bool updateBoard)
    {
        if (_board != null && updateBoard && (_column != column || _row != row))
        {
            if (_board.GetSpecialCell(_column, _row) == this)
                _board.SetSpecialCell(_column, _row, null);

            if (_board.Data != null &&
                _board.Data.IsValid(_column, _row) &&
                _board.Data.GetSpecialCell(_column, _row) == _typeIndex)
            {
                _board.Data.SetSpecialCell(_column, _row, 0);
            }
        }

        _column = column;
        _row = row;

        if (_board != null && updateBoard)
        {
            _board.SetSpecialCell(column, row, this);
            _board.Data?.SetSpecialCell(column, row, _typeIndex);
        }

        if (_board != null)
        {
            var targetTile = _board.transform.Find($"Tile({column},{row})");
            if (targetTile != null)
                transform.SetParent(targetTile, true);

            transform.position = _board.GetWorldPosition(column, row);
        }

        SyncSortingLayer();
    }
}
