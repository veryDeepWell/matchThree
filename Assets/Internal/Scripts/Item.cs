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
    [SerializeField] private float _minSwipeDistance = 20f;
    
    [Header("Type")]
    public ItemTypes _itemType;

    private void Start()
    {
        if (board == null)
        {
            board = FindObjectOfType<Board>();
            if (board == null)
            {
                //Debug.LogError("Board not found in scene!");
            }
        }

        transform.position = new Vector2(column, row);
    }

    private void OnMouseDown()
    {
        if (_isMoving) return;
        if (board == null) return;

        _firstTouchPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        //Debug.Log($"Mouse Down at: {_firstTouchPosition} for item ({column},{row})");
    }

    private void OnMouseUp()
    {
        if (_isMoving) return;
        if (board == null) return;

        _finalTouchPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        float swipeDistance = Vector2.Distance(_firstTouchPosition, _finalTouchPosition);
        //Debug.Log($"Swipe distance: {swipeDistance}");

        if (swipeDistance < _minSwipeDistance)
        {
            //Debug.Log("Swipe too short, ignoring");
            return;
        }

        CalculateAngleAndMove();
    }

    private void CalculateAngleAndMove()
    {
        Vector2 swipeDelta = _finalTouchPosition - _firstTouchPosition;
        _swipeAngle = Mathf.Atan2(swipeDelta.y, swipeDelta.x) * Mathf.Rad2Deg;

        //Debug.Log($"Swipe angle: {_swipeAngle} for item ({column},{row})");

        int targetColumn = column;
        int targetRow = row;

        // Определяем направление свайпа
        if (_swipeAngle > -45 && _swipeAngle <= 45)
        {
            // Вправо
            if (column < board.width - 1)
            {
                targetColumn = column + 1;
                //Debug.Log($"Swiping RIGHT to ({targetColumn},{targetRow})");
            }
            else
            {
                //Debug.Log("Can't swipe right - at edge");
                return;
            }
        }
        else if (_swipeAngle > 45 && _swipeAngle <= 135)
        {
            // Вверх
            if (row < board.height - 1)
            {
                targetRow = row + 1;
                //Debug.Log($"Swiping UP to ({targetColumn},{targetRow})");
            }
            else
            {
                //Debug.Log("Can't swipe up - at edge");
                return;
            }
        }
        else if ((_swipeAngle > 135 || _swipeAngle <= -135))
        {
            // Влево
            if (column > 0)
            {
                targetColumn = column - 1;
                //Debug.Log($"Swiping LEFT to ({targetColumn},{targetRow})");
            }
            else
            {
                //Debug.Log("Can't swipe left - at edge");
                return;
            }
        }
        else if (_swipeAngle < -45 && _swipeAngle >= -135)
        {
            // Вниз
            if (row > 0)
            {
                targetRow = row - 1;
                //Debug.Log($"Swiping DOWN to ({targetColumn},{targetRow})");
            }
            else
            {
                //Debug.Log("Can't swipe down - at edge");
                return;
            }
        }
        else
        {
            //Debug.Log($"Invalid swipe angle: {_swipeAngle}");
            return;
        }

        // Проверяем, что целевая позиция отличается от текущей
        if (targetColumn == column && targetRow == row)
        {
            //Debug.Log("Target position is same as current");
            return;
        }

        TrySwap(targetColumn, targetRow);
    }

    private void TrySwap(int targetColumn, int targetRow)
    {
        //Debug.Log($"Trying to swap ({column},{row}) with ({targetColumn},{targetRow})");

        // Проверяем, что целевая ячейка существует
        if (targetColumn < 0 || targetColumn >= board.width ||
            targetRow < 0 || targetRow >= board.height)
        {
            //Debug.LogError($"Target position ({targetColumn},{targetRow}) is out of bounds!");
            return;
        }

        GameObject otherItemObject = board._allItems[targetColumn, targetRow];
        if (otherItemObject == null)
        {
            //Debug.LogError($"No item at position ({targetColumn},{targetRow})!");
            return;
        }

        Item otherItem = otherItemObject.GetComponent<Item>();
        if (otherItem == null)
        {
            //Debug.LogError($"No Item component on object at ({targetColumn},{targetRow})!");
            return;
        }

        // Проверяем, что другой предмет не двигается
        if (otherItem._isMoving)
        {
            //Debug.Log("Other item is moving, can't swap");
            return;
        }

        //Debug.Log($"Before swap: This=({column},{row}), Other=({otherItem.column},{otherItem.row})");

        // Сохраняем старые позиции
        int thisOldColumn = column;
        int thisOldRow = row;
        int otherOldColumn = otherItem.column;
        int otherOldRow = otherItem.row;

        // ВАЖНО: Меняем местами в массиве
        board._allItems[thisOldColumn, thisOldRow] = otherItemObject;
        board._allItems[otherOldColumn, otherOldRow] = this.gameObject;

        // Обновляем координаты у объектов
        column = otherOldColumn;
        row = otherOldRow;
        otherItem.column = thisOldColumn;
        otherItem.row = thisOldRow;

        //Debug.Log($"After swap: This=({column},{row}), Other=({otherItem.column},{otherItem.row})");

        // Запускаем анимацию движения
        StartCoroutine(SwapAnimation(otherItem, otherOldColumn, otherOldRow, thisOldColumn, thisOldRow));
    }

    private IEnumerator SwapAnimation(Item otherItem, int thisTargetCol, int thisTargetRow, int otherTargetCol,
        int otherTargetRow)
    {
        //Debug.Log($"Starting swap animation: This->({thisTargetCol},{thisTargetRow}), Other->({otherTargetCol},{otherTargetRow})");

        _isMoving = true;
        otherItem._isMoving = true;

        // Запускаем обе анимации параллельно
        Coroutine thisMove = StartCoroutine(MoveToPosition(thisTargetCol, thisTargetRow));
        Coroutine otherMove = otherItem.StartCoroutine(otherItem.MoveToPosition(otherTargetCol, otherTargetRow));

        // Ждем завершения обеих анимаций
        yield return thisMove;
        yield return otherMove;

        _isMoving = false;
        otherItem._isMoving = false;

        //Debug.Log($"Swap animation complete for ({column},{row}) and ({otherItem.column},{otherItem.row})");

        if (board != null)
        {
            board.CheckMatches(this);
        }
    }

    public IEnumerator MoveToPosition(int targetColumn, int targetRow)
    {
        Vector2 startPos = transform.position;
        Vector2 targetPos = new Vector2(targetColumn, targetRow);

        //Debug.Log($"Moving from {startPos} to {targetPos}");

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

        // Убеждаемся, что координаты совпадают с позицией
        column = targetColumn;
        row = targetRow;

        //Debug.Log($"Arrived at ({targetColumn},{targetRow})");
    }

    public void SnapToPosition(int targetColumn, int targetRow)
    {
        column = targetColumn;
        row = targetRow;
        transform.position = new Vector2(targetColumn, targetRow);
    }

    public bool IsMoving()
    {
        return _isMoving;
    }

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