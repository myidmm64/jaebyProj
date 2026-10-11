using UnityEngine;

public class MonoSingleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T _inst;
    public static T Inst
    {
        get
        {
            if (_inst == null)
            {
                _inst = Object.FindAnyObjectByType<T>();
                if (_inst == null)
                {
                    GameObject singletonObject = new GameObject(typeof(T).Name);
                    _inst = singletonObject.AddComponent<T>();
                    Object.DontDestroyOnLoad(singletonObject);
                }
            }
            return _inst;
        }
    }
}
