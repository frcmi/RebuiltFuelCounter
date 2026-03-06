using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(RectTransform))]
public class ROIDragHandler : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    public FuelDetector.FuelDetectorManager fuelDetectorManager;
    public System.Action onDragEnd;
    public System.Action onRefreshNeeded;

    private enum DragMode { None, Top, Bottom }
    private DragMode _currentMode = DragMode.None;
    private RectTransform _rectTransform;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!fuelDetectorManager || !_rectTransform) return;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_rectTransform, eventData.position, eventData.pressEventCamera, out Vector2 localPoint))
        {
            Rect r = _rectTransform.rect;
            // Convert to UV space (0-1)
            Vector2 uv = new Vector2((localPoint.x - r.x) / r.width, (localPoint.y - r.y) / r.height);
            
            Rect roi = fuelDetectorManager.regionOfInterest;
            float threshold = 0.12f; // Hit test threshold for edge selection
            
            // Horizontal bounds check (allow some padding)
            if (uv.x >= roi.xMin - 0.2f && uv.x <= roi.xMax + 0.2f)
            {
                if (Mathf.Abs(uv.y - roi.yMax) < threshold)
                    _currentMode = DragMode.Top;
                else if (Mathf.Abs(uv.y - roi.yMin) < threshold)
                    _currentMode = DragMode.Bottom;
            }
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_currentMode == DragMode.None || !fuelDetectorManager || !_rectTransform) return;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_rectTransform, eventData.position, eventData.pressEventCamera, out Vector2 localPoint))
        {
            Rect r = _rectTransform.rect;
            Vector2 uv = new Vector2((localPoint.x - r.x) / r.width, (localPoint.y - r.y) / r.height);
            uv.y = Mathf.Clamp01(uv.y);

            Rect roi = fuelDetectorManager.regionOfInterest;
            float minHeight = 0.05f;
            
            if (_currentMode == DragMode.Top)
                roi.yMax = Mathf.Max(uv.y, roi.yMin + minHeight);
            else
                roi.yMin = Mathf.Min(uv.y, roi.yMax - minHeight);
            
            fuelDetectorManager.regionOfInterest = roi;
            onRefreshNeeded?.Invoke();
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (_currentMode != DragMode.None)
        {
            _currentMode = DragMode.None;
            onDragEnd?.Invoke();
        }
    }
}
