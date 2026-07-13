using UnityEngine;
using TMPro;

public class DiscoveryManager : MonoBehaviour
{
    public static DiscoveryManager Instance;

    [Header("UI")]
    public TextMeshProUGUI counterText;

    [Header("Stats")]
    // Total fijo de elementos de la experiencia (hardcodeado a pedido).
    private int totalObjects = 31;
    private int discoveredObjects = 0;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        UpdateUI();
    }

    public void RegisterDiscovery()
    {
        // No permitir superar el total fijo.
        if (discoveredObjects >= totalObjects) return;

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