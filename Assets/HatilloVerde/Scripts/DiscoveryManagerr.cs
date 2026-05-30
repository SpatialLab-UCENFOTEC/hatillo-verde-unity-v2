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
            if (obj.CompareTag("Discoverable"))
            {
                totalObjects++;
            }
        }

        UpdateUI();
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
            counterText.text = discoveredObjects + " / " + totalObjects;
        }
    }
}