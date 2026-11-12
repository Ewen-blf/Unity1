using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Gère l'inventaire réseau du joueur (Hotbar), le ramassage des objets au sol, 
/// l'affichage du modèle 3D en main (HoldPoint) et la synchronisation des échanges avec l'établi.
/// </summary>
public class PlayerInventory : NetworkBehaviour
{
    private const int HOTBAR_SIZE = 9;

    [Header("Références")]
    public Transform playerCamera;
    public Transform holdPoint;
    public float interactRange = 3f;
    public ItemDatabase itemDatabase;
    public HotbarUIController uiController;

    private NetworkList<int> inventorySlots;
    private NetworkVariable<int> activeSlot = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private int localSelectedSlot = 0;
    private GameObject currentVisualModel;
    private PlayerWorkbench playerWorkbench;

    void Awake()
    {
        inventorySlots = new NetworkList<int>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        playerWorkbench = GetComponent<PlayerWorkbench>();
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
        else if (uiController != null)
        {
            uiController.gameObject.SetActive(false);
        }

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
        if (playerWorkbench != null && playerWorkbench.IsAtWorkbench) return;

        Ray ray = new Ray(playerCamera.position, playerCamera.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, interactRange))
        {
            WorldItem item = hit.collider.GetComponentInParent<WorldItem>();
            if (item != null)
            {
                if (item.NetworkObject != null && item.NetworkObject.IsSpawned)
                {
                    RequestPickupRpc(item.NetworkObject.NetworkObjectId, localSelectedSlot);
                }
                else
                {
                    Debug.LogWarning($"Impossible de ramasser {item.name} : NetworkObject manquant ou non Spawné sur le réseau !");
                }
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
        RefreshHandVisual(activeSlot.Value);
    }

    private void OnActiveSlotChanged(int previousValue, int newValue)
    {
        RefreshHandVisual(newValue);
    }

    private void RefreshHandVisual(int slotIndex)
    {
        if (currentVisualModel != null) Destroy(currentVisualModel);
        
        if (slotIndex < 0 || slotIndex >= inventorySlots.Count) return;

        int itemId = inventorySlots[slotIndex];
        if (itemId == 0 || itemDatabase == null) return; 

        ItemData data = itemDatabase.GetItemById(itemId);
        if (data != null && data.handPrefab != null && holdPoint != null)
        {
            currentVisualModel = Instantiate(data.handPrefab, holdPoint);
            currentVisualModel.transform.localPosition = Vector3.zero;
            currentVisualModel.transform.localRotation = Quaternion.identity;
        }
    }

    public void SetHandVisible(bool visible)
    {
        if (holdPoint != null)
        {
            holdPoint.gameObject.SetActive(visible);
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

        if (originalSlot >= 0 && originalSlot < HOTBAR_SIZE && inventorySlots[originalSlot] == 0)
        {
            inventorySlots[originalSlot] = itemId;
            return;
        }

        int fallbackSlot = FindAvailableSlot(0);
        if (fallbackSlot != -1)
        {
            inventorySlots[fallbackSlot] = itemId;
        }
        else
        {
            Debug.LogWarning("Inventaire plein, l'objet devrait tomber au sol !");
        }
    }

    [Rpc(SendTo.Server)]
    public void SwapItemsRpc(string slotA, string slotB)
    {
        if (playerWorkbench == null) playerWorkbench = GetComponent<PlayerWorkbench>();
        if (playerWorkbench == null) return;

        string[] partsA = slotA.Split('-');
        string typeA = partsA[0];
        int idA = int.Parse(partsA[1]);

        string[] partsB = slotB.Split('-');
        string typeB = partsB[0];
        int idB = int.Parse(partsB[1]);

        int itemA = (typeA == "hotbar") ? inventorySlots[idA] : playerWorkbench.workbenchItems[idA];
        int itemB = (typeB == "hotbar") ? inventorySlots[idB] : playerWorkbench.workbenchItems[idB];

        if (typeA == "hotbar") inventorySlots[idA] = itemB;
        else playerWorkbench.SetWorkbenchItemServer(idA, itemB);

        if (typeB == "hotbar") inventorySlots[idB] = itemA;
        else playerWorkbench.SetWorkbenchItemServer(idB, itemA);
    }
}