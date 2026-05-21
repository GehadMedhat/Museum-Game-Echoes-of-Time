using UnityEngine;
using UnityEngine.UIElements;
using Cursor = UnityEngine.Cursor;

/// <summary>
/// Attach to a new GameObject called "InteractHint" (with a UIDocument).
/// Hides itself whenever a puzzle or note panel is open so it never
/// overlaps those UIs.
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class InteractHintUI : MonoBehaviour
{
    [SerializeField] private string hintText = "Press  [E]  to interact with exhibits";

    private VisualElement _root;
    private Label         _label;

    private void Awake()
    {
        var doc = GetComponent<UIDocument>();
        _root  = doc.rootVisualElement.Q("hint-root");
        _label = doc.rootVisualElement.Q<Label>("hint-label");

        if (_label != null && !string.IsNullOrEmpty(hintText))
            _label.text = hintText;
    }

    private void Update()
    {
        // Hide whenever any other UI panel is open
        bool anyUIOpen = (ModernNoteUI.Instance  != null && ModernNoteUI.Instance.IsOpen);

        // Also hide when circuit puzzle is open (check via cursor state as fallback)
        if (!anyUIOpen)
            anyUIOpen = (Cursor.lockState == CursorLockMode.None && !anyUIOpen);

        _root.style.display = anyUIOpen ? DisplayStyle.None : DisplayStyle.Flex;
    }
}
