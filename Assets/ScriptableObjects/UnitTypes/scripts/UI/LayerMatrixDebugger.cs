using UnityEngine;

public class LayerMatrixDebugger : MonoBehaviour
{
    [Header("Проверка слоев")]
    [SerializeField] private LayerMask buildableLayer = 1 << 8; // Слой 8
    [SerializeField] private LayerMask obstacleLayer = 1 << 9;  // Слой 9
    [SerializeField] private LayerMask buildingLayer = 1 << 10; // Слой 10

    [Header("Тест")]
    [SerializeField] private Vector2 testPosition = Vector2.zero;
    [SerializeField] private Vector2 testSize = new Vector2(2, 2);

    void Start()
    {
        Debug.Log("=== ПРОВЕРКА МАТРИЦЫ КОЛЛИЗИЙ ===");

        // 1. Проверяем, какие слои включены в LayerMask
        Debug.Log($"Buildable layer: {LayerMask.LayerToName(8)} = {buildableLayer.value}");
        Debug.Log($"Obstacle layer: {LayerMask.LayerToName(9)} = {obstacleLayer.value}");
        Debug.Log($"Building layer: {LayerMask.LayerToName(10)} = {buildingLayer.value}");

        // 2. Проверяем пересечения слоев
        TestLayerCollision();

        // 3. Выводим матрицу для справки
        PrintLayerMatrix();
    }

    void TestLayerCollision()
    {
        Debug.Log("=== ТЕСТ ПЕРЕСЕЧЕНИЙ СЛОЕВ ===");

        // Создаем тестовые объекты
        GameObject testBuildable = CreateTestObject("TestBuildable", 8);
        GameObject testObstacle = CreateTestObject("TestObstacle", 9);
        GameObject testBuilding = CreateTestObject("TestBuilding", 10);

        // Размещаем их в одной точке
        testBuildable.transform.position = testPosition;
        testObstacle.transform.position = testPosition;
        testBuilding.transform.position = testPosition;

        // Проверяем столкновения
        Collider2D[] colliders = Physics2D.OverlapBoxAll(testPosition, testSize, 0);
        Debug.Log($"Найдено коллайдеров в точке {testPosition}: {colliders.Length}");

        foreach (Collider2D col in colliders)
        {
            string layerName = LayerMask.LayerToName(col.gameObject.layer);
            Debug.Log($"- {col.name} (слой: {layerName}, индекс: {col.gameObject.layer})");
        }

        // Проверяем конкретные пересечения
        bool buildableVsObstacle = Physics2D.GetIgnoreLayerCollision(8, 9);
        bool buildableVsBuilding = Physics2D.GetIgnoreLayerCollision(8, 10);
        bool obstacleVsBuilding = Physics2D.GetIgnoreLayerCollision(9, 10);

        Debug.Log($"Buildable vs Obstacle игнорируются: {buildableVsObstacle}");
        Debug.Log($"Buildable vs Building игнорируются: {buildableVsBuilding}");
        Debug.Log($"Obstacle vs Building игнорируются: {obstacleVsBuilding}");

        // Удаляем тестовые объекты
        Destroy(testBuildable);
        Destroy(testObstacle);
        Destroy(testBuilding);
    }

    GameObject CreateTestObject(string name, int layer)
    {
        GameObject obj = new GameObject(name);
        obj.layer = layer;

        // Добавляем коллайдер
        BoxCollider2D collider = obj.AddComponent<BoxCollider2D>();
        collider.size = testSize;

        // Добавляем спрайт для визуализации
        SpriteRenderer renderer = obj.AddComponent<SpriteRenderer>();
        renderer.color = GetColorForLayer(layer);
        renderer.sprite = Sprite.Create(Texture2D.whiteTexture,
            new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));

        return obj;
    }

    Color GetColorForLayer(int layer)
    {
        return layer switch
        {
            8 => Color.green,    // Buildable
            9 => Color.red,      // Obstacle
            10 => Color.blue,    // Building
            _ => Color.gray
        };
    }

    void PrintLayerMatrix()
    {
        Debug.Log("=== МАТРИЦА КОЛЛИЗИЙ (текущая) ===");

        string[] layerNames = new string[]
        {
            "Default", "TransparentFX", "Ignore Raycast", "",
            "Water", "UI", "", "",
            "Buildable", "Obstacle", "Building", "Ground",
            "Resource", "", "", "",
            "", "", "", "",
            "", "", "", "",
            "", "", "", "",
            "", "", "", ""
        };

        for (int i = 0; i < 32; i++)
        {
            for (int j = 0; j < 32; j++)
            {
                if (!Physics2D.GetIgnoreLayerCollision(i, j))
                {
                    string layerI = string.IsNullOrEmpty(layerNames[i]) ? $"Layer {i}" : layerNames[i];
                    string layerJ = string.IsNullOrEmpty(layerNames[j]) ? $"Layer {j}" : layerNames[j];
                    if (!string.IsNullOrEmpty(layerI) && !string.IsNullOrEmpty(layerJ))
                        Debug.Log($"{layerI} ({i}) ↔ {layerJ} ({j})");
                }
            }
        }
    }

    void OnDrawGizmos()
    {
        // Рисуем зону теста
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(testPosition, testSize);

        // Рисуем информацию о слоях
        GUIStyle style = new GUIStyle();
        style.normal.textColor = Color.white;
        style.fontSize = 12;

        Vector3 labelPos = testPosition + Vector2.up * 3;

#if UNITY_EDITOR
        UnityEditor.Handles.Label(labelPos,
            $"Buildable (8) ↔ Obstacle (9): {Physics2D.GetIgnoreLayerCollision(8, 9)}", style);
        UnityEditor.Handles.Label(labelPos + Vector3.down * 0.5f,
            $"Buildable (8) ↔ Building (10): {Physics2D.GetIgnoreLayerCollision(8, 10)}", style);
        UnityEditor.Handles.Label(labelPos + Vector3.down * 1f,
            $"Obstacle (9) ↔ Building (10): {Physics2D.GetIgnoreLayerCollision(9, 10)}", style);
#endif
    }
}