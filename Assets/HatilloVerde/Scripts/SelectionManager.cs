using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;

public class SelectionManager : MonoBehaviour
{
    [Header("Interaction")]
    public LayerMask interactableLayer;

    private InteractableHighlight lastHovered;

    void Update()
    {
        if (Pointer.current == null && !HatilloXrRig.IsActive) return;

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

        Ray ray;
        bool xrRay = HatilloXrRig.TryGetRightRay(out ray);
        if (!xrRay)
        {
            if (Pointer.current == null) return;
            Vector2 screenPos = Pointer.current.position.ReadValue();
            if (Camera.main == null) return;
            ray = Camera.main.ScreenPointToRay(screenPos);
        }

        RaycastHit hit;

        // HOVER
        if (Physics.Raycast(ray, out hit, xrRay ? 40f : 100f, interactableLayer))
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
        bool pressed = HatilloXrRig.IsActive
            ? HatilloXrRig.RightTriggerPressedThisFrame
            : Pointer.current != null && Pointer.current.press.wasPressedThisFrame;

        if (pressed)
        {
            // Solo bloquear si clickeamos un botón real
            if (IsPointerOverBlockingUI())
            {
                return;
            }

            if (lastHovered != null)
            {
                if (!lastHovered.CompareTag("Discoverable"))
                {
                    return;
                }

                InteractableInfo info =
                    lastHovered.GetComponentInParent<InteractableInfo>();

                if (info != null)
                {
                    Debug.Log("Abriendo info de: " + lastHovered.name + "!");
                    info.TriggerPopup();
                }
            }
        }
    }

    bool IsPointerOverBlockingUI()
    {
        if (EventSystem.current == null) return false;
        if (HatilloXrRig.IsActive)
            return false;

        PointerEventData pointerData = new PointerEventData(EventSystem.current);
        pointerData.position = Pointer.current.position.ReadValue();

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);

        foreach (RaycastResult result in results)
        {
            // SOLO bloquear botones reales
            if (result.gameObject.GetComponent<Button>() != null)
            {
                return true;
            }
        }

        return false;
    }
}