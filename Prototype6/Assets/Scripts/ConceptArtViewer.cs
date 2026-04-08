using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class ConceptArtViewer : MonoBehaviour
{
    [Header("Images (drag sprites here)")]
    public Sprite[] images;

    [Header("References")]
    public Button openButton;

    private GameObject viewer;
    private Image displayImage;
    private int currentIndex;
    private bool isOpen;

    void Start()
    {
        BuildViewer();
        viewer.SetActive(false);

        if (openButton != null)
            openButton.onClick.AddListener(Open);
    }

    void Update()
    {
        if (!isOpen) return;

        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Close();
            return;
        }

        if (Keyboard.current.rightArrowKey.wasPressedThisFrame)
            Navigate(1);
        if (Keyboard.current.leftArrowKey.wasPressedThisFrame)
            Navigate(-1);
    }

    void Open()
    {
        if (images == null || images.Length == 0) return;
        currentIndex = 0;
        isOpen = true;
        viewer.SetActive(true);
        ShowCurrent();
    }

    void Close()
    {
        isOpen = false;
        viewer.SetActive(false);
    }

    void Navigate(int dir)
    {
        if (images.Length == 0) return;
        currentIndex = (currentIndex + dir + images.Length) % images.Length;
        ShowCurrent();
    }

    void ShowCurrent()
    {
        Sprite sprite = images[currentIndex];
        displayImage.sprite = sprite;
        displayImage.preserveAspect = true;
    }

    void BuildViewer()
    {
        Canvas canvas = GetComponentInParent<Canvas>();

        viewer = new GameObject("ConceptArtViewer", typeof(RectTransform));
        viewer.transform.SetParent(canvas.transform, false);
        RectTransform vrt = viewer.GetComponent<RectTransform>();
        vrt.anchorMin = Vector2.zero;
        vrt.anchorMax = Vector2.one;
        vrt.offsetMin = Vector2.zero;
        vrt.offsetMax = Vector2.zero;

        // Black background
        Image bg = viewer.AddComponent<Image>();
        bg.color = Color.black;

        // Center image with padding
        GameObject imgObj = new GameObject("Display", typeof(RectTransform));
        imgObj.transform.SetParent(viewer.transform, false);
        RectTransform irt = imgObj.GetComponent<RectTransform>();
        irt.anchorMin = new Vector2(0.05f, 0.05f);
        irt.anchorMax = new Vector2(0.95f, 0.95f);
        irt.offsetMin = Vector2.zero;
        irt.offsetMax = Vector2.zero;

        displayImage = imgObj.AddComponent<Image>();
        displayImage.preserveAspect = true;
    }
}
