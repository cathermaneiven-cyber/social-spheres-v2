using UnityEngine;
using UnityEngine.UI;

public class HandUIButtonAutoSetup : MonoBehaviour
{
    public string handTag = "HandTag";
    public float cooldown = 0.35f;
    public Vector3 colliderPadding = new Vector3(8f, 8f, 0.02f);

    private void Awake()
    {
        Debug.Log("[HandUIButtonAutoSetup] Running setup...");
        SetupButtons();
    }

    [ContextMenu("Setup Buttons")]
    public void SetupButtons()
    {
        Button[] buttons = GetComponentsInChildren<Button>(true);

        Debug.Log("[HandUIButtonAutoSetup] Found " + buttons.Length + " buttons");

        foreach (Button button in buttons)
        {
            Debug.Log("[HandUIButtonAutoSetup] Setting up button: " + button.name);

            BoxCollider box = button.GetComponent<BoxCollider>();

            if (box == null)
            {
                box = button.gameObject.AddComponent<BoxCollider>();
                Debug.Log("[HandUIButtonAutoSetup] Added BoxCollider to " + button.name);
            }

            RectTransform rt = button.GetComponent<RectTransform>();

            if (rt != null)
            {
                Vector2 size = rt.rect.size;
                box.size = new Vector3(
                    Mathf.Abs(size.x) + colliderPadding.x,
                    Mathf.Abs(size.y) + colliderPadding.y,
                    colliderPadding.z
                );
                box.center = Vector3.zero;

                Debug.Log("[HandUIButtonAutoSetup] Set collider size for " + button.name + " to " + box.size);
            }

            box.isTrigger = true;

            HandUIButton press = button.GetComponent<HandUIButton>();

            if (press == null)
            {
                press = button.gameObject.AddComponent<HandUIButton>();
                Debug.Log("[HandUIButtonAutoSetup] Added HandUIButton to " + button.name);
            }

            press.button = button;
            press.handTag = handTag;
            press.cooldown = cooldown;
        }

        Debug.Log("[HandUIButtonAutoSetup] Setup COMPLETE");
    }
}

public class HandUIButton : MonoBehaviour
{
    public Button button;
    public string handTag = "HandTag";
    public float cooldown = 0.35f;

    private float lastPressTime;

    private void Reset()
    {
        button = GetComponent<Button>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(handTag))
            return;

        Debug.Log("[HandUIButton] Hand touched button: " + gameObject.name);

        if (Time.time - lastPressTime < cooldown)
        {
            Debug.Log("[HandUIButton] Cooldown active, ignoring press");
            return;
        }

        lastPressTime = Time.time;

        if (button != null && button.interactable)
        {
            Debug.Log("[HandUIButton] CLICKED: " + gameObject.name);
            button.onClick.Invoke();
        }
        else
        {
            Debug.Log("[HandUIButton] Button missing or not interactable: " + gameObject.name);
        }
    }
}