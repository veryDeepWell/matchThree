using UnityEngine;

[CreateAssetMenu(fileName = "SpecialCellData", menuName = "Game/Special Cell Data")]
public class SpecialCellData : ScriptableObject
{
    [Header("Visual")]
    public Sprite icon;
    public Color color = Color.white;
    
    [Header("Behavior")]
    public bool canBeSwappedByPlayer = false;    // Может ли игрок свапнуть эту ячейку
    public bool canFall = false;                 // Падает ли под действием гравитации
    public int maxHealth = 1;                    // Сколько ударов нужно, чтобы сломать
    public bool isDestroyableBySpecial = true;   // Можно ли сломать спец-предметом
    
    [Header("Effects")]
    public GameObject breakEffect;               // Эффект при разрушении
    public AudioClip breakSound;                 // Звук при разрушении
}