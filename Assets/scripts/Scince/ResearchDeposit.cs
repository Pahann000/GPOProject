using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Усовершенствованный класс месторождения ресурсов с визуальной обратной связью и событиями
/// </summary>
public class ResourceDeposit : MonoBehaviour
{
    [System.Serializable]
    public class DepositEvent : UnityEvent<ResourceDeposit> { }

    [Header("Основные настройки")]
    [SerializeField] private ResourceType _resourceType;
    [SerializeField] private int _totalAmount = 1000;
    [SerializeField] private int _minimalAmount = 50;

    [Header("Визуальные элементы")]
    [SerializeField] private ParticleSystem _miningParticles;
    [SerializeField] private GameObject _depletedVisual;
    [SerializeField] private GameObject _activeVisual;

    [Header("События")]
    public DepositEvent OnDepleted = new DepositEvent();
    public DepositEvent OnAmountChanged = new DepositEvent();

    private int _remainingAmount;
    private bool _wasDepleted;

    /// <summary> Тип ресурса в месторождении </summary>
    public ResourceType ResourceType => _resourceType;

    /// <summary> Общий начальный объем ресурсов </summary>
    public int TotalAmount => _totalAmount;

    /// <summary> Текущий остаток ресурсов </summary>
    public int RemainingAmount
    {
        get => _remainingAmount;
        private set
        {
            _remainingAmount = Mathf.Clamp(value, 0, _totalAmount);
            OnAmountChanged.Invoke(this);
            UpdateVisuals();
        }
    }

    /// <summary> Процент оставшихся ресурсов (0-1) </summary>
    public float RemainingPercent => (float)RemainingAmount / TotalAmount;

    /// <summary> Флаг полного истощения </summary>
    public bool IsDepleted => RemainingAmount <= _minimalAmount;

    private void Start()
    {
        ResetDeposit();
        UpdateVisuals();
    }

    /// <summary> Извлечение ресурсов с визуальной обратной связью </summary>
    public int ExtractResource(int requestedAmount)
    {
        if (IsDepleted) return 0;

        int actualAmount = Mathf.Min(requestedAmount, RemainingAmount - _minimalAmount);
        RemainingAmount -= actualAmount;

        PlayMiningEffects();
        CheckDepletionState();

        return actualAmount;
    }

    /// <summary> Сброс месторождения в исходное состояние </summary>
    public void ResetDeposit()
    {
        RemainingAmount = _totalAmount;
        _wasDepleted = false;
        UpdateVisuals();
    }

    private void PlayMiningEffects()
    {
        if (_miningParticles != null)
        {
            _miningParticles.Play();
        }
    }

    private void CheckDepletionState()
    {
        if (!_wasDepleted && IsDepleted)
        {
            _wasDepleted = true;
            OnDepleted.Invoke(this);
        }
    }

    private void UpdateVisuals()
    {
        if (_depletedVisual != null)
            _depletedVisual.SetActive(IsDepleted);

        if (_activeVisual != null)
            _activeVisual.SetActive(!IsDepleted);
    }

    /// <summary> Вспомогательный метод для редактора: предпросмотр состояния </summary>
    private void OnValidate()
    {
        _remainingAmount = Mathf.Clamp(_remainingAmount, 0, _totalAmount);
        UpdateVisuals();
    }
}