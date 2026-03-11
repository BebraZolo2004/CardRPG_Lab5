using System.Collections.Generic;
using UnityEngine;

public class AssetGenerator : MonoBehaviour
{
    [Header("Список карт для GameManager")]
    public List<Item> generatedCards = new List<Item>();
 
    private void Awake()
    {
        GenerateCards();
        // Если GameManager уже есть, добавляем карты туда
        if (GameManager.Instance != null)
        {
            GameManager.Instance.allCards.AddRange(generatedCards);
            Debug.Log($"✅ Добавлено {generatedCards.Count} карт в GameManager");
        }
    }

    private void GenerateCards()
    {
        Debug.Log("🏗 Генерация карт из спрайтов...");

        // --- Attack карты ---
        Sprite[] attackSprites = Resources.LoadAll<Sprite>("Sprites/Cards/Attack");
        foreach (var sprite in attackSprites)
        {
            AttackCard card = new AttackCard(sprite.name, 5, DamageType.Physical); // базовый урон 5
            card.icon = sprite;
            generatedCards.Add(card);
            Debug.Log($"✔ Создана Attack карта: {sprite.name}");
        }

        // --- Heal карты ---
        Sprite[] healSprites = Resources.LoadAll<Sprite>("Sprites/Cards/Heal");
        foreach (var sprite in healSprites)
        {
            HealCard card = new HealCard(sprite.name, 5, false); // базовое лечение 5
            card.icon = sprite;
            generatedCards.Add(card);
            Debug.Log($"✔ Создана Heal карта: {sprite.name}");
        }

        // --- Buff карты ---
        Sprite[] buffSprites = Resources.LoadAll<Sprite>("Sprites/Cards/Buff");
        foreach (var sprite in buffSprites)
        {
            BuffCard card = new BuffCard(sprite.name, BuffEffectType.PlayerDamageBuff, 3, 3);
            card.icon = sprite;
            generatedCards.Add(card);
            Debug.Log($"✔ Создана Buff карта: {sprite.name}");
        }

        Debug.Log($"✅ Всего карт создано: {generatedCards.Count}");

        // ВЫВОД ВСЕХ КАРТ ДЛЯ ПРОВЕРКИ
        Debug.Log("📋 Проверка всех созданных карт:");
        foreach (var card in generatedCards)
        {
            Debug.Log($"- {card.itemName} | Тип: {card.GetType().Name}");
        }
    }
}