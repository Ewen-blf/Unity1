using UnityEngine;
using UnityEngine.UIElements;

public class WorkbenchUIController : MonoBehaviour
{
    private UIDocument uiDocument;
    private VisualElement container;

    void OnEnable()
    {
        uiDocument = GetComponent<UIDocument>();
        if (uiDocument.rootVisualElement == null) return;
        
        container = uiDocument.rootVisualElement.Q<VisualElement>("workbench-container");
        if (container != null) container.style.display = DisplayStyle.None;

        // --- NOUVEAU : Inscription des cases ---
        UIDragAndDropManager dragManager = GetComponent<UIDragAndDropManager>();
        if (dragManager != null && container != null)
        {
            // On cherche toutes les cases manuelles (wb-0, wb-1, wb-2) et on les inscrit
            var wbSlots = container.Query<VisualElement>(className: "slot").ToList();
            foreach (var slot in wbSlots)
            {
                dragManager.RegisterSlot(slot);
                
                // Optionnel: on s'assure qu'elles ont bien un enfant "icon" pour recevoir l'image
                if (slot.Q<VisualElement>("icon") == null)
                {
                    VisualElement icon = new VisualElement { name = "icon" };
                    icon.style.flexGrow = 1;
                    slot.Add(icon);
                }
            }
        }
    }

    public void ToggleUI(bool isVisible)
    {
        if (container != null)
        {
            container.style.display = isVisible ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }

}