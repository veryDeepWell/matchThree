using System;
using System.Collections;
using UnityEngine;

public class Item : MonoBehaviour
{
    [Header("Grid Position")] 
    public int column;
    public int row;

    [Header("References")] 
    public Board board;

    private Vector2 _firstTouchPosition;
    private Vector2 _finalTouchPosition;
    private float _swipeAngle;
    private bool _isMoving;

    [Header("Movement Settings")] 
    [SerializeField] private float _moveDuration = 0.15f;
    [SerializeField] private float _minSwipeDistance = 0.2f;
    
    [Header("Type")]
    public ItemTypes _itemType;
    public SpecialItemTypes _specialType = SpecialItemTypes.None;

    private void Start()
    {
        if (board == null)
        {
            board = FindObjectOfType<Board>();
        }

        transform.position = new Vector2(column, row);
    }

    private void OnMouseDown()
    {
        if (_isMoving) return;
        if (board == null) return;

        _firstTouchPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
    }

    private void OnMouseUp()
    {
        if (_isMoving) return;
        if (board == null) return;

        _finalTouchPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        float swipeDistance = Vector2.Distance(_firstTouchPosition, _finalTouchPosition);

        if (swipeDistance < _minSwipeDistance)
        {
            return;
        }

        CalculateAngleAndMove();
    }

    private void CalculateAngleAndMove()
    {
        Vector2 swipeDelta = _finalTouchPosition - _firstTouchPosition;
        _swipeAngle = Mathf.Atan2(swipeDelta.y, swipeDelta.x) * Mathf.Rad2Deg;
        
        int targetColumn = column;
        int targetRow = row;

        if (_swipeAngle > -45 && _swipeAngle <= 45)
        {
            if (column < board.width - 1)
                targetColumn = column + 1;
            else return;
        }
        else if (_swipeAngle > 45 && _swipeAngle <= 135)
        {
            if (row < board.height - 1)
                targetRow = row + 1;
            else return;
        }
        else if ((_swipeAngle > 135 || _swipeAngle <= -135))
        {
            if (column > 0)
                targetColumn = column - 1;
            else return;
        }
        else if (_swipeAngle < -45 && _swipeAngle >= -135)
        {
            if (row > 0)
                targetRow = row - 1;
            else return;
        }
        else return;
        
        if (targetColumn == column && targetRow == row) return;

        TrySwap(targetColumn, targetRow);
    }

    private void TrySwap(int targetColumn, int targetRow)
    {
        if (targetColumn < 0 || targetColumn >= board.width || targetRow < 0 || targetRow >= board.height) return;
        
        Item otherItem = board._allItems[targetColumn, targetRow];
        if (otherItem == null) return;
        
        if (otherItem._isMoving) return;

        int thisOldColumn = column;
        int thisOldRow = row;
        int otherOldColumn = otherItem.column;
        int otherOldRow = otherItem.row;
        
        board._allItems[thisOldColumn, thisOldRow] = otherItem;
        board._allItems[otherOldColumn, otherOldRow] = this;
        
        column = otherOldColumn;
        row = otherOldRow;
        otherItem.column = thisOldColumn;
        otherItem.row = thisOldRow;
        
        StartCoroutine(SwapAnimation(otherItem, otherOldColumn, otherOldRow, thisOldColumn, thisOldRow));
    }

    private IEnumerator SwapAnimation(Item otherItem, int thisTargetCol, int thisTargetRow, int otherTargetCol, int otherTargetRow)
    {
        _isMoving = true;
        otherItem._isMoving = true;
        
        Coroutine thisMove = StartCoroutine(MoveToPosition(thisTargetCol, thisTargetRow));
        Coroutine otherMove = otherItem.StartCoroutine(otherItem.MoveToPosition(otherTargetCol, otherTargetRow));
        
        yield return thisMove;
        yield return otherMove;

        _isMoving = false;
        otherItem._isMoving = false;
        
        if (board != null)
        {
            board.CheckMatches();
        }
    }

    public IEnumerator MoveToPosition(int targetColumn, int targetRow)
    {
        Vector2 startPos = transform.position;
        Vector2 targetPos = new Vector2(targetColumn, targetRow);
        
        float elapsedTime = 0f;

        while (elapsedTime < _moveDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / _moveDuration;
            float smoothT = t * t * (3f - 2f * t);

            transform.position = Vector2.Lerp(startPos, targetPos, smoothT);
            yield return null;
        }

        transform.position = targetPos;
        column = targetColumn;
        row = targetRow;
    }

    public void SnapToPosition(int targetColumn, int targetRow)
    {
        column = targetColumn;
        row = targetRow;
        transform.position = new Vector2(targetColumn, targetRow);
    }

    public bool IsMoving() => _isMoving;

    public void CancelMovement()
    {
        StopAllCoroutines();
        _isMoving = false;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        gameObject.name = $"Item({column},{row})";
    }
#endif
}