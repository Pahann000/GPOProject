using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TalentUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI элементы")]
    public Image iconImage;
    public Text nameText;
    public Text costText;
    public Image progressBar;
    public GameObject lockedOverlay;
    public GameObject researchedOverlay;
    public Button researchButton;

    [Header("Всплывающая подсказка")]
    public GameObject tooltipPanel;
    public Text tooltipTitleText;
    public Text tooltipDescriptionText;
    public Text tooltipCostText;
    public Text tooltipRequirementsText;

    private TalentData technology;
    private TalentManager techManager;

    public void Initialize(TalentData tech, TalentManager manager)
    {
        technology = tech;
        techManager = manager;

        // Заполняем UI
        if (iconImage != null) iconImage.sprite = tech.Icon;
        if (nameText != null) nameText.text = tech.TalentName;

        // Формируем текст стоимости
        if (costText != null && tech.ResearchCost.Resources != null)
        {
            string costString = "";
            foreach (var cost in tech.ResearchCost.Resources)
            {
                costString += $"{cost.Type}: {cost.Amount}\n";
            }
            costText.text = costString;
        }

        // Настраиваем кнопку
        if (researchButton != null)
        {
            researchButton.onClick.AddListener(OnResearchButtonClicked);
        }

        UpdateUI();

        // Подписываемся на обновления дерева
        if (techManager != null)
        {
            techManager.OnTechnologyTreeUpdated += UpdateUI;
        }
    }

    private void OnDestroy()
    {
        if (techManager != null)
        {
            techManager.OnTechnologyTreeUpdated -= UpdateUI;
        }
    }

    public void UpdateUI()
    {
        if (technology == null || techManager == null) return;

        if (technology.IsResearched)
        {
            // Уже исследовано
            SetResearchedState();
        }
        else if (technology.IsAvailable)
        {
            // Доступно для исследования
            SetAvailableState();
        }
        else
        {
            // Заблокировано
            SetLockedState();
        }
    }

    private void SetResearchedState()
    {
        if (lockedOverlay != null) lockedOverlay.SetActive(false);
        if (researchedOverlay != null) researchedOverlay.SetActive(true);
        if (researchButton != null) researchButton.gameObject.SetActive(false);
        if (progressBar != null) progressBar.fillAmount = 1f;
    }

    private void SetAvailableState()
    {
        if (lockedOverlay != null) lockedOverlay.SetActive(false);
        if (researchedOverlay != null) researchedOverlay.SetActive(false);
        if (researchButton != null)
        {
            researchButton.gameObject.SetActive(true);
            researchButton.interactable = techManager.CanResearch(technology);
        }

        if (progressBar != null)
        {
            progressBar.fillAmount = techManager.GetResearchProgress(technology);
        }
    }

    private void SetLockedState()
    {
        if (lockedOverlay != null) lockedOverlay.SetActive(true);
        if (researchedOverlay != null) researchedOverlay.SetActive(false);
        if (researchButton != null) researchButton.gameObject.SetActive(false);
        if (progressBar != null) progressBar.fillAmount = 0f;
    }

    private void OnResearchButtonClicked()
    {
        if (techManager != null && technology != null)
        {
            techManager.ResearchTechnology(technology);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (tooltipPanel != null && technology != null)
        {
            tooltipPanel.SetActive(true);

            if (tooltipTitleText != null)
                tooltipTitleText.text = technology.TalentName;

            if (tooltipDescriptionText != null)
                tooltipDescriptionText.text = technology.Description;

            // Показываем стоимость
            if (tooltipCostText != null && technology.ResearchCost.Resources != null)
            {
                string costString = "Стоимость:\n";
                foreach (var cost in technology.ResearchCost.Resources)
                {
                    costString += $"  • {cost.Type}: {cost.Amount}\n";
                }
                tooltipCostText.text = costString;
            }

            // Показываем требования
            if (tooltipRequirementsText != null)
            {
                if (!technology.IsAvailable && !technology.IsResearched)
                {
                    string reqString = "Требуется:\n";
                    foreach (var prereq in technology.Prerequisites)
                    {
                        if (prereq != null && !prereq.IsResearched)
                        {
                            reqString += $"  • {prereq.TalentName}\n";
                        }
                    }
                    tooltipRequirementsText.text = reqString;
                }
                else
                {
                    tooltipRequirementsText.text = "";
                }
            }
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (tooltipPanel != null)
        {
            tooltipPanel.SetActive(false);
        }
    }
}
