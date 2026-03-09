using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// =============================================
// СУНДУК - выдает случайные карты
// =============================================
public class Chest : InteractableObject
{
    [Header("Настройки сундука")]
    [SerializeField] private List<Item> possibleItems; // ПЕРЕТАЩИ СЮДА КАРТЫ В ИНСПЕКТОРЕ!
    [SerializeField] private int minItems = 1;
    [SerializeField] private int maxItems = 3;
    [SerializeField] private bool hasKey = true;      // Всегда ли есть ключ
    [SerializeField] private GameObject openVisualEffect; // Для анимации открытия

    private bool isOpened = false;

    public override void Interact(Player player)
    {
        if (isOpened)
        {
            Debug.Log("Сундук уже пуст");
            return;
        }

        Debug.Log($"Открываем сундук {objectName}!");

        // Генерируем случайные карты
        int itemsCount = Random.Range(minItems, maxItems + 1);
        List<Item> itemsToGive = new List<Item>();

        for (int i = 0; i < itemsCount; i++)
        {
            if (possibleItems.Count > 0)
            {
                Item randomItem = possibleItems[Random.Range(0, possibleItems.Count)];
                Item itemCopy = CreateItemCopy(randomItem); // Копируем, чтобы не изменять оригинал
                itemsToGive.Add(itemCopy);
            }
        }

        // Добавляем ключ
        if (hasKey)
        {
            itemsToGive.Add(new Key());
        }

        // Пытаемся добавить все предметы игроку
        foreach (var item in itemsToGive)
        {
            if (!player.AddItem(item))
            {
                // Инвентарь полон - вызываем UI для выбора
                Debug.Log("Инвентарь полон! Нужно выбрать, что взять");
                OnInventoryFull(player, itemsToGive);
                return;
            }
        }

        isOpened = true;
        Debug.Log("Сундук успешно открыт!");

        // Эффект открытия для фронтенда
        if (openVisualEffect != null)
            Instantiate(openVisualEffect, transform.position, Quaternion.identity);
    }

    /// <summary>
    /// Создает копию предмета (важно для уникальности экземпляров)
    /// </summary>
    private Item CreateItemCopy(Item original)
    {
        if (original is AttackCard attack)
        {
            return new AttackCard(attack.itemName, attack.damage, attack.damageType);
        }
        if (original is HealCard heal)
        {
            return new HealCard(heal.itemName, heal.healAmount, heal.isPercentage);
        }
        if (original is BuffCard buff)
        {
            return new BuffCard(buff.itemName, buff.effectType, buff.effectValue, buff.duration);
        }

        Debug.LogError("Неизвестный тип предмета!");
        return null;
    }

    private void OnInventoryFull(Player player, List<Item> newItems)
    {
        // Здесь фронтендщик должен показать окно выбора
        // Временное решение - просто выбрасываем старый предмет
        Debug.LogWarning("ТУТ ДОЛЖЕН БЫТЬ UI ВЫБОРА");
    }

    public override string GetInteractionPrompt()
    {
        return isOpened ? "Сундук (пуст)" : "Открыть сундук";
    }
}

// =============================================
// ДВЕРЬ - требует ключ для открытия
// =============================================
public class Door : InteractableObject
{
    [Header("Настройки двери")]
    [SerializeField] private int doorId;           // Куда ведет дверь
    [SerializeField] private bool isLocked = true;
    [SerializeField] private bool isBossDoor = false; // Особо важная дверь
    [SerializeField] private GameObject lockedVisual;  // Визуал закрытой двери
    [SerializeField] private GameObject openVisual;    // Визуал открытой двери

    private bool isOpened = false;

    public override void Interact(Player player)
    {
        if (isOpened)
        {
            GoThroughDoor();
            return;
        }

        if (!isLocked)
        {
            OpenDoor();
            GoThroughDoor();
            return;
        }

        // Ищем ключ в инвентаре
        Key key = FindKeyInInventory(player);

        if (key != null)
        {
            player.RemoveItemFromInventory(key);
            isLocked = false;
            OpenDoor();
            GoThroughDoor();
            Debug.Log("Дверь открыта ключом!");
        }
        else
        {
            Debug.Log("Дверь заперта. Нужен ключ!");
            OnPlayerMessage("Дверь заперта. Нужен ключ!");
        }
    }

    private Key FindKeyInInventory(Player player)
    {
        foreach (var item in player.Inventory)
        {
            if (item is Key)
                return item as Key;
        }
        return null;
    }

    private void OpenDoor()
    {
        isOpened = true;

        // Меняем визуал
        if (lockedVisual != null) lockedVisual.SetActive(false);
        if (openVisual != null) openVisual.SetActive(true);

        Debug.Log("Дверь открыта!");
    }

    private void GoThroughDoor()
    {
        Debug.Log($"Игрок проходит через дверь {doorId}");
        // GameManager.Instance.EnterRoom(doorId);
    }

    private void OnPlayerMessage(string message)
    {
        // Для UI лога
        Debug.Log(message);
    }

    public override string GetInteractionPrompt()
    {
        if (isOpened) return "Дверь (открыта)";
        if (isLocked) return "Дверь (заперта)";
        return "Дверь (открыта)";
    }
}

// =============================================
// АЛТАРЬ - позволяет использовать карты на себе
// =============================================
public class Altar : InteractableObject
{
    [Header("Настройки алтаря")]
    [SerializeField] private ParticleSystem activateEffect;
    [SerializeField] private AudioClip useSound;

    public override void Interact(Player player)
    {
        Debug.Log("Алтарь активирован. Выберите карту для использования");

        // Показываем UI с картами игрока
        ShowSelfUseUI(player);
    }

    private void ShowSelfUseUI(Player player)
    {
        Debug.Log("=== ВАШИ КАРТЫ (выбери для использования на себе) ===");
        foreach (var item in player.Inventory)
        {
            Debug.Log($"- {item.itemName}: {item.description}");
        }
        Debug.Log("(Здесь фронтендщик сделает нормальное UI)");
    }

    public override string GetInteractionPrompt()
    {
        return "Алтарь (использовать карту)";
    }
}