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
        
        // Sécurité : on s'assure qu'il est bien caché au lancement
        if (container != null) container.style.display = DisplayStyle.None;
    }

    public void ShowUI()
    {
    Debug.Log("Demande d'affichage du menu Craft reçue !");
    
        if (container != null) 
        {
            container.style.display = DisplayStyle.Flex;
            Debug.Log("Le container a été affiché avec succès.");
        }
        else
        {
            Debug.LogError("Échec : Le container 'workbench-container' est introuvable. Vérifie le nom dans UI Builder.");
        }
    }

    public void HideUI()
    {
        if (container != null) container.style.display = DisplayStyle.None;
    }

}