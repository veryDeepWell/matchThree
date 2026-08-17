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
    private bool _isQueued;
    private bool _isTriggered;

    public SpecialItemEffect Effect => _effect;
    public int Column => _column;
    public int Row => _row;
    public bool IsTriggered => _isTriggered;

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
        if (item == null)
            return;

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
        if (_board == null)
            _board = FindObjectOfType<Board>();

        if (_board == null || _isQueued || _isTriggered || this == null)
            return;

        _isQueued = true;
        _board.QueueSpecialItem(this);
    }

    public IEnumerator TriggerRoutine()
    {
        if (_isTriggered || this == null)
            yield break;

        _isQueued = false;
        _isTriggered = true;

        if (_effect == null || _board == null)
            yield break;

        var item = GetComponent<Item>();
        if (item != null)
        {
            _column = item.Column;
            _row = item.Row;
        }

        // Null-safe: only plays if assigned on the SpecialItemEffect asset.
        {
            GameObject vfx = _effect.ActivationEffect;
            AudioClip sfx = _effect.ActivationSound;
            var catalog = SoundManager.GetCatalog();
            if (vfx == null && catalog != null) vfx = catalog.specialActivateVfx;
            if (sfx == null && catalog != null) sfx = catalog.specialActivateSfx;
            FxPlayer.Play(vfx, sfx, transform.position);
        }

        var matchesHandler = FindObjectOfType<MatchesHandler>();
        float delay = matchesHandler != null
            ? matchesHandler.GetSpecialItemTriggerDelay()
            : 0.3f;

        yield return new WaitForSeconds(delay);

        if (this == null || _board == null)
            yield break;

        if (item != null)
        {
            _column = item.Column;
            _row = item.Row;
        }

        _effect.Execute(_board, _column, _row);

        if (_board.Data != null && _board.Data.IsValid(_column, _row))
        {
            var cell = _board.GetSpecialCell(_column, _row);
            cell?.ClearOccupant(item);

            _board.SetItemId(_column, _row, "");
            _board.SetSpecialItemId(_column, _row, "");
            _board.Items[_column, _row] = null;
        }

        Destroy(gameObject);
    }

    private void UpdateVisual()
    {
        if (_spriteRenderer == null)
            _spriteRenderer = GetComponent<SpriteRenderer>();

        if (_spriteRenderer == null)
            return;

        Sprite sprite = _definition != null ? _definition.Icon : null;
        Color color = _definition != null ? _definition.Color : Color.white;

        // Definition may be missing after a reimport, or colour may have been
        // saved with alpha 0 (old bomb asset). Keep the special visible.
        if (sprite == null)
        {
            var registry = FindObjectOfType<ItemHandler>()?.GetRegistry();
            registry?.Initialize();
            var bombDef = registry?.Get("bomb");
            sprite = bombDef != null ? bombDef.Icon : null;
        }

        if (color.a < 0.1f)
            color.a = 1f;

        _spriteRenderer.sprite = sprite;
        _spriteRenderer.color = color;
        _spriteRenderer.enabled = sprite != null;
        _spriteRenderer.sortingOrder = 5;
    }
}
