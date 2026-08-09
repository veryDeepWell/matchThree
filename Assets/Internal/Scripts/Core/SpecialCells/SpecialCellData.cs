using UnityEngine;

[CreateAssetMenu(fileName = "SpecialCellData", menuName = "Game/Special Cell Data")]
public class SpecialCellData : ScriptableObject
{
    [Header("Behavior")]
    public bool canBeSwappedByPlayer = false;
    public bool canFall = false;
    public bool isDestroyableBySpecial = true;

    [Header("Effects")]
    public GameObject breakEffect;
    public AudioClip breakSound;
}
