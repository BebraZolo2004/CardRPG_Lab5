using System.Collections;
using System.Collections.Generic;
using UnityEngine;


// =============================================
// БАЗОВЫЙ КЛАСС МОНСТРА
// Наследуйся от него, чтобы создать нового монстра
// =============================================
public abstract class Monster : MonoBehaviour
{
    [Header("Основные характеристики монстра (ЗАПОЛНИ В ИНСПЕКТОРЕ!)")]
    public string monsterName = "Монстр";
    public int MaxHealth = 20;
    public int Health { get; protected set; }
    public int Damage = 3;

    [Header("Уязвимости (добавь типы урона, к которым монстр слаб)")]
    public List<DamageType> Vulnerabilities = new List<DamageType>();

    [Header("Сопротивления (урон снижается вдвое)")]
    public List<DamageType> Resistances = new List<DamageType>();

    [Header("Визуал")]
    public Sprite monsterSprite;
    public RuntimeAnimatorController animatorController;

    // События для UI
    public System.Action<int, int> OnHealthChanged;
    public System.Action<string> OnMonsterMessage;

    protected virtual void Start()
    {
        Health = MaxHealth;
        Debug.Log($"Монстр {monsterName} появился: {Health} HP, {Damage} урона");
    }

    /// <summary>
    /// ГЛАВНЫЙ МЕТОД - определяет, что монстр сделает в свой ход
    /// </summary>
    public abstract MonsterAction GetNextAction();

    /// <summary>
    /// Получение урона с учетом сопротивлений и уязвимостей
    /// </summary>
    public virtual void TakeDamage(int damage, DamageType damageType = DamageType.Physical)
    {
        float finalDamage = damage;

        if (Vulnerabilities.Contains(damageType))
        {
            finalDamage *= 1.5f;
            OnMonsterMessage?.Invoke($"{monsterName} уязвим! Урон увеличен!");
        }
        else if (Resistances.Contains(damageType))
        {
            finalDamage *= 0.5f;
            OnMonsterMessage?.Invoke($"{monsterName} сопротивляется! Урон снижен!");
        }

        Health -= (int)finalDamage;
        OnHealthChanged?.Invoke(Health, MaxHealth);

        Debug.Log($"{monsterName} получил {finalDamage} урона. Осталось {Health} HP");

        if (Health <= 0)
        {
            Die();
        }
    }

    public virtual void Attack(Player player)
    {
        player.TakeDamage(Damage);
        OnMonsterMessage?.Invoke($"{monsterName} атакует и наносит {Damage} урона!");
    }

    protected virtual void Die()
    {
        Debug.Log($"{monsterName} повержен!");
        OnMonsterMessage?.Invoke($"{monsterName} пал в бою!");

        // Спавним сундук с наградой
        SpawnRewardChest();

        Destroy(gameObject, 1f); // Удаляем монстра через секунду
    }

    protected virtual void SpawnRewardChest()
    {
        Debug.Log("Монстр оставил сундук с сокровищами!");
        // Здесь GameManager должен создать сундук на месте монстра
    }
}

// =============================================
// ПРИМЕР 1: Монстр с ЦИКЛИЧЕСКИМ поведением
// Делает действия по порядку
// =============================================
public class CyclicMonster : Monster
{
    [Header("Настройка цикла действий")]
    [SerializeField] private List<MonsterActionType> actionPattern;
    private int currentActionIndex = 0;

    protected override void Start()
    {
        base.Start();

        // Если паттерн не задан, создаем стандартный
        if (actionPattern == null || actionPattern.Count == 0)
        {
            actionPattern = new List<MonsterActionType>
            {
                MonsterActionType.Attack,
                MonsterActionType.Defend,
                MonsterActionType.Attack,
                MonsterActionType.Charge
            };
        }
    }

    public override MonsterAction GetNextAction()
    {
        MonsterActionType actionType = actionPattern[currentActionIndex];
        currentActionIndex = (currentActionIndex + 1) % actionPattern.Count;

        switch (actionType)
        {
            case MonsterActionType.Attack:
                return new MonsterAttackAction(this);
            case MonsterActionType.Defend:
                return new MonsterDefendAction(this);
            case MonsterActionType.Charge:
                return new MonsterChargeAction(this);
            case MonsterActionType.Heal:
                return new MonsterHealAction(this);
            default:
                return new MonsterAttackAction(this);
        }
    }
}

// =============================================
// ПРИМЕР 2: Монстр со СЛУЧАЙНЫМ поведением
// Каждый ход выбирает случайное действие
// =============================================
public class RandomMonster : Monster
{
    [Header("Шансы действий (в процентах)")]
    [SerializeField][Range(0, 100)] private int attackChance = 40;
    [SerializeField][Range(0, 100)] private int defendChance = 30;
    [SerializeField][Range(0, 100)] private int specialChance = 30;

    public override MonsterAction GetNextAction()
    {
        int roll = Random.Range(0, 100);

        if (roll < attackChance)
        {
            return new MonsterAttackAction(this);
        }
        else if (roll < attackChance + defendChance)
        {
            return new MonsterDefendAction(this);
        }
        else
        {
            return new MonsterSpecialAction(this);
        }
    }
}

// =============================================
// ПРИМЕР 3: Монстр с УМНЫМ поведением (реагирует на игрока)
// =============================================
public class SmartMonster : Monster
{
    private bool isEnraged = false;
    private int turnsSinceLastAttack = 0;

    public override MonsterAction GetNextAction()
    {
        turnsSinceLastAttack++;

        // Если здоровье меньше 30% - в ярость
        if (Health < MaxHealth * 0.3f && !isEnraged)
        {
            isEnraged = true;
            Damage *= 2;
            return new MonsterSpecialAction(this, "ЯРОСТЬ: Урон удвоен!");
        }

        // Если давно не атаковал - точно атакует
        if (turnsSinceLastAttack > 2)
        {
            turnsSinceLastAttack = 0;
            return new MonsterAttackAction(this);
        }

        // Иначе рандом
        return Random.value > 0.5f ?
            (MonsterAction)new MonsterAttackAction(this) :
            (MonsterAction)new MonsterDefendAction(this);
    }
}

// =============================================
// Типы действий для монстров
// =============================================
public enum MonsterActionType { Attack, Defend, Charge, Heal, Special }

// =============================================
// БАЗОВЫЙ КЛАСС ДЕЙСТВИЯ МОНСТРА
// =============================================
public abstract class MonsterAction
{
    public abstract void Execute(Monster monster, Player player);
    public abstract string GetDescription();
}

// Атака
public class MonsterAttackAction : MonsterAction
{
    private Monster owner;

    public MonsterAttackAction(Monster m) { owner = m; }

    public override void Execute(Monster monster, Player player)
    {
        monster.Attack(player);
    }

    public override string GetDescription()
    {
        return $"{owner.monsterName} готовится атаковать!";
    }
}

// Защита
public class MonsterDefendAction : MonsterAction
{
    private Monster owner;
    private int defenseBonus = 3;

    public MonsterDefendAction(Monster m) { owner = m; }

    public override void Execute(Monster monster, Player player)
    {
        // Логика защиты - монстр получает меньше урона в следующий ход
        Debug.Log($"{owner.monsterName} защищается! Следующая атака нанесет -{defenseBonus} урона");
    }

    public override string GetDescription()
    {
        return $"{owner.monsterName} защищается!";
    }
}

// Зарядка (следующая атака сильнее)
public class MonsterChargeAction : MonsterAction
{
    private Monster owner;

    public MonsterChargeAction(Monster m) { owner = m; }

    public override void Execute(Monster monster, Player player)
    {
        Debug.Log($"{owner.monsterName} накапливает силу! Следующая атака будет мощнее!");
        // В реальном коде здесь нужно добавить бафф на следующий ход
    }

    public override string GetDescription()
    {
        return $"{owner.monsterName} накапливает энергию!";
    }
}

// Лечение
public class MonsterHealAction : MonsterAction
{
    private Monster owner;

    public MonsterHealAction(Monster m) { owner = m; }

    public override void Execute(Monster monster, Player player)
    {
        int healAmount = owner.MaxHealth / 5;
        owner.Health = Mathf.Min(owner.Health + healAmount, owner.MaxHealth);
        Debug.Log($"{owner.monsterName} лечится! +{healAmount} HP");
    }

    public override string GetDescription()
    {
        return $"{owner.monsterName} лечится!";
    }
}

// Особое действие
public class MonsterSpecialAction : MonsterAction
{
    private Monster owner;
    private string specialMessage;

    public MonsterSpecialAction(Monster m, string msg = "использует особую атаку!")
    {
        owner = m;
        specialMessage = msg;
    }

    public override void Execute(Monster monster, Player player)
    {
        // Здесь может быть что угодно - яд, проклятье, призыв миньонов
        player.TakeDamage(owner.Damage + 2);
        Debug.Log($"{owner.monsterName} {specialMessage}");
    }

    public override string GetDescription()
    {
        return $"{owner.monsterName} {specialMessage}";
    }
}