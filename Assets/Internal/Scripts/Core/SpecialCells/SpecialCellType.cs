public enum SpecialCellType
{
    None = 0,
    Ice,        // HP > 1, не падает
    Stone,      // HP > 1, падает
    Vine,       // HP = 1, не падает
    Chain,      // HP = 1, падает
}