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
    public int[] workbenchItems = new int[3];

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
                workbenchUI.ToggleUI(true);
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

    public void ExitWorkbench()
    {
        isAtWorkbench = false;

        // 1. On ferme l'interface
        if (workbenchUI != null)
        {
            workbenchUI.ToggleUI(false); 
        }

        // 2. On verrouille et on cache la souris
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // 3. Restitution de la caméra au Joueur (Cinemachine 3)
        if (activeWorkbenchCam != null)
        {
            activeWorkbenchCam.Priority = 0;
            activeWorkbenchCam = null; // Optionnel : on vide la référence
        }

        // 4. On lance la coroutine qui attend la fin de la caméra pour rendre le mouvement !
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