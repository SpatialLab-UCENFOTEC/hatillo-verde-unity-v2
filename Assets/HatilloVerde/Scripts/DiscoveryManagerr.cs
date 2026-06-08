using UnityEngine;
using TMPro;

public class DiscoveryManager : MonoBehaviour
{
    public static DiscoveryManager Instance;

    [Header("UI")]
    public TextMeshProUGUI counterText;

    [Header("Stats")]
    private int totalObjects;
    private int discoveredObjects = 0;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        InteractableInfo[] allObjects =
            FindObjectsByType<InteractableInfo>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        totalObjects = 0;

        foreach (InteractableInfo obj in allObjects)
        {
            // Solo cuentan los elementos realmente interactivos: tag Discoverable,
            // el componente InteractableInfo habilitado y un Collider habilitado.
            // Se filtra por collider HABILITADO (no por GameObject activo en
            // jerarquía) para incluir las épocas que arrancan desactivadas.
            if (!obj.CompareTag("Discoverable")) continue;
            if (!obj.enabled) continue;
            if (!HasEnabledCollider(obj)) continue;

            totalObjects++;
        }

        UpdateUI();
    }

    // True si el objeto (o un hijo, incluso inactivo) tiene un Collider habilitado.
    // El raycast de interacción usa colliders, así que un collider deshabilitado
    // hace el elemento no interactivo y no debe contarse.
    bool HasEnabledCollider(Component obj)
    {
        Collider[] colliders = obj.GetComponentsInChildren<Collider>(true);
        foreach (Collider col in colliders)
        {
            if (col.enabled) return true;
        }
        return false;
    }

    public void RegisterDiscovery()
    {
        discoveredObjects++;

        UpdateUI();

        Debug.Log("Objetos encontrados: " + discoveredObjects);

        if (discoveredObjects >= totalObjects)
        {
            Debug.Log("¡Todos los objetos encontrados!");
        }
    }
    public int GetDiscoveredCount()
    {
        return discoveredObjects;
    }

    public int GetTotalCount()
    {
        return totalObjects;
    }

    void UpdateUI()
    {
        if (counterText != null)
        {
            counterText.text = "Elementos encontrados: " + discoveredObjects + " / " + totalObjects;
        }
    }
}