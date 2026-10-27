using UnityEngine;
using UnityEngine.EventSystems;

public class RunnerSwipeInput : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    public PlayerController player;
    private int? pointer;
    private Vector2 start;
    public void OnPointerDown(PointerEventData e)
    {
        if (pointer.HasValue || player == null ||
            GameManager.Instance == null || !GameManager.Instance.CanControl) return;
        pointer = e.pointerId;
        start = e.position;
    }
    // Keep pointer-up delivery on this surface during a drag.
    public void OnDrag(PointerEventData e) { }
    public void OnPointerUp(PointerEventData e)
    {
        if (pointer != e.pointerId) return;
        pointer = null;
        if (player == null) return;
        Vector2 delta = e.position - start;
        float threshold = Mathf.Max(25f, Screen.width * 50f / 480f);
        if (Mathf.Abs(delta.x) < threshold || Mathf.Abs(delta.x) <= Mathf.Abs(delta.y)) return;
        if (delta.x > 0f) player.MoveRight(); else player.MoveLeft();
    }
    private void OnDisable() { pointer = null; }
    private void OnApplicationFocus(bool focused) { if (!focused) pointer = null; }
}
