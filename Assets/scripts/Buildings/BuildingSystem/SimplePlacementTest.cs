using UnityEngine;

public class SimplePlacementTest : MonoBehaviour
{
    [SerializeField] private LayerMask buildableLayer;
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private Vector2 testSize = new Vector2(2, 2);

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            TestPlacement(mousePos);
        }
    }

    void TestPlacement(Vector2 position)
    {
        Debug.Log("=== ТЕСТ РАЗМЕЩЕНИЯ ===");
        Debug.Log($"Позиция: {position}");
        Debug.Log($"Размер: {testSize}");

        // 1. Проверяем препятствия
        Collider2D[] allColliders = Physics2D.OverlapBoxAll(position, testSize, 0);
        Debug.Log($"Все коллайдеры в зоне: {allColliders.Length}");
        foreach (Collider2D col in allColliders)
        {
            Debug.Log($"- {col.name} (слой: {LayerMask.LayerToName(col.gameObject.layer)})");
        }

        // 2. Проверка препятствий
        Collider2D obstacle = Physics2D.OverlapBox(position, testSize, 0, obstacleLayer);
        Debug.Log($"Препятствие найдено: {obstacle != null}");

        // 3. Проверка строительной поверхности
        Collider2D ground = Physics2D.OverlapBox(position, testSize, 0, buildableLayer);
        Debug.Log($"Строительная поверхность найдена: {ground != null}");

        // 4. Итог
        bool canBuild = (obstacle == null && ground != null);
        Debug.Log($"Можно строить: {canBuild}");

        // Визуализация
        Debug.DrawLine(position - testSize / 2, position + testSize / 2,
            canBuild ? Color.green : Color.red, 2f);
    }

    void OnDrawGizmos()
    {
        if (Application.isPlaying && Input.GetMouseButton(0))
        {
            Gizmos.color = Color.yellow;
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Gizmos.DrawWireCube(mousePos, testSize);
        }
    }
}