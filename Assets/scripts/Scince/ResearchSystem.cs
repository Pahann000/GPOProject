using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Серверная система древа исследований: кто что изучает, сколько прошло
/// и что уже открыто.
/// </summary>
/// <remarks>
/// Полностью серверная (как <see cref="ResourceSystem"/>): клиенты получают только
/// снимок состояния через <see cref="ResearchSync"/>. Прогресс идёт по времени,
/// скорость зависит от изученных технологий и от числа действующих
/// <see cref="ResearchLab"/> владельца.
/// </remarks>
public class ResearchSystem : IGameSystem
{
    public string SystemName => "Research System";
    public bool IsActive { get; set; } = true;

    /// <summary>Состояние исследований одного игрока.</summary>
    private class PlayerResearch
    {
        public readonly HashSet<int> Completed = new HashSet<int>();
        public int Current = ResearchRules.NoResearch;
        public float Progress;
    }

    private GameKernel _kernel;
    private ResearchAtlas _atlas;

    private readonly Dictionary<Player, PlayerResearch> _states = new Dictionary<Player, PlayerResearch>();
    private readonly Dictionary<Player, HashSet<ResearchLab>> _labs = new Dictionary<Player, HashSet<ResearchLab>>();
    private readonly List<int> _snapshotCompleted = new List<int>(32);

    /// <summary>Растёт при любом значимом изменении: по нему синхронизация решает, что отправить.</summary>
    public int Version { get; private set; }

    /// <summary>Атлас исследований из Resources (может быть null, тогда всё считается открытым).</summary>
    public ResearchAtlas Atlas => _atlas != null ? _atlas : (_atlas = ResearchAtlas.Cached);

    public void Initialize(GameKernel kernel)
    {
        _kernel = kernel;

        if (Atlas == null)
        {
            Debug.LogWarning($"[{SystemName}] Не найден ResearchAtlas в Resources: все здания и юниты считаются открытыми.");
        }

        Debug.Log($"[{SystemName}] Инициализирована.");
    }

    public void FixedTick(float fixedDeltaTime) { }

    public void Shutdown()
    {
        _states.Clear();
        _labs.Clear();
    }

    public void Tick(float deltaTime)
    {
        if (_states.Count == 0) return;

        foreach (KeyValuePair<Player, PlayerResearch> pair in _states)
        {
            Player player = pair.Key;
            PlayerResearch state = pair.Value;

            if (state.Current == ResearchRules.NoResearch) continue;

            ResearchProject project = Atlas != null ? Atlas.Get(state.Current) : null;
            if (project == null)
            {
                state.Current = ResearchRules.NoResearch;
                state.Progress = 0f;
                Version++;
                continue;
            }

            float speed = GetSpeed(player);
            state.Progress = ResearchRules.AdvanceProgress(state.Progress, project.Duration, deltaTime, speed);

            if (state.Progress >= project.Duration)
            {
                CompleteResearch(player, state, project);
            }
        }
    }

    // ---------------------------------------------------------------- игроки

    public void RegisterPlayer(Player player)
    {
        if (player == null) return;

        if (!_states.ContainsKey(player))
        {
            _states.Add(player, new PlayerResearch());
            Version++;
        }
    }

    public void UnregisterPlayer(Player player)
    {
        if (player == null) return;

        if (_states.Remove(player)) Version++;
        _labs.Remove(player);
    }

    /// <summary>Зарегистрирован ли игрок в системе исследований.</summary>
    public bool IsRegistered(Player player)
    {
        return player != null && _states.ContainsKey(player);
    }

    /// <summary>Регистрирует лабораторию, ускоряющую исследования владельца.</summary>
    public void RegisterLab(Player owner, ResearchLab lab)
    {
        if (owner == null || lab == null) return;

        if (!_labs.TryGetValue(owner, out HashSet<ResearchLab> labs))
        {
            labs = new HashSet<ResearchLab>();
            _labs[owner] = labs;
        }

        if (labs.Add(lab)) Version++;
    }

    public void UnregisterLab(Player owner, ResearchLab lab)
    {
        if (owner == null || lab == null) return;

        if (_labs.TryGetValue(owner, out HashSet<ResearchLab> labs) && labs.Remove(lab)) Version++;
    }

    /// <summary>Сколько действующих лабораторий у игрока.</summary>
    public int GetWorkingLabCount(Player player)
    {
        if (player == null || !_labs.TryGetValue(player, out HashSet<ResearchLab> labs)) return 0;

        int count = 0;

        foreach (ResearchLab lab in labs)
        {
            if (lab == null) continue;
            if (lab.State != BuildingState.Operational) continue;
            if (lab.Owner != player) continue;

            count++;
        }

        return count;
    }

    // ---------------------------------------------------------------- прогресс

    /// <summary>Множитель скорости исследований игрока.</summary>
    public float GetSpeed(Player player)
    {
        float researchBonus = GetBonusPercent(player, p => p.researchSpeedBonusPercent);
        float labPercent = GetLabSpeedPercent(player);

        return ResearchRules.ComputeSpeed(researchBonus, GetWorkingLabCount(player), labPercent);
    }

    /// <summary>Бонус скорости от одной лаборатории (из префаба любой лаборатории игрока).</summary>
    private float GetLabSpeedPercent(Player player)
    {
        if (player == null || !_labs.TryGetValue(player, out HashSet<ResearchLab> labs)) return 0f;

        foreach (ResearchLab lab in labs)
        {
            if (lab != null) return lab.ResearchSpeedPercent;
        }

        return 0f;
    }

    /// <summary>Текущее исследование игрока (индекс в атласе) или <see cref="ResearchRules.NoResearch"/>.</summary>
    public int GetCurrentIndex(Player player)
    {
        return TryGetState(player, out PlayerResearch state) ? state.Current : ResearchRules.NoResearch;
    }

    /// <summary>Прогресс текущего исследования в секундах.</summary>
    public float GetProgress(Player player)
    {
        return TryGetState(player, out PlayerResearch state) ? state.Progress : 0f;
    }

    /// <summary>Длительность текущего исследования в секундах.</summary>
    public float GetCurrentDuration(Player player)
    {
        int index = GetCurrentIndex(player);
        ResearchProject project = Atlas != null ? Atlas.Get(index) : null;

        return project != null ? project.Duration : 0f;
    }

    /// <summary>Изучено ли исследование игроком.</summary>
    public bool IsCompleted(Player player, ResearchProject project)
    {
        if (project == null || Atlas == null) return false;

        return IsCompleted(player, Atlas.IndexOf(project));
    }

    /// <summary>Изучено ли исследование по индексу.</summary>
    public bool IsCompleted(Player player, int index)
    {
        return TryGetState(player, out PlayerResearch state) && state.Completed.Contains(index);
    }

    /// <summary>Копия списка изученных индексов (для синхронизации).</summary>
    public List<int> GetCompletedIndices(Player player)
    {
        _snapshotCompleted.Clear();

        if (TryGetState(player, out PlayerResearch state))
        {
            foreach (int index in state.Completed) _snapshotCompleted.Add(index);
        }

        return _snapshotCompleted;
    }

    /// <summary>
    /// Пытается начать исследование: проверяет требования и ресурсы,
    /// списывает стоимость и запускает прогресс.
    /// </summary>
    public bool TryStartResearch(Player player, int index)
    {
        if (!TryGetState(player, out PlayerResearch state))
        {
            Debug.LogWarning($"[{SystemName}] Игрок не зарегистрирован в системе исследований.");
            return false;
        }

        ResearchAtlas atlas = Atlas;
        ResearchProject project = atlas != null ? atlas.Get(index) : null;

        if (state.Current != ResearchRules.NoResearch)
        {
            Debug.Log($"[{SystemName}] Нельзя начать новое исследование: уже идёт {GetProjectName(state.Current)}.");
            return false;
        }

        ResourceSystem resources = null;
        _kernel?.TryGetSystem(out resources);

        ResourcePair[] cost = ResearchRules.CostOf(project);
        bool hasResources = resources != null ? resources.HasResources(player, cost) : cost == null;

        if (!ResearchRules.CanStart(project, index, state.Completed, atlas, hasResources, out string reason))
        {
            Debug.Log($"[{SystemName}] Исследование «{GetProjectName(index)}» не начато: {reason}");
            return false;
        }

        if (resources != null && cost != null && !resources.TrySpendResources(player, cost))
        {
            Debug.Log($"[{SystemName}] Исследование «{GetProjectName(index)}» не начато: не удалось списать ресурсы.");
            return false;
        }

        state.Current = index;
        state.Progress = 0f;
        Version++;

        Debug.Log($"[{SystemName}] Начато исследование «{GetProjectName(index)}» ({project.Duration:F0} с).");
        return true;
    }

    private void CompleteResearch(Player player, PlayerResearch state, ResearchProject project)
    {
        state.Completed.Add(state.Current);
        state.Current = ResearchRules.NoResearch;
        state.Progress = 0f;
        Version++;

        if (_kernel != null)
        {
            _kernel.EventBus.Raise(new ResearchCompletedEvent(player, project));
        }

        Debug.Log($"[{SystemName}] Исследование «{project.DisplayName}» завершено.");
    }

    // ---------------------------------------------------------------- доступ и бонусы

    /// <summary>Доступно ли здание игроку (с учётом изученных исследований).</summary>
    public bool IsBuildingUnlocked(Player player, BuildingTypeNames type)
    {
        if (Atlas == null) return true;

        return TryGetState(player, out PlayerResearch state)
            ? ResearchRules.IsBuildingUnlocked(Atlas, state.Completed, type)
            : ResearchRules.IsBuildingUnlocked(Atlas, null, type);
    }

    /// <summary>Доступен ли тип юнита игроку.</summary>
    public bool IsUnitUnlocked(Player player, UnitTypeName type)
    {
        if (Atlas == null) return true;

        return TryGetState(player, out PlayerResearch state)
            ? ResearchRules.IsUnitUnlocked(Atlas, state.Completed, type)
            : ResearchRules.IsUnitUnlocked(Atlas, null, type);
    }

    /// <summary>Множитель производства зданий игрока.</summary>
    public float GetProductionMultiplier(Player player)
    {
        return 1f + GetBonusPercent(player, p => p.productionBonusPercent) / 100f;
    }

    /// <summary>Множитель урона юнитов игрока.</summary>
    public float GetUnitDamageMultiplier(Player player)
    {
        return 1f + GetBonusPercent(player, p => p.unitDamageBonusPercent) / 100f;
    }

    /// <summary>Множитель здоровья новых юнитов игрока.</summary>
    public float GetUnitHealthMultiplier(Player player)
    {
        return 1f + GetBonusPercent(player, p => p.unitHealthBonusPercent) / 100f;
    }

    private float GetBonusPercent(Player player, System.Func<ResearchProject, float> select)
    {
        if (player == null || Atlas == null) return 0f;
        if (!TryGetState(player, out PlayerResearch state)) return 0f;

        return ResearchRules.SumBonusPercent(Atlas, state.Completed, select);
    }

    // ---------------------------------------------------------------- служебное

    private bool TryGetState(Player player, out PlayerResearch state)
    {
        state = null;

        if (player == null) return false;

        return _states.TryGetValue(player, out state) && state != null;
    }

    private string GetProjectName(int index)
    {
        ResearchProject project = Atlas != null ? Atlas.Get(index) : null;

        return project != null ? project.DisplayName : $"#{index}";
    }
}
