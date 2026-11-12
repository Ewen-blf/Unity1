using UnityEngine;
using Unity.Cinemachine;

/// <summary>
/// Représente l'établi 3D dans la scène et stocke les références vers ses caméras 
/// Cinemachine (vue menu et vue de dessus) ainsi que les points d'apparition des objets 3D.
/// </summary>
public class Workbench : MonoBehaviour
{
    public CinemachineCamera workbenchCam;
    public CinemachineCamera WorkbenchTopCamera;
    public Transform[] spawnPoints = new Transform[3];
}