using UnityEngine;

public class SpecialCell : MonoBehaviour
{
    [Header("Runtime Data")]
    [SerializeField] private SpecialCellData _data;
    [SerializeField] private int _currentHealth;
    [SerializeField] private int _column;
    [SerializeField] private int _row;
    
    public SpecialCellData Data => _data;
    public int CurrentHealth => _currentHealth;
    public int Column => _column;
    public int Row => _row;
    public bool IsDestroyed => _currentHealth <= 0;
    
    public void Initialize(SpecialCellData data, int column, int row)
    {
        _data = data;
        _column = column;
        _row = row;
        _currentHealth = data.maxHealth;
        
        // Обновляем визуал
        UpdateVisual();
    }
    
    public bool CanBeSwappedByPlayer() => _data?.canBeSwappedByPlayer ?? false;
    public bool CanFall() => _data?.canFall ?? false;
    public bool IsDestroyableBySpecial() => _data?.isDestroyableBySpecial ?? true;
    
    public void TakeDamage(int damage = 1)
    {
        if (_data == null || IsDestroyed) return;
        
        _currentHealth -= damage;
        
        if (_currentHealth <= 0)
        {
            DestroyCell();
        }
        else
        {
            UpdateVisual();
        }
    }
    
    private void DestroyCell()
    {
        // Эффекты разрушения
        if (_data.breakEffect != null)
            Instantiate(_data.breakEffect, transform.position, Quaternion.identity);
        
        // TODO: Sound
        // AudioSource.PlayClipAtPoint(_data.BreakSound, transform.position);
        
        // Оповещаем доску
        var board = FindObjectOfType<Board>();
        if (board != null)
        {
            // Освобождаем ячейку
            board.SetSpecialCell(_column, _row, null);
        }
        
        Destroy(gameObject);
    }
    
    private void UpdateVisual()
    {
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null && _data != null)
        {
            sr.sprite = _data.icon;
            sr.color = _data.color;
            
            if (_data.maxHealth > 1)
            {
                // TODO: Отобразить количество HP
            }
        }
    }
}