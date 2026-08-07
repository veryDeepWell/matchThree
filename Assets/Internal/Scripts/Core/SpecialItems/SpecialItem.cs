using System.Collections;
using UnityEngine;

public class SpecialItem : MonoBehaviour, ISpecialItem
{
    [Header("Runtime Data")]
    [SerializeField] private SpecialItemEffect _effect;
    [SerializeField] private ItemDefinition _definition;
    [SerializeField] private int _column = -1;
    [SerializeField] private int _row = -1;

    private Board _board;
    private SpriteRenderer _spriteRenderer;

    public SpecialItemEffect Effect => _effect;
    public int Column => _column;
    public int Row => _row;

    private void Start()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        UpdateVisual();
    }

    public void Initialize(SpecialItemEffect effect, ItemDefinition definition, int column, int row)
    {
        _effect = effect;
        _definition = definition;
        _column = column;
        _row = row;
        _board = FindObjectOfType<Board>();
        UpdateVisual();
    }

    public void SetBoard(Board board)
    {
        _board = board;
        var item = GetComponent<Item>();
        if (item != null)
            item.Board = board;
    }

    public void SetGridPosition(int column, int row)
    {
        _column = column;
        _row = row;

        var item = GetComponent<Item>();
        if (item == null) return;

        item.Column = column;
        item.Row = row;
        if (_board != null)
            item.transform.position = _board.GetWorldPosition(column, row);
    }

    public void CreateSpecialItem(int column, int row)
    {
        SetGridPosition(column, row);
    }

    public void TriggerSpecialItem()
    {
        if (_effect == null || _board == null)
        {
            Debug.LogWarning("[SpecialItem] Effect or Board is null.");
            return;
        }

        var item = GetComponent<Item>();
        if (item == null)
        {
            Debug.LogWarning("[SpecialItem] Item component is missing.");
            return;
        }

        _column = item.Column;
        _row = item.Row;

        if (_effect.ActivationEffect != null)
            Instantiate(_effect.ActivationEffect, transform.position, Quaternion.identity);

        if (_effect.ActivationSound != null)
            AudioSource.PlayClipAtPoint(_effect.ActivationSound, transform.position);

        _board.StartCoroutine(ExecuteWithDelay(_column, _row));
    }

    private IEnumerator ExecuteWithDelay(int column, int row)
    {
        var matchHandler = FindObjectOfType<MatchesHandler>();
        float delay = matchHandler != null ? matchHandler.GetBombExplosionDelay() : 0.3f;
        yield return new WaitForSeconds(delay);

        var item = GetComponent<Item>();
        if (item != null)
        {
            column = item.Column;
            row = item.Row;
        }

        _effect.Execute(_board, column, row);

        if (_board != null && _board.Data != null && _board.Data.IsValid(column, row))
        {
            _board.SetItemId(column, row, "");
            _board.SetSpecialItemId(column, row, "");
            _board.Items[column, row] = null;
        }

        Destroy(gameObject);

        if (matchHandler != null)
        {
            matchHandler.DropItems(_board);
            matchHandler.ProcessMatches(_board);
        }
    }

    private void UpdateVisual()
    {
        if (_spriteRenderer == null)
            _spriteRenderer = GetComponent<SpriteRenderer>();

        if (_spriteRenderer == null || _definition == null)
            return;

        _spriteRenderer.sprite = _definition.Icon;
        _spriteRenderer.color = _definition.Color;
        _spriteRenderer.enabled = _spriteRenderer.sprite != null;
        _spriteRenderer.sortingOrder = 2;
    }
}
