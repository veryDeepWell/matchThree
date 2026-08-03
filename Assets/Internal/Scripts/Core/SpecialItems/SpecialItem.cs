using System.Collections;
using UnityEngine;

public class SpecialItem : MonoBehaviour, ISpecialItem
{
    [Header("Effect")] [SerializeField] private SpecialItemEffect _effect;
    private Board _board;
    private SpriteRenderer _spriteRenderer;
    private int _column = -1;
    private int _row = -1;

    public SpecialItemEffect Effect => _effect;
    public int Column => _column;
    public int Row => _row;

    // Start теперь НЕ трогает позицию
    private void Start()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        UpdateVisual();
    }

    public void Initialize(SpecialItemEffect effect, int column, int row)
    {
        _effect = effect;
        _column = column;
        _row = row;
        _board = FindObjectOfType<Board>();
        UpdateVisual();
        
        // Если есть валидные координаты — сразу регистрируем
        if (_board != null && column >= 0 && row >= 0)
        {
            RegisterInBoard();
        }
    }

    public void RegisterInBoard()
    {
        if (_board == null || _column < 0 || _row < 0) return;
        if (_column >= _board.Width || _row >= _board.Height) return;

        var item = GetComponent<Item>();
        if (item != null)
        {
            item.Column = _column;
            item.Row = _row;
            item.Board = _board;
            _board.Items[_column, _row] = item;
            item.transform.position = _board.GetWorldPosition(_column, _row);
            Debug.Log($"[SpecialItem] Registered at ({_column},{_row})");
        }
    }

    public void SetBoard(Board board)
    {
        _board = board;
        var item = GetComponent<Item>();
        if (item != null) item.Board = board;
    }

    public void SetGridPosition(int column, int row)
    {
        _column = column;
        _row = row;
        var item = GetComponent<Item>();
        if (item != null)
        {
            item.Column = column;
            item.Row = row;
            if (_board != null)
            {
                item.transform.position = _board.GetWorldPosition(column, row);
            }
        }
    }

    public void CreateSpecialItem(int column, int row)
    {
        _column = column;
        _row = row;
    }

    public void TriggerSpecialItem()
    {
        if (_effect == null || _board == null)
        {
            Debug.LogWarning("SpecialItem: Effect or Board is null!");
            return;
        }

        // Эффект активации
        if (_effect.ActivationEffect != null)
            Instantiate(_effect.ActivationEffect, transform.position, Quaternion.identity);

        // Задержка перед взрывом (чтобы игрок увидел)
        _board.StartCoroutine(ExecuteWithDelay());
    }

    private IEnumerator ExecuteWithDelay()
    {
        // Получаем задержку из MatchesHandler
        var handler = FindObjectOfType<MatchesHandler>();
        float delay = handler != null ? handler.GetBombExplosionDelay() : 0.3f;
    
        yield return new WaitForSeconds(delay);

        // Выполняем эффект
        _effect.Execute(_board, _column, _row);

        // Удаляем сам предмет
        if (_board != null && _board.Data != null)
        {
            _board.SetItemId(_column, _row, "");
            _board.Items[_column, _row] = null;
        }
        Destroy(gameObject);

        // Падение и проверка
        if (handler != null)
        {
            handler.DropItems(_board);
            handler.ProcessMatches(_board);
        }
    }

    private void UpdateVisual()
    {
        if (_effect == null) return;
        if (_spriteRenderer == null) _spriteRenderer = GetComponent<SpriteRenderer>();
        if (_spriteRenderer == null) return;

        if (_effect.Icon != null)
        {
            _spriteRenderer.sprite = _effect.Icon;
            _spriteRenderer.color = _effect.Color;
            _spriteRenderer.enabled = true;
            _spriteRenderer.sortingOrder = 2;
        }
    }
}