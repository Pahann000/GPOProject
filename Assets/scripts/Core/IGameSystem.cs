using UnityEngine;

public interface IGameSystem
{
    string SystemName { get; }

    bool IsActive { get; set; }

    void Initialize(GameKernel kernel);

    void Tick(float deltaTime);

    void FixedTick(float fixedDeltaTime);

    void Shutdown();
}