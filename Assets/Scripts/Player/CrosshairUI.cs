using UnityEngine;

public class CrosshairUI : MonoBehaviour
{
    public static CrosshairUI Instance;
    [SerializeField] GameObject crosshair;

    void Awake()
    {
        Instance = this;
    }

    public void Show(bool show)
    {
        if (crosshair != null)
            crosshair.SetActive(show);
    }
}
