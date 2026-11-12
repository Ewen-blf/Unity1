using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Gère le glisser-déposer (Drag & Drop) visuel des icônes entre les emplacements d'UI Toolkit 
/// (Hotbar et Établi) et déclenche la synchronisation réseau sur PlayerInventory.
/// </summary>
public class UIDragAndDropManager : MonoBehaviour
{
    private UIDocument uiDoc;
    private VisualElement ghostIcon;
    private VisualElement originalSlot;
    private PlayerInventory playerInventory;
    private bool isDragging = false;

    void OnEnable()
    {
        uiDoc = GetComponent<UIDocument>();
        playerInventory = GetComponentInParent<PlayerInventory>();
        
        if (uiDoc == null)
        {
            Debug.LogError("ERREUR : Le script UIDragAndDropManager n'est pas sur le même objet que le UIDocument (MainHUD) !");
            return;
        }

        var root = uiDoc.rootVisualElement;
        if (root == null) return;

        ghostIcon = root.Q<VisualElement>("ghost-icon");
        if (ghostIcon != null) 
        {
            ghostIcon.style.display = DisplayStyle.None;
            ghostIcon.style.position = Position.Absolute;
            ghostIcon.pickingMode = PickingMode.Ignore;
        }
        else
        {
            Debug.LogError("DragAndDrop : Impossible de trouver 'ghost-icon' dans UI Builder !");
        }
    }

    public void RegisterSlot(VisualElement slot)
    {
        if (!slot.ClassListContains("slot")) slot.AddToClassList("slot");

        slot.RegisterCallback<PointerDownEvent>(evt => OnPointerDown(evt, slot));
        slot.RegisterCallback<PointerMoveEvent>(OnPointerMove);
        slot.RegisterCallback<PointerUpEvent>(evt => OnPointerUp(evt, slot));
    }

    void OnPointerDown(PointerDownEvent evt, VisualElement slot)
    {
        VisualElement icon = slot.Q<VisualElement>("icon");
        if (icon == null) return;

        if (icon.style.backgroundImage.value.texture == null && icon.style.backgroundImage.value.sprite == null)
        {
            return;
        }

        if (evt.button == 0)
        {
            isDragging = true;
            originalSlot = slot;

            ghostIcon.style.backgroundImage = icon.style.backgroundImage;
            ghostIcon.style.display = DisplayStyle.Flex;

            icon.style.opacity = 0.2f;

            UpdateGhostPosition(evt.position);

            slot.CapturePointer(evt.pointerId);
            evt.StopPropagation();
        }
    }

    void OnPointerMove(PointerMoveEvent evt)
    {
        if (!isDragging || ghostIcon == null) return;
        UpdateGhostPosition(evt.position);
        evt.StopPropagation();
    }

    void UpdateGhostPosition(Vector2 mousePos)
    {
        ghostIcon.style.left = mousePos.x - 32;
        ghostIcon.style.top = mousePos.y - 32;
    }

    void OnPointerUp(PointerUpEvent evt, VisualElement slot)
    {
        if (!isDragging) return;
        isDragging = false;
        
        slot.ReleasePointer(evt.pointerId);
        if (ghostIcon != null) ghostIcon.style.display = DisplayStyle.None;
        
        VisualElement originalIcon = originalSlot.Q<VisualElement>("icon");
        if (originalIcon != null) originalIcon.style.opacity = 1f;

        VisualElement targetElement = uiDoc.rootVisualElement.panel.Pick(evt.position);
        VisualElement targetSlot = FindParentWithClass(targetElement, "slot");

        if (targetSlot != null && targetSlot != originalSlot)
        {
            VisualElement targetIcon = targetSlot.Q<VisualElement>("icon");

            if (targetIcon != null && originalIcon != null)
            {
                var tempImg = targetIcon.style.backgroundImage;
                targetIcon.style.backgroundImage = originalIcon.style.backgroundImage;
                originalIcon.style.backgroundImage = tempImg;
            }

            if (playerInventory == null) playerInventory = GetComponentInParent<PlayerInventory>();
            if (playerInventory != null)
            {
                playerInventory.SwapItemsRpc(originalSlot.name, targetSlot.name);
            }
        }
        
        evt.StopPropagation();
    }

    VisualElement FindParentWithClass(VisualElement element, string className)
    {
        while (element != null)
        {
            if (element.ClassListContains(className)) return element;
            element = element.parent;
        }
        return null;
    }
}