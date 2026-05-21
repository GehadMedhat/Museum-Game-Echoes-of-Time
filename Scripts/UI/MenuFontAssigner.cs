using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using TMPro;

public class MenuFontAssigner : MonoBehaviour
{
    [Header("Drag SDF Font Assets here from the Project window")]
    [SerializeField] private TMP_FontAsset cinzelRegular;
    [SerializeField] private TMP_FontAsset cinzelDecBold;
    [SerializeField] private TMP_FontAsset crimsonRegular;
    [SerializeField] private TMP_FontAsset crimsonItalic;

    [Header("Or load automatically from Resources/Fonts/")]
    [SerializeField] private bool loadFromResources = false;

    private static readonly (string cssClass, int fontSlot)[] FontMap =
    {
        ("ornament",               0),
        ("title-sub",              0),
        ("divider-gem",            0),
        ("btn-info__label",        0),
        ("divein-label",           0),
        ("info-category",          0),
        ("btn-back",               0),
        ("levels-eyebrow",         0),
        ("card-badge",             0),
        ("card-level-num",         0),
        ("artifact-tag",           0),
        ("card-cta",               0),
        ("progress-label__left",   0),
        ("progress-label__right",  0),
        ("play-era-tag",           0),
        ("btn-play-action",        0),

        ("title-main",             1),
        ("btn-divein",             1),
        ("info-title",             1),
        ("levels-title",           1),
        ("card-era-name",          1),
        ("play-title",             1),

        ("info-body-text",         2),
        ("card-desc",              2),
        ("play-note__text",        2),

        ("tagline",                3),
        ("play-desc",              3),
    };

    private void Start()
    {
        // Debug: print exact asset names Unity sees
        var allTMP = Resources.LoadAll<TMP_FontAsset>("Fonts");
        foreach (var f in allTMP)
            Debug.Log("Found TMP font: " + f.name);

        if (loadFromResources)
            LoadFontsFromResources();

        Debug.Log($"Fonts — Cinzel:{cinzelRegular} CinzelDec:{cinzelDecBold} Crimson:{crimsonRegular} CrimsonI:{crimsonItalic}");

        if (!ValidateFonts()) return;

        var uiDoc = GetComponent<UIDocument>();
        if (uiDoc == null)
        {
            Debug.LogError("[MenuFontAssigner] No UIDocument on this GameObject.");
            return;
        }

        uiDoc.rootVisualElement
             .schedule
             .Execute(() => AssignFonts(uiDoc.rootVisualElement))
             .ExecuteLater(1);
    }

private void AssignFonts(VisualElement root)
{
    TMP_FontAsset[] slots = { cinzelRegular, cinzelDecBold, crimsonRegular, crimsonItalic };

    int assigned = 0;
    foreach (var (cssClass, fontSlot) in FontMap)
    {
        TMP_FontAsset tmp = slots[fontSlot];
        if (tmp == null) continue;

        // Use object → FontAsset (same underlying type in Unity 6)
        object boxed = tmp;
        UnityEngine.TextCore.Text.FontAsset textCoreFont = 
            boxed as UnityEngine.TextCore.Text.FontAsset;

        if (textCoreFont == null)
        {
            Debug.LogWarning($"Could not get TextCore font from {tmp.name}");
            continue;
        }

        var elements = root.Query(className: cssClass).ToList();
        foreach (var el in elements)
        {
            el.style.unityFontDefinition = FontDefinition.FromSDFFont(textCoreFont);
            assigned++;
        }
    }

    Debug.Log($"[MenuFontAssigner] Assigned fonts to {assigned} elements.");
}

    private void LoadFontsFromResources()
    {
        if (cinzelRegular  == null) cinzelRegular  = Resources.Load<TMP_FontAsset>("Fonts/Cinzel-Regular SDF");
        if (cinzelDecBold  == null) cinzelDecBold  = Resources.Load<TMP_FontAsset>("Fonts/CinzelDecorative-Bold SDF");
        if (crimsonRegular == null) crimsonRegular = Resources.Load<TMP_FontAsset>("Fonts/CrimsonText-Regular SDF");
        if (crimsonItalic  == null) crimsonItalic  = Resources.Load<TMP_FontAsset>("Fonts/CrimsonText-Italic SDF");
    }

    private bool ValidateFonts()
    {
        bool ok = true;
        if (cinzelRegular  == null) { Debug.LogError("[MenuFontAssigner] cinzelRegular missing!");  ok = false; }
        if (cinzelDecBold  == null) { Debug.LogError("[MenuFontAssigner] cinzelDecBold missing!");  ok = false; }
        if (crimsonRegular == null) { Debug.LogError("[MenuFontAssigner] crimsonRegular missing!"); ok = false; }
        if (crimsonItalic  == null) { Debug.LogError("[MenuFontAssigner] crimsonItalic missing!");  ok = false; }
        return ok;
    }
}
