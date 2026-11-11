using UnityEngine;
using UnityEngine.UIElements;

public class UIDragAndDropManager : MonoBehaviour
{
    private UIDocument uiDoc;
    private VisualElement ghostIcon;
    private VisualElement originalSlot;
    private bool isDragging = false;

    void OnEnable()
    {
        uiDoc = GetComponent<UIDocument>();
        var root = uiDoc.rootVisualElement;

        // 1. On récupère notre icône fantôme
        ghostIcon = root.Q<VisualElement>("ghost-icon");
        if (ghostIcon != null) ghostIcon.style.display = DisplayStyle.None;

        // 2. On trouve TOUTES les cases du jeu (Hotbar + Workbench)
        UQueryBuilder<VisualElement> allSlots = root.Query<VisualElement>(className: "slot");
        allSlots.ForEach(SetupSlot);
    }

    void SetupSlot(VisualElement slot)
    {
        // On abonne chaque case aux événements de la souris
        slot.RegisterCallback<PointerDownEvent>(evt => OnPointerDown(evt, slot));
        slot.RegisterCallback<PointerMoveEvent>(OnPointerMove);
        slot.RegisterCallback<PointerUpEvent>(evt => OnPointerUp(evt, slot));
    }

    void OnPointerDown(PointerDownEvent evt, VisualElement slot)
    {
        // Clic gauche (0) ET la case contient une image
        if (evt.button == 0 && slot.style.backgroundImage.value.texture != null)
        {
            isDragging = true;
            originalSlot = slot;

            // Le fantôme copie l'image et apparaît
            ghostIcon.style.backgroundImage = slot.style.backgroundImage;
            ghostIcon.style.display = DisplayStyle.Flex;

            // La case d'origine devient semi-transparente pour montrer qu'on la vide
            slot.style.opacity = 0.2f;

            UpdateGhostPosition(evt.position);

            // TRÈS IMPORTANT : On "capture" la souris pour ne pas perdre le clic si on bouge très vite
            slot.CapturePointer(evt.pointerId);
            evt.StopPropagation();
        }
    }

    void OnPointerMove(PointerMoveEvent evt)
    {
        if (!isDragging) return;
        UpdateGhostPosition(evt.position);
        evt.StopPropagation();
    }

    void UpdateGhostPosition(Vector2 mousePos)
    {
        // On centre le fantôme sur le curseur (32 = la moitié de 64px)
        ghostIcon.style.left = mousePos.x - 32;
        ghostIcon.style.top = mousePos.y - 32;
    }

    void OnPointerUp(PointerUpEvent evt, VisualElement slot)
    {
        if (!isDragging) return;
        isDragging = false;
        
        // On libère la souris et on cache le fantôme
        slot.ReleasePointer(evt.pointerId);
        ghostIcon.style.display = DisplayStyle.None;
        originalSlot.style.opacity = 1f;

        // On tire un "laser" pour voir sur quel élément de l'UI on a relâché le clic
        VisualElement targetElement = uiDoc.rootVisualElement.panel.Pick(evt.position);
        VisualElement targetSlot = FindParentWithClass(targetElement, "slot");

        if (targetSlot != null && targetSlot != originalSlot)
        {
            Debug.Log($"BINGO ! Transfert de l'objet depuis [{originalSlot.name}] vers [{targetSlot.name}]");
            // TODO: Appeler les scripts du Joueur pour échanger les données en mémoire
        }
        else
        {
            Debug.Log("Relâché dans le vide. L'objet retourne à sa place.");
        }
        
        evt.StopPropagation();
    }

    // Fonction utilitaire pour remonter l'arbre UI jusqu'à trouver une case
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