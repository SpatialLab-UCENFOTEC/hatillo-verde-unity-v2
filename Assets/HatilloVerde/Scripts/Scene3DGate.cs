using UnityEngine;

// Enables/disables the parent of the three 3D environments so nothing 3D runs
// (render, Update loops, particles, ambient audio) while an opaque full-screen
// UI covers it: intro, outro, and the fully-black stretch of a transition.
public class Scene3DGate : MonoBehaviour
{
    public static Scene3DGate Instance { get; private set; }

    [Tooltip("Parent GameObject of the 3D environments (---------------SCENES----------------).")]
    [SerializeField] private GameObject scenesRoot;

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void Show()
    {
        if (scenesRoot != null && !scenesRoot.activeSelf)
        {
            scenesRoot.SetActive(true);
        }
    }

    public void Hide()
    {
        if (scenesRoot != null && scenesRoot.activeSelf)
        {
            scenesRoot.SetActive(false);
        }
    }
}
