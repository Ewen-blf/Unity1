using Unity.Netcode;
using UnityEngine;
using Unity.Cinemachine;

// 1. Il doit hériter de NetworkBehaviour pour parler sur le réseau
public class Workbench : NetworkBehaviour
{
    public CinemachineCamera workbenchCam;
    public CinemachineCamera WorkbenchTopCamera;
    public Transform[] spawnPoints = new Transform[3];
    
    // 2. La mémoire partagée ! (Une liste réseau au lieu d'un tableau local)
    public NetworkList<int> workbenchItems;

    private void Awake()
    {
        // Initialisation de la liste
        workbenchItems = new NetworkList<int>();
    }

    public override void OnNetworkSpawn()
    {
        // Le serveur crée les 3 cases vides (0) au démarrage
        if (IsServer && workbenchItems.Count == 0)
        {
            workbenchItems.Add(0);
            workbenchItems.Add(0);
            workbenchItems.Add(0);
        }
    }
}