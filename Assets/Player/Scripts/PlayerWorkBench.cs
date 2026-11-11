using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;
using Unity.Cinemachine;
using System.Collections;

public class PlayerWorkbench : NetworkBehaviour
{
    [Header("Références")]
    public Transform playerCamera;
    public float interactRange = 3f;

    private MonoBehaviour playerMovement;
    private MonoBehaviour playerLook;
    private CinemachineBrain brain;

    private bool isAtWorkbench = false;
    private CinemachineCamera activeWorkbenchCam;
    public WorkbenchUIController workbenchUI;

    [Header("Crafting Data")]
    // Stocke l'ID des objets dans les 3 cases (0 = vide)
    private int[] workbenchItems = new int[3]; 
    // Mémorise de quelle case de la Hotbar l'objet provient
    private int[] originHotbarSlots = new int[3]; 
    private PlayerInventory playerInventory;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner && workbenchUI != null)
        {
            workbenchUI.gameObject.SetActive(false);
        }
    }

    void Start()
    {
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

        // Bloque les mouvements du joueur
        if (playerMovement != null) playerMovement.enabled = false;
        if (playerLook != null) playerLook.enabled = false;

        // Affiche la souris
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        activeWorkbenchCam.Priority = 100;
        if (workbenchUI == null) 
            {
                workbenchUI = FindFirstObjectByType<WorkbenchUIController>();
            }

            if (workbenchUI != null) 
            {
                workbenchUI.ShowUI();
            }
            else
            {
                Debug.LogError("Le Player n'arrive pas à trouver le WorkbenchUIController dans la scène !");
            }
    }

    void OnExit(InputValue value)
    {
        if (!IsOwner || !value.isPressed || !isAtWorkbench) return;
        ExitWorkbench();
    }

    void ExitWorkbench()
    {
        if (workbenchUI != null) workbenchUI.HideUI();
        isAtWorkbench = false;
        for (int i = 0; i < 3; i++)
        {
            if (workbenchItems[i] != 0)
            {
                // On demande au serveur de remettre l'objet dans l'inventaire
                if (playerInventory != null)
                {
                    playerInventory.ReturnItemToInventoryRpc(workbenchItems[i], originHotbarSlots[i]);
                }
                
                // On vide la case de l'établi
                workbenchItems[i] = 0;
            }
        }

        // On remet la priorité à 0. Le Brain ramène la vue dans la tête du joueur !
        if (activeWorkbenchCam != null)
        {
            activeWorkbenchCam.Priority = 0;
            activeWorkbenchCam = null;
        }

        // Cache la souris
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // On attend la fin du "Lerp" cinématique avant de redonner les contrôles
        StartCoroutine(WaitAndEnableControl());
    }

    IEnumerator WaitAndEnableControl()
    {
        // Attend que Cinemachine ait totalement terminé son voyage
        yield return new WaitUntil(() => !brain.IsBlending);

        if (!isAtWorkbench) // Sécurité si on a cliqué sur E pendant le retour
        {
            if (playerMovement != null) playerMovement.enabled = true;
            if (playerLook != null) playerLook.enabled = true;
        }
    }
}