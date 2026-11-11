using UnityEngine;

[CreateAssetMenu(fileName = "NewItem", menuName = "Inventory/Item Data")]
public class ItemData : ScriptableObject
{
    public int itemId; // Doit être supérieur à 0 (0 = case vide)
    public string itemName;
    public Sprite icon;
    public GameObject handPrefab;
}