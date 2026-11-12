using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Contrôle l'affichage du conteneur UI Toolkit de l'établi, enregistre ses emplacements 
/// auprès du système de Drag & Drop et relie le bouton de fabrication au joueur.
/// </summary>
public class WorkbenchUIController : MonoBehaviour
{
    private UIDocument uiDocument;
    private VisualElement container;
    private Button craftBtn;
    private PlayerWorkbench playerWorkbench;

    void OnEnable()
    {
        uiDocument = GetComponent<UIDocument>();
        playerWorkbench = GetComponentInParent<PlayerWorkbench>();

        if (uiDocument == null || uiDocument.rootVisualElement == null) return;
        
        container = uiDocument.rootVisualElement.Q<VisualElement>("workbench-container");
        if (container != null) container.style.display = DisplayStyle.None;

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
                    icon.pickingMode = PickingMode.Ignore;
                    slot.Add(icon);
                }
            }
        }

        craftBtn = uiDocument.rootVisualElement.Q<Button>("craft-button");
        if (craftBtn != null)
        {
            craftBtn.clicked += OnCraftButtonClicked;
        }
    }

    void OnDisable()
    {
        if (craftBtn != null)
        {
            craftBtn.clicked -= OnCraftButtonClicked;
        }
    }

    private void OnCraftButtonClicked()
    {
        if (playerWorkbench == null) playerWorkbench = GetComponentInParent<PlayerWorkbench>();
        
        if (playerWorkbench != null)
        {
            playerWorkbench.StartPhysicalCrafting();
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