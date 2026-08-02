using UnityEngine;

public class SpecialItem : MonoBehaviour, ISpecialItem
{
    [Header("Effect")]
    [SerializeField] private SpecialItemEffect _effect;
    
    [Header("Runtime")]
    [SerializeField] private int _column;
    [SerializeField] private int _row;
    
    private Board _board;
    
    public SpecialItemEffect Effect => _effect;
    public int Column => _column;
    public int Row => _row;
    
    private void Start()
    {
        _board = FindObjectOfType<Board>();
        UpdateVisual();
    }
    
    public void Initialize(SpecialItemEffect effect, int column, int row)
    {
        _effect = effect;
        _column = column;
        _row = row;
        UpdateVisual();
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
        
        if (_effect.ActivationEffect != null)
            Instantiate(_effect.ActivationEffect, transform.position, Quaternion.identity);
        
        _effect.Execute(_board, _column, _row);
        
        // Проверяем что Board и Data существуют
        if (_board != null && _board.Data != null)
        {
            _board.SetItemId(_column, _row, "");
            _board.Items[_column, _row] = null;
        }
        
        Destroy(gameObject);
        
        var handler = FindObjectOfType<MatchesHandler>();
        if (handler != null)
        {
            handler.DropItems(_board);
            handler.ProcessMatches(_board);
        }
    }
    
    private void UpdateVisual()
    {
        if (_effect == null) return;
        
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            if (_effect.Icon != null)
                sr.sprite = _effect.Icon;
            sr.color = _effect.Color;
        }
    }
}