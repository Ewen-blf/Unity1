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

        // --- Inscription des cases ---
        UIDragAndDropManager dragManager = GetComponent<UIDragAndDropManager>();
        if (dragManager != null && container != null)
        {
            var wbSlots = container.Query<VisualElement>(className: "slot").ToList();
            foreach (var slot in wbSlots)
            {
                dragManager.RegisterSlot(slot);
                
                if (slot.Q<VisualElement>("icon") == null)
                {
                    VisualElement icon = new VisualElement { name = "icon" };
                    icon.style.flexGrow = 1;
                    slot.Add(icon);
                }
            }
        }

        // --- NOUVEAU : Connexion du bouton Fabriquer ---
        Button craftBtn = uiDocument.rootVisualElement.Q<Button>("craft-button");
        if (craftBtn != null)
        {
            craftBtn.clicked += OnCraftButtonClicked;
        }
    }

    private void OnCraftButtonClicked()
    {
        PlayerWorkbench pw = GetComponentInParent<PlayerWorkbench>();
        if (pw != null)
        {
            pw.StartPhysicalCrafting();
        }
        else
        {
            Debug.LogError("Impossible de trouver PlayerWorkbench sur le parent !");
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