using System.Collections;
using UnityEngine;

public class Item : MonoBehaviour
{
    [Header("Grid Position")]
    public int Row;
    public int Column;

    [Header("References")]
    public Board Board;

    [Header("Movement Settings")]
    [SerializeField] private float _moveDuration = 0.15f;
    [SerializeField] private float _minSwipeDistance = 0.2f;

    [Header("Type")]
    public string ItemId;
    public string SpecialItemId;
    
    private Camera _camera;
    private Vector2 _firstTouch;
    private Vector2 _finalTouch;
    private bool _isMoving;
    private Transform _cachedTransform;
    
    private ItemRegistry _registry;

    private void Start()
    {
        _camera = Camera.main;
        _cachedTransform = transform;
        
        var handler = FindObjectOfType<ItemHandler>();
        if (handler != null)
        {
            _registry = handler.GetRegistry();
        }
        
        if (_registry == null)
        {
            _registry = Resources.Load<ItemRegistry>("ItemRegistry");
        }
        
        if (Board != null)
        {
            _cachedTransform.position = Board.GetWorldPosition(Column, Row);
        }
        else
        {
            _cachedTransform.position = new Vector3(Column, Row, 0);
        }
    }

    private void OnMouseDown()
    {
        if (_isMoving || Board == null || _cachedTransform == null) return;
        _firstTouch = _camera.ScreenToWorldPoint(Input.mousePosition);
    }

    private void OnMouseUp()
    {
        if (_isMoving || Board == null || _cachedTransform == null) return;

        _finalTouch = _camera.ScreenToWorldPoint(Input.mousePosition);
        if (Vector2.Distance(_firstTouch, _finalTouch) < _minSwipeDistance) return;

        TrySwipe();
    }

    private void TrySwipe()
    {
        if (Board == null || Board.Data == null) return;

        Vector2 delta = _finalTouch - _firstTouch;
        float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;

        int targetX = Column;
        int targetY = Row;

        if (angle > -45 && angle <= 45) targetX = Column + 1;
        else if (angle > 45 && angle <= 135) targetY = Row + 1;
        else if (angle > 135 || angle <= -135) targetX = Column - 1;
        else if (angle < -45 && angle >= -135) targetY = Row - 1;
        else return;

        if (targetX == Column && targetY == Row) return;
        if (!Board.IsCellActive(targetX, targetY)) return;

        var other = Board.Items[targetX, targetY];
        if (other == null || other._isMoving) return;

        // Бомбы можно свапать без комбинации
        bool isBomb = !string.IsNullOrEmpty(SpecialItemId) && SpecialItemId == "bomb";
        bool otherIsBomb = other != null && !string.IsNullOrEmpty(other.SpecialItemId) && other.SpecialItemId == "bomb";
    
        if (!isBomb && !otherIsBomb)
        {
            if (!WouldCreateMatch(other, targetX, targetY)) return;
        }

        StartCoroutine(Swap(other, targetX, targetY));
    }

    private bool WouldCreateMatch(Item other, int targetX, int targetY)
    {
        if (Board?.Data == null) return false;

        var data = Board.Data;
        int myIdx = data.GetIndex(Column, Row);
        int otherIdx = data.GetIndex(targetX, targetY);

        string myType = data.Items[myIdx];
        string otherType = data.Items[otherIdx];

        data.Items[myIdx] = otherType;
        data.Items[otherIdx] = myType;

        var matches = MatchFinder.FindMatches(data);

        data.Items[myIdx] = myType;
        data.Items[otherIdx] = otherType;

        return matches.Count > 0;
    }

    private IEnumerator Swap(Item other, int targetX, int targetY)
    {
        if (Board == null || other == null) yield break;

        _isMoving = true;
        other._isMoving = true;

        int myX = Column;
        int myY = Row;
        int otherX = other.Column;
        int otherY = other.Row;

        // Сохраняем позицию свапа (этот предмет был свапнут)
        int swapX = Column;
        int swapY = Row;

        Board.Items[myX, myY] = other;
        Board.Items[otherX, otherY] = this;

        Column = otherX;
        Row = otherY;
        other.Column = myX;
        other.Row = myY;

        Coroutine move1 = StartCoroutine(MoveToPosition(otherX, otherY));
        Coroutine move2 = other.StartCoroutine(other.MoveToPosition(myX, myY));

        yield return move1;
        yield return move2;

        _isMoving = false;
        other._isMoving = false;

        // Проверяем бомбы на обеих позициях
        CheckAndTriggerBomb(Board, swapX, swapY);
        CheckAndTriggerBomb(Board, otherX, otherY);

        // Запускаем проверку матчей с позицией свапа
        if (Board != null)
            Board.CheckMatches(swapX, swapY);
    }

    private void CheckAndTriggerBomb(Board board, int x, int y)
    {
        if (board == null) return;
        if (x < 0 || x >= board.Width || y < 0 || y >= board.Height) return;
    
        var item = board.Items[x, y];
        if (item != null && !string.IsNullOrEmpty(item.SpecialItemId) && item.SpecialItemId == "bomb")
        {
            Debug.Log($"[CheckAndTriggerBomb] Bomb triggered at ({x},{y})");
            item.GetComponent<ISpecialItem>()?.TriggerSpecialItem();
        }
    }

    public IEnumerator MoveToPosition(int targetX, int targetY)
    {
        if (_cachedTransform == null)
        {
            _cachedTransform = transform;
            if (_cachedTransform == null) yield break;
        }

        if (Board == null) yield break;

        Vector2 start = _cachedTransform.position;
        Vector2 target = Board.GetWorldPosition(targetX, targetY);

        float elapsed = 0f;

        while (elapsed < _moveDuration)
        {
            if (_cachedTransform == null || Board == null) yield break;

            elapsed += Time.deltaTime;
            float t = elapsed / _moveDuration;
            float smooth = t * t * (3f - 2f * t);
            _cachedTransform.position = Vector2.Lerp(start, target, smooth);
            yield return null;
        }

        if (_cachedTransform != null)
        {
            _cachedTransform.position = target;
            Column = targetX;
            Row = targetY;
        }
    }

    public void SnapToPosition(int targetX, int targetY)
    {
        if (_cachedTransform == null)
        {
            _cachedTransform = transform;
            if (_cachedTransform == null) return;
        }

        Column = targetX;
        Row = targetY;

        if (Board != null)
            _cachedTransform.position = Board.GetWorldPosition(targetX, targetY);
        else
            _cachedTransform.position = new Vector2(targetX, targetY);
    }
}