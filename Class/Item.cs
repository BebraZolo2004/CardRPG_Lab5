using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// =============================================
// БАЗОВЫЙ КЛАСС ПРЕДМЕТА
// От этого класса наследуются ВСЕ карты и ключи
// =============================================
[System.Serializable] // Позволяет видеть поля в Inspector
public abstract class Item
{
    [Header("Основные характеристики предмета")]
    public string itemName;      // Название для UI
    [TextArea(2, 4)]
    public string description;    // Описание эффекта
    public Sprite icon;           // Иконка (вставит фронтендщик)
    public int rarity = 1;        // Редкость (1 - обычная, 3 - редкая)

    /// <summary>
    /// ГЛАВНЫЙ МЕТОД - вызывается при использовании карты
    /// </summary>
    /// <param name="player">Игрок, который использует карту</param>
    /// <param name="target">Цель (монстр или null, если карта на себя)</param>
    public abstract void Use(Player player, Monster target = null);

    /// <summary>
    /// Для UI - возвращает цвет рамки в зависимости от редкости
    /// </summary>
    public virtual Color GetRarityColor()
    {
        switch (rarity)
        {
            case 1: return Color.white;   // Обычные
            case 2: return Color.blue;    // Необычные
            case 3: return Color.magenta; // Редкие
            default: return Color.white;
        }
    }
}

// =============================================
// ТИП 1: АТАКУЮЩИЕ КАРТЫ
// Используются на монстрах для нанесения урона
// =============================================
public class AttackCard : Item
{
    [Header("Характеристики атаки")]
    public int damage;           // Базовый урон
    public DamageType damageType; // Тип урона (огонь, холод, физика)

    // Конструктор для создания через код
    public AttackCard(string name, int dmg, DamageType type = DamageType.Physical)
    {
        itemName = name;
        damage = dmg;
        damageType = type;
        description = $"Наносит {damage} ед. {GetDamageTypeName()} урона";
    }

    public override void Use(Player player, Monster target = null)
    {
        if (target == null)
        {
            Debug.LogWarning("Нельзя использовать атакующую карту без цели!");
            return;
        }

        // Проверка на уязвимости монстра
        float finalDamage = damage;
        if (target.Vulnerabilities.Contains(damageType))
        {
            finalDamage *= 1.5f; // Уязвимость = урон х1.5
            Debug.Log("КРИТИЧЕСКИЙ УРОН! Монстр уязвим к " + damageType);
        }

        target.TakeDamage((int)finalDamage);
        Debug.Log($"Игрок использовал {itemName} и нанес {finalDamage} урона");

        // Одноразовое использование (карта исчезает)
        player.RemoveItemFromInventory(this);
    }

    private string GetDamageTypeName()
    {
        switch (damageType)
        {
            case DamageType.Physical: return "физического";
            case DamageType.Fire: return "огненного";
            case DamageType.Ice: return "ледяного";
            default: return "";
        }
    }
}

// Типы урона (для уязвимостей монстров)
public enum DamageType { Physical, Fire, Ice, Poison }

// =============================================
// ТИП 2: ЛЕЧАЩИЕ КАРТЫ
// Используются на игроке для восстановления здоровья
// =============================================
public class HealCard : Item
{
    [Header("Характеристики лечения")]
    public int healAmount;       // Сколько лечит
    public bool isPercentage;    // Процентное или фиксированное лечение

    public HealCard(string name, int heal, bool percentage = false)
    {
        itemName = name;
        healAmount = heal;
        isPercentage = percentage;

        if (percentage)
            description = $"Восстанавливает {heal}% от максимального здоровья";
        else
            description = $"Восстанавливает {heal} ед. здоровья";
    }

    public override void Use(Player player, Monster target = null)
    {
        int actualHeal = healAmount;

        if (isPercentage)
        {
            // Процент от максимального здоровья
            actualHeal = (player.MaxHealth * healAmount) / 100;
        }

        player.Heal(actualHeal);
        Debug.Log($"Игрок использовал {itemName} и восстановил {actualHeal} HP");

        player.RemoveItemFromInventory(this);
    }
}

// =============================================
// ТИП 3: КАРТЫ БАФФОВ/ДЕБАФФОВ
// Могут влиять на игрока или на монстра
// =============================================
public class BuffCard : Item
{
    [Header("Тип эффекта")]
    public BuffEffectType effectType;

    [Header("Сила эффекта")]
    public int effectValue;       // +5 к атаке или -5 к защите

    [Header("Длительность")]
    public int duration = 3;      // На сколько ходов

    public BuffCard(string name, BuffEffectType type, int value, int dur = 3)
    {
        itemName = name;
        effectType = type;
        effectValue = value;
        duration = dur;
        description = GetDescription();
    }

    public override void Use(Player player, Monster target = null)
    {
        switch (effectType)
        {
            case BuffEffectType.PlayerDamageBuff:
                player.AddBuff(new Buff(BuffType.Damage, effectValue, duration));
                Debug.Log($"Игрок усилил атаку на {effectValue} на {duration} хода");
                break;

            case BuffEffectType.PlayerDefenseBuff:
                player.AddBuff(new Buff(BuffType.Defense, effectValue, duration));
                Debug.Log($"Игрок усилил защиту на {effectValue}");
                break;

            case BuffEffectType.MonsterDebuff:
                if (target != null)
                {
                    target.AddDebuff(new Buff(BuffType.DamageReduction, effectValue, duration));
                    Debug.Log($"Монстр ослаблен: урон -{effectValue}");
                }
                break;

            case BuffEffectType.MonsterStun:
                if (target != null)
                {
                    target.Stun(duration);
                    Debug.Log($"Монстр оглушен на {duration} хода!");
                }
                break;
        }

        player.RemoveItemFromInventory(this);
    }

    private string GetDescription()
    {
        switch (effectType)
        {
            case BuffEffectType.PlayerDamageBuff:
                return $"+{effectValue} к урону на {duration} хода";
            case BuffEffectType.MonsterDebuff:
                return $"Ослабляет монстра: -{effectValue} урона";
            case BuffEffectType.MonsterStun:
                return $"Оглушает монстра на {duration} хода";
            default:
                return $"Эффект {effectType}";
        }
    }
}

// Типы эффектов для карт
public enum BuffEffectType
{
    PlayerDamageBuff,   // Бафф атаки игрока
    PlayerDefenseBuff,  // Бафф защиты игрока
    MonsterDebuff,      // Дебафф монстра
    MonsterStun         // Оглушение
}

// =============================================
// КЛЮЧ - особый предмет для открытия дверей
// =============================================
public class Key : Item
{
    public Key()
    {
        itemName = "Ключ";
        description = "Открывает одну запертую дверь";
        rarity = 2; // Ключи редкие
    }

    public override void Use(Player player, Monster target = null)
    {
        Debug.Log("Ключ нельзя использовать напрямую. Подойди к двери!");
        // Здесь можно вызвать событие для UI
    }
}