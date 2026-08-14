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
    private bool _positionSetExplicitly;
    private bool _usingTouchInput;
    private Transform _cachedTransform;
    private bool _bonusPlacementHandled;

    public bool IsMoving => _isMoving;
    public float MoveDuration => _moveDuration;

    private void Start()
    {
        _camera = Camera.main;
        _cachedTransform = transform;

        if (_positionSetExplicitly)
            return;

        if (Board != null)
            _cachedTransform.position = Board.GetWorldPosition(Column, Row);
        else
            _cachedTransform.position = new Vector3(Column, Row, 0f);
    }

    private void Update()
    {
        if (!enabled || _isMoving || Board == null || Board.IsProcessing)
            return;

        HandleTouchInput();
    }

    private void OnMouseDown()
    {
        if (Input.touchCount > 0)
            return;

        if (_isMoving || Board == null || Board.IsProcessing || _cachedTransform == null)
            return;

        GameplayFlowController gameplayFlow = FindFirstObjectByType<GameplayFlowController>();
        if (gameplayFlow != null && gameplayFlow.HandleBonusCellClick(this))
        {
            _bonusPlacementHandled = true;
            return;
        }

        _camera ??= Camera.main;
        if (_camera == null)
            return;

        _firstTouch = _camera.ScreenToWorldPoint(Input.mousePosition);
    }

    private void OnMouseUp()
    {
        if (_bonusPlacementHandled)
        {
            _bonusPlacementHandled = false;
            return;
        }
        if (_usingTouchInput || Input.touchCount > 0)
            return;

        if (_isMoving || Board == null || Board.IsProcessing || _cachedTransform == null)
            return;

        _camera ??= Camera.main;
        if (_camera == null)
            return;

        _finalTouch = _camera.ScreenToWorldPoint(Input.mousePosition);
        TrySwipeFromPoints(_firstTouch, _finalTouch);
    }

    public void SetVisualPosition(Vector2 worldPosition)
    {
        _cachedTransform ??= transform;
        _cachedTransform.position = worldPosition;
        _positionSetExplicitly = true;
    }

    public void SnapToPosition(int targetX, int targetY)
    {
        _cachedTransform ??= transform;
        if (_cachedTransform == null)
            return;

        Column = targetX;
        Row = targetY;

        if (Board != null)
            _cachedTransform.position = Board.GetWorldPosition(targetX, targetY);
        else
            _cachedTransform.position = new Vector2(targetX, targetY);

        _positionSetExplicitly = true;
    }

    public IEnumerator MoveToPosition(int targetX, int targetY)
    {
        _cachedTransform ??= transform;

        if (_cachedTransform == null || Board == null)
            yield break;

        Vector2 startPosition = _cachedTransform.position;
        Vector2 targetPosition = Board.GetWorldPosition(targetX, targetY);
        float elapsed = 0f;

        while (elapsed < _moveDuration)
        {
            if (_cachedTransform == null || Board == null)
                yield break;

            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / _moveDuration);
            float smoothProgress = progress * progress * (3f - 2f * progress);

            _cachedTransform.position = Vector2.Lerp(startPosition, targetPosition, smoothProgress);

            var movingCell = Board.GetSpecialCell(Column, Row);
            if (movingCell != null && movingCell.Occupant == this)
                movingCell.FollowItemTransform(this);

            yield return null;
        }

        _cachedTransform.position = targetPosition;
        Column = targetX;
        Row = targetY;

        var finalCell = Board.GetSpecialCell(targetX, targetY);
        if (finalCell != null && finalCell.Occupant == this)
            finalCell.SetGridPosition(targetX, targetY);

        GetComponent<SpecialItem>()?.SetGridPosition(targetX, targetY);
    }

    public IEnumerator PlayHint(Vector2 direction, float distance, float duration)
    {
        if (_cachedTransform == null || _isMoving)
            yield break;

        Vector3 originalPosition = _cachedTransform.position;
        Vector3 offset = (Vector3)direction.normalized * distance;

        yield return MoveTransform(originalPosition, originalPosition + offset, duration);
        yield return MoveTransform(originalPosition + offset, originalPosition, duration);
    }

    private IEnumerator MoveTransform(Vector3 startPosition, Vector3 targetPosition, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float smoothProgress = progress * progress * (3f - 2f * progress);
            _cachedTransform.position = Vector3.Lerp(startPosition, targetPosition, smoothProgress);
            yield return null;
        }

        _cachedTransform.position = targetPosition;
    }

    private void HandleTouchInput()
    {
        if (Input.touchCount != 1)
            return;

        Touch touch = Input.GetTouch(0);
        _camera ??= Camera.main;
        if (_camera == null)
            return;

        if (touch.phase == TouchPhase.Began)
        {
            _usingTouchInput = true;
            _firstTouch = _camera.ScreenToWorldPoint(touch.position);
        }
        else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
        {
            _finalTouch = _camera.ScreenToWorldPoint(touch.position);

            if (touch.phase == TouchPhase.Ended)
                TrySwipeFromPoints(_firstTouch, _finalTouch);

            _usingTouchInput = false;
        }
    }

    private void TrySwipeFromPoints(Vector2 startPoint, Vector2 endPoint)
    {
        if (Board == null || Board.Data == null || Board.IsProcessing)
            return;

        if (Vector2.Distance(startPoint, endPoint) < _minSwipeDistance)
            return;

        Vector2 delta = endPoint - startPoint;
        int targetColumn = Column;
        int targetRow = Row;

        if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
            targetColumn += delta.x > 0f ? 1 : -1;
        else
            targetRow += delta.y > 0f ? 1 : -1;

        if (!Board.IsCellActive(targetColumn, targetRow))
        {
            StartCoroutine(PlayRejectedMove(Vector2.zero));
            return;
        }

        var sourceCell = Board.GetSpecialCell(Column, Row);
        var targetCell = Board.GetSpecialCell(targetColumn, targetRow);

        if ((sourceCell != null && !sourceCell.CanBeSwappedByPlayer()) ||
            (targetCell != null && !targetCell.CanBeSwappedByPlayer()))
        {
            StartCoroutine(PlayRejectedMove(Vector2.zero));
            return;
        }

        var otherItem = Board.Items[targetColumn, targetRow];
        if (otherItem == null || otherItem._isMoving)
        {
            StartCoroutine(PlayRejectedMove(Vector2.zero));
            return;
        }

        bool thisIsSpecial = !string.IsNullOrEmpty(SpecialItemId);
        bool otherIsSpecial = !string.IsNullOrEmpty(otherItem.SpecialItemId);

        // Specials can always be swapped (with each other or with normals).
        // Two normals still require a resulting match.
        if (!thisIsSpecial && !otherIsSpecial &&
            !WouldCreateMatch(otherItem, targetColumn, targetRow))
        {
            PlayCatalogSfx(invalid: true);
            StartCoroutine(PlayRejectedSwap(otherItem));
            return;
        }

        PlayCatalogSfx(invalid: false);
        StartCoroutine(Swap(otherItem, targetColumn, targetRow));
    }

    private void PlayCatalogSfx(bool invalid)
    {
        // Read serialized catalog from MatchesHandler only (plain field, no SoundManager).
        var catalog = FindObjectOfType<MatchesHandler>()?.FxCatalog;
        if (catalog == null)
            catalog = SoundManager.GetCatalog();
        if (catalog == null)
            return;

        var clip = invalid ? catalog.invalidSwapSfx : catalog.swapSfx;
        FxPlayer.PlaySfx(clip, transform.position);
    }

    private bool WouldCreateMatch(Item otherItem, int targetColumn, int targetRow)
    {
        if (Board?.Data == null || otherItem == null)
            return false;

        int firstIndex = Board.Data.GetIndex(Column, Row);
        int secondIndex = Board.Data.GetIndex(targetColumn, targetRow);

        string firstItemId = Board.Data.Items[firstIndex];
        string secondItemId = Board.Data.Items[secondIndex];

        Board.Data.Items[firstIndex] = secondItemId;
        Board.Data.Items[secondIndex] = firstItemId;

        bool createsMatch = MatchFinder.FindMatches(Board.Data).Count > 0;

        Board.Data.Items[firstIndex] = firstItemId;
        Board.Data.Items[secondIndex] = secondItemId;

        return createsMatch;
    }

    private IEnumerator Swap(Item otherItem, int targetColumn, int targetRow)
    {
        _isMoving = true;
        otherItem._isMoving = true;

        int sourceColumn = Column;
        int sourceRow = Row;

        var sourceCell = Board.GetSpecialCell(sourceColumn, sourceRow);
        var targetCell = Board.GetSpecialCell(targetColumn, targetRow);

        string thisSpecialId = SpecialItemId;
        string otherSpecialId = otherItem.SpecialItemId;
        string thisItemId = ItemId;
        string otherItemId = otherItem.ItemId;

        Board.Items[sourceColumn, sourceRow] = otherItem;
        Board.Items[targetColumn, targetRow] = this;

        Board.SetItemId(sourceColumn, sourceRow, otherItemId);
        Board.SetItemId(targetColumn, targetRow, thisItemId);
        Board.SetSpecialItemId(sourceColumn, sourceRow, otherSpecialId);
        Board.SetSpecialItemId(targetColumn, targetRow, thisSpecialId);

        Column = targetColumn;
        Row = targetRow;
        otherItem.Column = sourceColumn;
        otherItem.Row = sourceRow;

        if (sourceCell != null)
        {
            sourceCell.SetGridPosition(targetColumn, targetRow);
            sourceCell.AttachItem(this);
        }

        if (targetCell != null)
        {
            targetCell.SetGridPosition(sourceColumn, sourceRow);
            targetCell.AttachItem(otherItem);
        }

        StartCoroutine(MoveToPosition(targetColumn, targetRow));
        StartCoroutine(otherItem.MoveToPosition(sourceColumn, sourceRow));

        yield return new WaitForSeconds(_moveDuration);

        _isMoving = false;
        otherItem._isMoving = false;

        // After the visual swap, positions are:
        //   this      → (targetColumn, targetRow)
        //   otherItem → (sourceColumn, sourceRow)
        bool handledCombo = SpecialCombination.TryResolve(
            Board, this, otherItem,
            targetColumn, targetRow,
            sourceColumn, sourceRow);

        if (!handledCombo)
        {
            // Default: trigger any specials that were swapped (or involved).
            if (!string.IsNullOrEmpty(thisSpecialId))
                GetComponent<ISpecialItem>()?.TriggerSpecialItem();

            if (!string.IsNullOrEmpty(otherSpecialId))
                otherItem.GetComponent<ISpecialItem>()?.TriggerSpecialItem();

            Board.CheckMatches(sourceColumn, sourceRow, targetColumn, targetRow);
        }
        else
        {
            // Combination already cleared cells synchronously — force gravity
            // + cascade, otherwise holes stay on the board forever.
            var matchesHandler = Object.FindObjectOfType<MatchesHandler>();
            if (matchesHandler != null)
                matchesHandler.ProcessAfterClear(Board);
            else
                Board.CheckMatches();
        }
    }

    private IEnumerator PlayRejectedSwap(Item otherItem)
    {
        Vector2 direction = new Vector2(otherItem.Column - Column, otherItem.Row - Row).normalized;

        yield return StartCoroutine(PlayRejectedMove(direction));
        yield return StartCoroutine(otherItem.PlayRejectedMove(-direction));
    }

    private IEnumerator PlayRejectedMove(Vector2 direction)
    {
        if (_cachedTransform == null)
            yield break;

        Vector3 originalPosition = _cachedTransform.position;
        Vector3 sideDirection = direction.sqrMagnitude > 0.001f
            ? new Vector3(-direction.y, direction.x, 0f).normalized
            : Vector3.right;

        float distance = 0.06f;
        float duration = 0.07f;

        yield return MoveTransform(originalPosition, originalPosition + sideDirection * distance, duration);
        yield return MoveTransform(originalPosition + sideDirection * distance, originalPosition - sideDirection * distance, duration * 2f);
        yield return MoveTransform(originalPosition - sideDirection * distance, originalPosition, duration);
    }
}
