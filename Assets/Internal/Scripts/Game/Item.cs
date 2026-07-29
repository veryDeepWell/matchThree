using System.Collections;
using UnityEngine;

[DefaultExecutionOrder(10)]
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
    public ItemTypes ItemType;
    public SpecialItemTypes SpecialType = SpecialItemTypes.None;

    private Camera _camera;
    private Vector2 _firstTouch;
    private Vector2 _finalTouch;
    private bool _isMoving;
    private Transform _cachedTransform;

    private void Start()
    {
        _camera = Camera.main;
        _cachedTransform = transform;
        _cachedTransform.position = new Vector3(Column, Row, 0);
    }

    private void OnMouseDown()
    {
        if (_isMoving || Board == null) return;
        _firstTouch = _camera.ScreenToWorldPoint(Input.mousePosition);
    }

    private void OnMouseUp()
    {
        if (_isMoving || Board == null) return;

        _finalTouch = _camera.ScreenToWorldPoint(Input.mousePosition);
        if (Vector2.Distance(_firstTouch, _finalTouch) < _minSwipeDistance) return;

        TrySwipe();
    }

    private void TrySwipe()
    {
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

        StartCoroutine(Swap(other, targetX, targetY));
    }

    private IEnumerator Swap(Item other, int targetX, int targetY)
    {
        _isMoving = true;
        other._isMoving = true;

        int myX = Column;
        int myY = Row;
        int otherX = other.Column;
        int otherY = other.Row;

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

        if (SpecialType != SpecialItemTypes.None)
            GetComponent<ISpecialItem>()?.TriggerSpecialItem();

        Board.CheckMatches();
    }

    public IEnumerator MoveToPosition(int targetX, int targetY)
    {
        Vector2 start = _cachedTransform.position;
        Vector2 target = new Vector2(targetX, targetY);
        float elapsed = 0f;

        while (elapsed < _moveDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / _moveDuration;
            float smooth = t * t * (3f - 2f * t);
            _cachedTransform.position = Vector2.Lerp(start, target, smooth);
            yield return null;
        }

        _cachedTransform.position = target;
        Column = targetX;
        Row = targetY;
    }
}