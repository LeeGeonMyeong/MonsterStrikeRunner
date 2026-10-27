using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class RunnerHoldInput : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public PlayerController player;
    private int? pointer;
    public void OnPointerDown(PointerEventData e)
    {
        if (pointer.HasValue || player == null || !player.CanAttack) return;
        pointer = e.pointerId;
        player.BeginCharge();
        GetComponent<Image>().color = new Color(0.8f, 0.45f, 0.15f);
    }
    public void OnPointerUp(PointerEventData e)
    {
        if (pointer != e.pointerId) return;
        pointer = null;
        player.ReleaseCharge();
        GetComponent<Image>().color = new Color(0.4f, 0.23f, 0.1f, 0.95f);
    }
    private void Cancel()
    {
        pointer = null;
        if (player != null) player.CancelCharge();
    }
    private void OnDisable() { Cancel(); }
    private void OnApplicationFocus(bool focused) { if (!focused) Cancel(); }
}
