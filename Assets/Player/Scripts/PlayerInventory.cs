using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInventory : NetworkBehaviour
{
    private const int HOTBAR_SIZE = 9;

    [Header("Références")]
    public Transform playerCamera;
    public Transform holdPoint; // NOUVEAU : Le point d'attache (la main)
    public float interactRange = 3f;
    public ItemDatabase itemDatabase;

    private NetworkList<int> inventorySlots;
    
    // NOUVEAU : Synchronise le choix de la molette sur le réseau
    private NetworkVariable<int> activeSlot = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private int localSelectedSlot = 0;
    public HotbarUIController uiController;
    private GameObject currentVisualModel; // L'arme physique générée

    void Awake()
    {
        inventorySlots = new NetworkList<int>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            for (int i = 0; i < HOTBAR_SIZE; i++) inventorySlots.Add(0);
        }

        if (IsOwner)
        {
            RefreshFullUI();
        }
        else
        {
            if (uiController != null)
            {
                uiController.gameObject.SetActive(false);
            }
        }

        // Tout le monde (Hôte et Client) écoute ces changements pour afficher les armes
        inventorySlots.OnListChanged += OnInventoryChanged;
        activeSlot.OnValueChanged += OnActiveSlotChanged;
        
        RefreshHandVisual(activeSlot.Value);
    }

    public override void OnNetworkDespawn()
    {
        inventorySlots.OnListChanged -= OnInventoryChanged;
        activeSlot.OnValueChanged -= OnActiveSlotChanged;
    }

    void OnScroll(InputValue value)
    {
        if (!IsOwner) return;

        Vector2 scroll = value.Get<Vector2>();
        if (scroll.y > 0.1f)
        {
            localSelectedSlot = (localSelectedSlot - 1 + HOTBAR_SIZE) % HOTBAR_SIZE;
            UpdateSelectionLocalAndServer();
        }
        else if (scroll.y < -0.1f)
        {
            localSelectedSlot = (localSelectedSlot + 1) % HOTBAR_SIZE;
            UpdateSelectionLocalAndServer();
        }
    }

    void UpdateSelectionLocalAndServer()
    {
        uiController?.UpdateSelection(localSelectedSlot);
        ChangeActiveSlotRpc(localSelectedSlot);
    }

    [Rpc(SendTo.Server)]
    private void ChangeActiveSlotRpc(int newSlot)
    {
        activeSlot.Value = newSlot;
    }

    void OnInteract(InputValue value)
    {
        if (!IsOwner || !value.isPressed) return;

        Ray ray = new Ray(playerCamera.position, playerCamera.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, interactRange))
        {
            WorldItem item = hit.collider.GetComponentInParent<WorldItem>();
            if (item != null && item.NetworkObject != null && item.NetworkObject.IsSpawned)
            {
                RequestPickupRpc(item.NetworkObject.NetworkObjectId, localSelectedSlot);
            }
        }
    }

    [Rpc(SendTo.Server)]
    private void RequestPickupRpc(ulong networkObjectId, int preferredSlot)
    {
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(networkObjectId, out NetworkObject netObj)) return;
        WorldItem worldItem = netObj.GetComponent<WorldItem>();
        if (worldItem == null || worldItem.itemData == null) return;

        int targetSlot = FindAvailableSlot(preferredSlot);
        if (targetSlot == -1) return;

        inventorySlots[targetSlot] = worldItem.itemData.itemId;
        netObj.Despawn(true);
    }

    private int FindAvailableSlot(int startSlot)
    {
        startSlot = Mathf.Clamp(startSlot, 0, HOTBAR_SIZE - 1);
        if (inventorySlots[startSlot] == 0) return startSlot;

        for (int offset = 1; offset < HOTBAR_SIZE; offset++)
        {
            int right = startSlot + offset;
            int left = startSlot - offset;
            if (right < HOTBAR_SIZE && inventorySlots[right] == 0) return right;
            if (left >= 0 && inventorySlots[left] == 0) return left;
        }
        return -1;
    }

    private void OnInventoryChanged(NetworkListEvent<int> changeEvent)
    {
        if (IsOwner) RefreshFullUI();
        RefreshHandVisual(activeSlot.Value); // Met à jour l'arme si on la ramasse directement dans la case active
    }

    private void OnActiveSlotChanged(int previousValue, int newValue)
    {
        RefreshHandVisual(newValue); // Met à jour l'arme quand on tourne la molette
    }

    private void RefreshHandVisual(int slotIndex)
    {
        // 1. Supprime l'ancienne arme de la main
        if (currentVisualModel != null) Destroy(currentVisualModel);
        
        if (slotIndex < 0 || slotIndex >= inventorySlots.Count) return;

        // 2. Vérifie la nouvelle arme
        int itemId = inventorySlots[slotIndex];
        if (itemId == 0) return; 

        ItemData data = itemDatabase.GetItemById(itemId);
        if (data != null && data.handPrefab != null)
        {
            // 3. Crée la nouvelle arme dans le HoldPoint
            currentVisualModel = Instantiate(data.handPrefab, holdPoint);
            currentVisualModel.transform.localPosition = Vector3.zero;
            currentVisualModel.transform.localRotation = Quaternion.identity;
        }
    }

    private void RefreshFullUI()
    {
        if (uiController == null || itemDatabase == null) return;
        for (int i = 0; i < inventorySlots.Count; i++)
        {
            int itemId = inventorySlots[i];
            if (itemId == 0) uiController.UpdateSlotIcon(i, null);
            else
            {
                ItemData data = itemDatabase.GetItemById(itemId);
                uiController.UpdateSlotIcon(i, data != null ? data.icon : null);
            }
        }
    }

    [Rpc(SendTo.Server)]
    public void ReturnItemToInventoryRpc(int itemId, int originalSlot)
    {
        if (itemId == 0) return;

        // 1. On essaie de le remettre exactement là où il était
        if (originalSlot >= 0 && originalSlot < HOTBAR_SIZE && inventorySlots[originalSlot] == 0)
        {
            inventorySlots[originalSlot] = itemId;
            return;
        }

        // 2. Sécurité : si la case est prise, on cherche la première case vide
        int fallbackSlot = FindAvailableSlot(0);
        if (fallbackSlot != -1)
        {
            inventorySlots[fallbackSlot] = itemId;
        }
        else
        {
            // Plus tard : Si l'inventaire est totalement plein, on jettera l'objet au sol (Drop)
            Debug.LogWarning("Inventaire plein, l'objet devrait tomber au sol !");
        }
    }

    [Rpc(SendTo.Server)]
    public void SwapItemsRpc(string slotA, string slotB)
    {
        PlayerWorkbench workbench = GetComponent<PlayerWorkbench>();
        if (workbench == null) return;

        // 1. Découpage des noms (ex: "hotbar-2" -> type "hotbar", id 2)
        string typeA = slotA.Split('-')[0];
        int idA = int.Parse(slotA.Split('-')[1]);

        string typeB = slotB.Split('-')[0];
        int idB = int.Parse(slotB.Split('-')[1]);

        // 2. Lecture des objets en mémoire
        int itemA = (typeA == "hotbar") ? inventorySlots[idA] : workbench.workbenchItems[idA];
        int itemB = (typeB == "hotbar") ? inventorySlots[idB] : workbench.workbenchItems[idB];

        // 3. Échange dans les tableaux !
        if (typeA == "hotbar") inventorySlots[idA] = itemB;
        else workbench.workbenchItems[idA] = itemB;

        if (typeB == "hotbar") inventorySlots[idB] = itemA;
        else workbench.workbenchItems[idB] = itemA;
        
        // (Le NetworkList de la Hotbar mettra à jour l'UI automatiquement pour tous les clients)
    }
}