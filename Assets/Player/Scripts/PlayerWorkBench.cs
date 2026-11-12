using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;
using Unity.Cinemachine;
using System.Collections;
using System.Collections.Generic;

public class PlayerWorkbench : NetworkBehaviour
{
    [Header("Références")]
    public Transform playerCamera;
    public float interactRange = 3f;
    public int[] workbenchItems = new int[3];

    [Header("Visuel Joueur")]
    [Tooltip("Glisse ici l'objet de ton bras/arme (Dummy) pour qu'il se cache automatiquement à l'établi")]
    public GameObject weaponDummy; 

    private MonoBehaviour playerMovement;
    private MonoBehaviour playerLook;
    private CinemachineBrain brain;

    private bool isAtWorkbench = false;
    private CinemachineCamera activeWorkbenchCam;
    public WorkbenchUIController workbenchUI;

    [Header("Crafting Data")]
    // Mémorise de quelle case de la Hotbar l'objet provient
    private int[] originHotbarSlots = new int[3]; 
    private PlayerInventory playerInventory;
    
    [Header("Base de données")]
    public ItemDatabase database;
    
    private Workbench currentWorkbench;
    private List<GameObject> spawnedCraftObjects = new List<GameObject>();

    public override void OnNetworkSpawn()
    {
        if (!IsOwner && workbenchUI != null)
        {
            workbenchUI.gameObject.SetActive(false);
        }
    }

    void Start()
    {
        // ⚠️ Je suppose ici que tes scripts s'appellent exactement "PlayerMovement" et "PlayerLook".
        // Si c'est différent (ex: "FirstPersonController"), dis-le moi !
        playerMovement = GetComponent("PlayerMovement") as MonoBehaviour;
        playerLook = GetComponent("PlayerLook") as MonoBehaviour; 
        
        brain = playerCamera.GetComponent<CinemachineBrain>();
        playerInventory = GetComponent<PlayerInventory>();
    }

    void OnInteract(InputValue value)
    {
        if (!IsOwner || !value.isPressed || isAtWorkbench) return;

        Ray ray = new Ray(playerCamera.position, playerCamera.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, interactRange))
        {
            Workbench workbench = hit.collider.GetComponent<Workbench>();
            if (workbench != null && workbench.workbenchCam != null)
            {
                EnterWorkbench(workbench.workbenchCam);
            }
        }
    }

    void EnterWorkbench(CinemachineCamera cam)
    {
        isAtWorkbench = true;
        activeWorkbenchCam = cam;

        // 1. Bloque les mouvements
        if (playerMovement != null) playerMovement.enabled = false;
        if (playerLook != null) playerLook.enabled = false;

        // 2. Cache l'arme (Dummy) !
        if (weaponDummy != null) weaponDummy.SetActive(false);

        // 3. Affiche la souris
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        activeWorkbenchCam.Priority = 100;
        if (workbenchUI == null) 
        {
            workbenchUI = FindFirstObjectByType<WorkbenchUIController>();
        }

        if (workbenchUI != null) 
        {
            workbenchUI.ToggleUI(true);
        }
        else
        {
            Debug.LogError("Le Player n'arrive pas à trouver le WorkbenchUIController !");
        }
    }

    void OnExit(InputValue value)
    {
        if (!IsOwner || !value.isPressed || !isAtWorkbench) return;
        ExitWorkbench();
    }

    public void ExitWorkbench()
    {
        isAtWorkbench = false;

        if (workbenchUI != null) workbenchUI.ToggleUI(false); 

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (activeWorkbenchCam != null)
        {
            activeWorkbenchCam.Priority = 0;
            
            Workbench wb = activeWorkbenchCam.GetComponentInParent<Workbench>();
            if (wb != null && wb.WorkbenchTopCamera != null)
            {
                wb.WorkbenchTopCamera.Priority = 0;
            }
            
            activeWorkbenchCam = null;
        }

        StartCoroutine(WaitAndEnableControl());
    }

    IEnumerator WaitAndEnableControl()
    {
        yield return new WaitUntil(() => !brain.IsBlending);

        if (!isAtWorkbench) 
        {
            // Réactive les mouvements
            if (playerMovement != null) playerMovement.enabled = true;
            if (playerLook != null) playerLook.enabled = true;
            
            // Fait réapparaître l'arme (Dummy) !
            if (weaponDummy != null) weaponDummy.SetActive(true);
        }
    }

    public void StartPhysicalCrafting()
    {
        if (workbenchUI != null) workbenchUI.ToggleUI(false);

        if (activeWorkbenchCam != null)
        {
            Workbench wb = activeWorkbenchCam.GetComponentInParent<Workbench>();
            
            if (wb == null) return;

            if (wb.WorkbenchTopCamera != null)
            {
                wb.WorkbenchTopCamera.Priority = 110;
            }

            if (database == null) return;

            for (int i = 0; i < workbenchItems.Length; i++)
            {
                int itemId = workbenchItems[i];
                
                if (itemId > 0)
                {
                    ItemData itemData = database.GetItemById(itemId);

                    if (itemData == null || itemData.Prefab3D == null)
                    {
                        Debug.LogError($"❌ Le Prefab3D manque pour : {itemData?.itemName ?? "ID inconnu"}");
                        continue;
                    }

                    Transform pt = wb.spawnPoints[i];
                    GameObject obj = Instantiate(itemData.Prefab3D, pt.position, pt.rotation);
                    
                    if (obj.TryGetComponent<Rigidbody>(out Rigidbody rb))
                    {
                        rb.isKinematic = false;
                        rb.useGravity = true;
                    }
                    
                    spawnedCraftObjects.Add(obj);
                }
            }
        }
    }
}