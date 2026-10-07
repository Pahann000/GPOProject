using UnityEngine;

/// <summary>
/// Временная заглушка игрока, пока PlayerSystem не готов.
/// УДАЛИТЬ еогда настроится игрок
/// </summary>
public class PlayerStub : MonoBehaviour
{
    public static PlayerStub Instance { get; private set; }

    [SerializeField] private Player dummyPlayer;

    public Player LocalPlayer => dummyPlayer;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}