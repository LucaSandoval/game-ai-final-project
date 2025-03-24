using UnityEngine;

/// <summary>
///  Any class that is a singleton should inherit from the base singleton class to ensure only
///  one exists in the scene at any given time. 
/// </summary>
public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    public static T Instance { get; private set; }

    public static bool IsInitialized { get; private set; } = false;

    protected virtual void Awake()
    {
        Instance = this as T;
        IsInitialized = true;
    }


    protected virtual void OnApplicationQuit()
    {
        Instance = null;
        Destroy(gameObject);
    }
}

/// <summary>
/// An extension of the Singleton base class that ensures one instance of the singleton exists and 
/// persists between scenes.
/// </summary>
public abstract class SingletonPersistent<T> : Singleton<T> where T : MonoBehaviour
{
    protected override void Awake()
    {
        if (Instance != null) Destroy(gameObject);
        DontDestroyOnLoad(gameObject);
        base.Awake();
    }
}
