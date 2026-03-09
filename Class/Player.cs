using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// =============================================
// КЛАСС ИГРОКА - хранит состояние персонажа
// =============================================
public class Player
{
    [Header("Основные характеристики")]
    public int Health { get; private set; }
    public int MaxHealth { get; private set; }
    public int InventorySize { get; private set; }
    public int BaseDamage { get; private set; } = 2; // Базовый урон без карт

    [Header("Инвентарь")]
    private List<Item> inventory = new List<Item>();
    public IReadOnlyList<Item> Inventory => inventory; // Только для чтения из UI

    [Header("Активные баффы")]
    private List<Buff> activeBuffs = new List<Buff>();

    // События для UI (фронтендщик подпишется)
    public System.Action<int, int> OnHealthChanged; // текущее, максимальное
    public System.Action<List<Item>> OnInventoryChanged;
    public System.Action<string> OnPlayerMessage; // для лога боя

    public Player(int maxHealth, int inventorySize)
    {
        MaxHealth = maxHealth;
        Health = maxHealth;
        InventorySize = inventorySize;
        Debug.Log($"Игрок создан: HP {Health}, инвентарь {InventorySize} слотов");
    }

    // ========== УПРАВЛЕНИЕ ИНВЕНТАРЕМ ==========

    /// <summary>
    /// Добавить предмет в инвентарь
    /// </summary>
    public bool AddItem(Item item)
    {
        if (inventory.Count >= InventorySize)
        {
            OnPlayerMessage?.Invoke("Инвентарь полон! Придется что-то выбросить.");
            return false;
        }

        inventory.Add(item);
        OnInventoryChanged?.Invoke(inventory);
        OnPlayerMessage?.Invoke($"Подобран предмет: {item.itemName}");
        Debug.Log($"Предмет {item.itemName} добавлен в инвентарь. Мест: {inventory.Count}/{InventorySize}");
        return true;
    }

    /// <summary>
    /// Удалить предмет из инвентаря
    /// </summary>
    public void RemoveItemFromInventory(Item item)
    {
        if (inventory.Contains(item))
        {
            inventory.Remove(item);
            OnInventoryChanged?.Invoke(inventory);
            Debug.Log($"Предмет {item.itemName} удален из инвентаря");
        }
    }

    /// <summary>
    /// Выбросить предмет (вызывается из UI)
    /// </summary>
    public void DropItem(Item item)
    {
        RemoveItemFromInventory(item);
        OnPlayerMessage?.Invoke($"Вы выбросили {item.itemName}");
    }

    // ========== БОЕВЫЕ МЕТОДЫ ==========

    /// <summary>
    /// Расчет итогового урона с учетом баффов
    /// </summary>
    public int CalculateDamage()
    {
        int totalDamage = BaseDamage;

        // Применяем все активные баффы
        foreach (var buff in activeBuffs)
        {
            if (buff.type == BuffType.Damage)
                totalDamage += buff.value;
        }

        return totalDamage;
    }

    /// <summary>
    /// Получение урона (с учетом защиты)
    /// </summary>
    public void TakeDamage(int damage)
    {
        int defense = 0;

        // Считаем защиту из баффов
        foreach (var buff in activeBuffs)
        {
            if (buff.type == BuffType.Defense)
                defense += buff.value;
        }

        int finalDamage = Mathf.Max(1, damage - defense); // Минимум 1 урон
        Health -= finalDamage;

        OnHealthChanged?.Invoke(Health, MaxHealth);
        OnPlayerMessage?.Invoke($"Игрок получил {finalDamage} урона. Осталось {Health} HP");

        if (Health <= 0)
        {
            Die();
        }
    }

    public void Heal(int amount)
    {
        Health = Mathf.Min(Health + amount, MaxHealth);
        OnHealthChanged?.Invoke(Health, MaxHealth);
        OnPlayerMessage?.Invoke($"Игрок восстановил {amount} HP. Текущее HP: {Health}");
    }

    // ========== БАФФЫ ==========

    public void AddBuff(Buff buff)
    {
        activeBuffs.Add(buff);
        OnPlayerMessage?.Invoke($"Получен бафф: {buff.GetDescription()}");
    }

    public void UpdateBuffs()
    {
        // Уменьшаем длительность баффов (вызывается каждый ход)
        for (int i = activeBuffs.Count - 1; i >= 0; i--)
        {
            activeBuffs[i].duration--;
            if (activeBuffs[i].duration <= 0)
            {
                activeBuffs.RemoveAt(i);
            }
        }
    }

    private void Die()
    {
        OnPlayerMessage?.Invoke("ИГРОК ПОГИБ... Конец игры");
        Debug.LogError("PLAYER DIED - GAME OVER");
        // Здесь GameManager должен показать экран поражения
    }
}

// =============================================
// КЛАСС БАФФА для временных эффектов
// =============================================
public class Buff
{
    public BuffType type;
    public int value;
    public int duration;

    public Buff(BuffType t, int v, int d)
    {
        type = t;
        value = v;
        duration = d;
    }

    public string GetDescription()
    {
        string typeName = type == BuffType.Damage ? "атака" : "защита";
        return $"{typeName} {value} на {duration} ходов";
    }
}

public enum BuffType { Damage, Defense, DamageReduction }