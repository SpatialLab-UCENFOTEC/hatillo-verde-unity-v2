using UnityEngine;
using UnityEngine.InputSystem;

public class SelectionManager : MonoBehaviour
{
    private InteractableHighlight lastHovered;

    void Update()
    {
        if (Pointer.current == null) return;

        // Bloquear interaccion 3D si hay UI abierta
        if (InfoPanelController.IsUIOpen)
        {
            if (lastHovered != null)
            {
                lastHovered.OnHoverExit();
                lastHovered = null;
            }
            return;
        }

        Vector2 screenPos = Pointer.current.position.ReadValue();
        Ray ray = Camera.main.ScreenPointToRay(screenPos);
        RaycastHit hit;

        // HOVER
        if (Physics.Raycast(ray, out hit, 100f))
        {
            InteractableHighlight newHover =
                hit.transform.GetComponentInParent<InteractableHighlight>();

            if (newHover != lastHovered)
            {
                if (lastHovered != null) lastHovered.OnHoverExit();
                if (newHover != null) newHover.OnHoverEnter();
                lastHovered = newHover;
            }
        }
        else
        {
            if (lastHovered != null)
            {
                lastHovered.OnHoverExit();
                lastHovered = null;
            }
        }

        // CLICK
        if (Pointer.current.press.wasPressedThisFrame)
        {
            if (lastHovered != null)
            {
                InteractableInfo info =
                    lastHovered.GetComponent<InteractableInfo>();

                if (info != null)
                {
                    Debug.Log("Abriendo info de: " + lastHovered.name + "!");
                    info.TriggerPopup();
                }
            }
        }
    }
}