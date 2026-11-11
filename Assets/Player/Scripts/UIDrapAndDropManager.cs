using UnityEngine;
using UnityEngine.UIElements;
using System.Linq;

public class UIDragAndDropManager : MonoBehaviour
{
    private UIDocument uiDoc;
    private VisualElement ghostIcon;
    private VisualElement originalSlot;
    private bool isDragging = false;

    void OnEnable()
    {
        uiDoc = GetComponent<UIDocument>();
        
        if (uiDoc == null)
        {
            Debug.LogError("🚨 ERREUR : Le script DragAndDropManager n'est pas sur le même objet que le UIDocument (MainHUD) !");
            return;
        }

        var root = uiDoc.rootVisualElement;
        
        if (root == null) 
        {
            Debug.LogWarning("⚠️ ATTENTION : Le UIDocument est là, mais aucun fichier UI n'y est assigné.");
            return;
        }

        ghostIcon = root.Q<VisualElement>("ghost-icon");
        if (ghostIcon != null) 
        {
            ghostIcon.style.display = DisplayStyle.None;
            ghostIcon.style.position = Position.Absolute;
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

        if (icon == null)
        {
            Debug.LogError($"❌ Annulation : La case {slot.name} n'a pas d'enfant nommé 'icon' !");
            return;
        }

        if (icon.style.backgroundImage.value.texture == null && icon.style.backgroundImage.value.sprite == null)
        {
            return; // Case vide, on annule silencieusement
        }

        if (evt.button == 0) // Clic gauche
        {
            isDragging = true;
            originalSlot = slot;

            ghostIcon.style.backgroundImage = icon.style.backgroundImage;
            ghostIcon.style.display = DisplayStyle.Flex;

            icon.style.opacity = 0.2f;

            UpdateGhostPosition(evt.position);

            slot.CapturePointer(evt.pointerId);
            evt.StopPropagation();
            
            Debug.Log($"🖱️ Drag démarré depuis : {slot.name}");
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
        // On centre l'icône de 64x64 sur la pointe du curseur (-32px)
        ghostIcon.style.left = mousePos.x - 32;
        ghostIcon.style.top = mousePos.y - 32;
    }

    void OnPointerUp(PointerUpEvent evt, VisualElement slot)
    {
        if (!isDragging) return;
        isDragging = false;
        
        // 1. Libération de la souris et nettoyage visuel
        slot.ReleasePointer(evt.pointerId);
        if (ghostIcon != null) ghostIcon.style.display = DisplayStyle.None;
        
        VisualElement originalIcon = originalSlot.Q<VisualElement>("icon");
        if (originalIcon != null) originalIcon.style.opacity = 1f;

        // 2. Tir de laser pour trouver la case d'arrivée
        VisualElement targetElement = uiDoc.rootVisualElement.panel.Pick(evt.position);
        VisualElement targetSlot = FindParentWithClass(targetElement, "slot");

        // 3. Traitement du transfert
        if (targetSlot != null && targetSlot != originalSlot)
        {
            VisualElement targetIcon = targetSlot.Q<VisualElement>("icon");

            // --- A. ÉCHANGE VISUEL IMMÉDIAT ---
            if (targetIcon != null && originalIcon != null)
            {
                var tempImg = targetIcon.style.backgroundImage;
                targetIcon.style.backgroundImage = originalIcon.style.backgroundImage;
                originalIcon.style.backgroundImage = tempImg;
            }

            // --- B. ENVOI AU SERVEUR ---
            PlayerInventory inventory = GetComponentInParent<PlayerInventory>();
            if (inventory != null)
            {
                inventory.SwapItemsRpc(originalSlot.name, targetSlot.name);
            }

            Debug.Log($"✅ TRANSFERT RÉUSSI : {originalSlot.name} ---> {targetSlot.name}");
        }
        else
        {
            Debug.Log($"❌ Annulé. La souris a été relâchée sur : {(targetSlot != null ? targetSlot.name : "le vide")}");
        }
        
        evt.StopPropagation();
    }

    // Fonction utilitaire pour remonter l'arbre UI
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