using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    // Global settings
    public float conveyorSpeed = 2.0f;
    public float itemSpawnInterval = 1.5f;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); // Enforce singleton
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Optional, if you want it across scenes
        }
    }

    

    // Add methods for spawning items, updating global settings, etc.
}