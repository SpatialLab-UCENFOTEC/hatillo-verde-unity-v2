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
        if (Physics.Raycast(ray, out hit, 100f, interactableLayer))
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