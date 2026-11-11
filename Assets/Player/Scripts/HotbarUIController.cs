using UnityEngine;
using UnityEngine.UIElements;

public class HotbarUIController : MonoBehaviour
{
    private UIDocument uiDocument;
    private VisualElement[] slots = new VisualElement[9];
    private VisualElement[] icons = new VisualElement[9];

    void OnEnable()
    {
    uiDocument = GetComponent<UIDocument>();
    var root = uiDocument.rootVisualElement;
    
    if (root == null) return; // Sécurité

    var container = root.Q<VisualElement>("hotbar-container");

    // Si le conteneur est introuvable, on affiche une erreur claire dans la console
    if (container == null)
        {
        Debug.LogError("Impossible de trouver 'hotbar-container' ! Vérifie le nom dans UI Builder.");
        return;
        }

    container.Clear(); // Nettoyage de sécurité avant de générer

    // Génération automatique des 9 cases
    for (int i = 0; i < 9; i++)
        {
        VisualElement slot = new VisualElement();
        slot.style.width = 64;
        slot.style.height = 64;
        slot.style.marginLeft = 4;
        slot.style.marginRight = 4;
        slot.style.backgroundColor = new Color(0.15f, 0.15f, 0.15f, 0.85f);
        
        SetSlotBorder(slot, 2, Color.gray);

        VisualElement icon = new VisualElement();
        icon.style.flexGrow = 1;
        icon.style.marginLeft = 6;
        icon.style.marginRight = 6;
        icon.style.marginTop = 6;
        icon.style.marginBottom = 6;
        slot.Add(icon);

        Label num = new Label((i + 1).ToString());
        num.style.position = Position.Absolute;
        num.style.top = 2;
        num.style.left = 5;
        num.style.color = Color.white;
        num.style.fontSize = 12;
        slot.Add(num);

        container.Add(slot);
        slots[i] = slot;
        icons[i] = icon;
        }

    UpdateSelection(0);
    }

    public void UpdateSelection(int selectedIndex)
    {
        for (int i = 0; i < 9; i++)
        {
            if (i == selectedIndex)
                SetSlotBorder(slots[i], 4, Color.yellow); // Case sélectionnée en jaune
            else
                SetSlotBorder(slots[i], 2, Color.gray);
        }
    }

    public void UpdateSlotIcon(int index, Sprite sprite)
    {
        if (sprite != null)
            icons[index].style.backgroundImage = new StyleBackground(sprite);
        else
            icons[index].style.backgroundImage = null;
    }

    private void SetSlotBorder(VisualElement el, float width, Color color)
    {
        el.style.borderTopWidth = el.style.borderBottomWidth = el.style.borderLeftWidth = el.style.borderRightWidth = width;
        el.style.borderTopColor = el.style.borderBottomColor = el.style.borderLeftColor = el.style.borderRightColor = color;
    }
}