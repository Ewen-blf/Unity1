using UnityEngine;
using Unity.Netcode; // Indispensable pour parler au NetworkManager

public class NetworkUI : MonoBehaviour
{
    public void StartAsHost()
    {
        // Démarre en tant qu'Hôte (Serveur + Joueur local)
        NetworkManager.Singleton.StartHost();
        
        // Masque l'interface une fois connecté
        gameObject.SetActive(false); 
    }

    public void StartAsClient()
    {
        // Démarre en tant que Client (Joueur local qui rejoint)
        NetworkManager.Singleton.StartClient();
        
        gameObject.SetActive(false);
    }
}