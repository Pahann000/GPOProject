using UnityEngine;

public class TestConfig : ScriptableObject
{
    [Header("Settings")]
    public string description;
    public int max_value;
    public float speed;

    public void Initialize()
    {
        Debug.Log($"Initializing {class_name}");
    }
}
