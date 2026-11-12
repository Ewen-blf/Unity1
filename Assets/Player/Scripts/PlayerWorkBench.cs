using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;
using Unity.Cinemachine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Gère l'interaction avec l'établi (Workbench), les transitions de caméras Cinemachine 3,
/// le masquage de l'arme en main et l'instanciation des modèles 3D physiques pour le craft.
/// Réutilise automatiquement les références configurées dans PlayerInventory.
/// </summary>
public class PlayerWorkbench : NetworkBehaviour
{
    [Header("Interface & Données Établi")]
    public WorkbenchUIController workbenchUI;
    public int[] workbenchItems = new int[3];

    [Header("Optionnel (Auto-récupéré depuis PlayerInventory si vide)")]
    public Transform playerCamera;
    public float interactRange = 3f;
    public ItemDatabase database;
    public GameObject weaponDummy;

    private PlayerMovement playerMovement;
    private PlayerLook playerLook;
    private PlayerInventory playerInventory;
    private CinemachineBrain brain;

    private bool isAtWorkbench = false;
    public bool IsAtWorkbench => isAtWorkbench;

    private CinemachineCamera activeWorkbenchCam;
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
        playerMovement = GetComponent<PlayerMovement>();
        playerLook = GetComponent<PlayerLook>();
        playerInventory = GetComponent<PlayerInventory>();

        if (playerInventory != null)
        {
            if (playerCamera == null) playerCamera = playerInventory.playerCamera;
            if (database == null) database = playerInventory.itemDatabase;
            interactRange = playerInventory.interactRange;
        }

        if (playerCamera != null)
        {
            brain = playerCamera.GetComponent<CinemachineBrain>();
        }
    }

    public void SetWorkbenchItemServer(int index, int itemId)
    {
        if (index < 0 || index >= workbenchItems.Length) return;
        workbenchItems[index] = itemId;
        SyncWorkbenchItemClientRpc(index, itemId);
    }

    [Rpc(SendTo.Owner)]
    private void SyncWorkbenchItemClientRpc(int index, int itemId)
    {
        if (index >= 0 && index < workbenchItems.Length)
        {
            workbenchItems[index] = itemId;
        }
    }

    void OnInteract(InputValue value)
    {
        if (!IsOwner || !value.isPressed || isAtWorkbench || playerCamera == null) return;

        Ray ray = new Ray(playerCamera.position, playerCamera.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, interactRange))
        {
            Workbench workbench = hit.collider.GetComponentInParent<Workbench>();
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

        if (playerMovement != null) playerMovement.enabled = false;
        if (playerLook != null) playerLook.enabled = false;

        SetWeaponVisible(false);

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

        ClearSpawnedCraftObjects();

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

    private void ClearSpawnedCraftObjects()
    {
        for (int i = 0; i < spawnedCraftObjects.Count; i++)
        {
            if (spawnedCraftObjects[i] != null)
            {
                Destroy(spawnedCraftObjects[i]);
            }
        }
        spawnedCraftObjects.Clear();
    }

    private void SetWeaponVisible(bool visible)
    {
        if (weaponDummy != null)
        {
            weaponDummy.SetActive(visible);
        }
        else if (playerInventory != null)
        {
            playerInventory.SetHandVisible(visible);
        }
    }

    IEnumerator WaitAndEnableControl()
    {
        if (brain != null)
        {
            yield return new WaitUntil(() => !brain.IsBlending);
        }

        if (!isAtWorkbench)
        {
            if (playerMovement != null) playerMovement.enabled = true;
            if (playerLook != null) playerLook.enabled = true;
            SetWeaponVisible(true);
        }
    }

    public void StartPhysicalCrafting()
    {
        if (workbenchUI != null) workbenchUI.ToggleUI(false);

        if (activeWorkbenchCam == null) return;

        Workbench wb = activeWorkbenchCam.GetComponentInParent<Workbench>();
        if (wb == null) return;

        if (wb.WorkbenchTopCamera != null)
        {
            wb.WorkbenchTopCamera.Priority = 110;
        }

        if (database == null) return;

        ClearSpawnedCraftObjects();

        for (int i = 0; i < workbenchItems.Length; i++)
        {
            int itemId = workbenchItems[i];

            if (itemId > 0)
            {
                ItemData itemData = database.GetItemById(itemId);

                if (itemData == null || itemData.Prefab3D == null)
                {
                    Debug.LogError($"Le Prefab3D manque pour : {itemData?.itemName ?? "ID inconnu"}");
                    continue;
                }

                if (wb.spawnPoints == null || i >= wb.spawnPoints.Length || wb.spawnPoints[i] == null)
                {
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