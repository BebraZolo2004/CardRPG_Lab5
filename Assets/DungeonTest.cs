using System;
using System.Collections.Generic;
using System.Linq;


namespace DungeonTest
{
    // =============================================
    // ТЕСТОВЫЙ УРОВЕНЬ - консольная версия для проверки механик
    // =============================================
    class Program
    {
        static GameManager gameManager;
        static Player player;
        static bool gameRunning = true;

        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.Title = "DUNGEON TEST";

            InitializeTestGame();
            MainGameLoop();
        }

        static void InitializeTestGame()
        {
            Console.WriteLine("========================================");
            Console.WriteLine("    ТЕСТОВОЕ ПОДЗЕМЕЛЬЕ - КОНСОЛЬ");
            Console.WriteLine("========================================\n");

            // Создаем GameManager
            gameManager = new GameManager();

            // Создаем игрока (40 HP, 5 слотов в инвентаре)
            player = new Player(40, 5);

            // Подписываемся на события игрока для вывода в консоль
            player.OnPlayerMessage += (msg) => Console.WriteLine($"[ИГРОК] {msg}");

            // СОЗДАЕМ ТЕСТОВЫЕ КАРТЫ
            Console.WriteLine("ЗАГРУЗКА КАРТ:");

            // Атакующие карты
            var fireball = new AttackCard("Огненный шар", 8, DamageType.Fire);
            var iceShard = new AttackCard("Ледяной шип", 6, DamageType.Ice);
            var dagger = new AttackCard("Кинжал", 4, DamageType.Physical);
            var poison = new AttackCard("Отравленный клинок", 5, DamageType.Poison);

            // Лечащие карты
            var smallHeal = new HealCard("Малая фляга", 5, false);
            var bigHeal = new HealCard("Большое зелье", 30, true); // 30% от макс HP

            // Бафф карты
            var rage = new BuffCard("Ярость", BuffEffectType.PlayerDamageBuff, 3, 3);
            var shield = new BuffCard("Каменная кожа", BuffEffectType.PlayerDefenseBuff, 4, 2);
            var weaken = new BuffCard("Проклятье", BuffEffectType.MonsterDebuff, 2, 3);
            var stun = new BuffCard("Оглушение", BuffEffectType.MonsterStun, 1, 1);

            // Добавляем карты в глобальный пул
            gameManager.allCards.AddRange(new Item[] {
                fireball, iceShard, dagger, poison,
                smallHeal, bigHeal, rage, shield, weaken, stun
            });

            Console.WriteLine($"  Загружено карт: {gameManager.allCards.Count}");
            foreach (var card in gameManager.allCards)
            {
                Console.WriteLine($"    - {card.itemName}: {card.description}");
            }

            // СОЗДАЕМ ТЕСТОВЫХ МОНСТРОВ
            Console.WriteLine("\nЗАГРУЗКА МОНСТРОВ:");

            var goblin = new CyclicMonster
            {
                monsterName = "Гоблин",
                MaxHealth = 15,
                Damage = 3,
                Vulnerabilities = new List<DamageType> { DamageType.Fire },
                Resistances = new List<DamageType> { DamageType.Poison }
            };

            var orc = new RandomMonster
            {
                monsterName = "Орк",
                MaxHealth = 25,
                Damage = 5,
                Vulnerabilities = new List<DamageType> { DamageType.Ice }
            };

            var troll = new SmartMonster
            {
                monsterName = "Тролль",
                MaxHealth = 35,
                Damage = 4,
                Vulnerabilities = new List<DamageType> { DamageType.Fire }
            };

            gameManager.allMonsters.AddRange(new Monster[] { goblin, orc, troll });

            Console.WriteLine($"  Загружено монстров: {gameManager.allMonsters.Count}");
            foreach (var monster in gameManager.allMonsters)
            {
                Console.WriteLine($"    - {monster.monsterName}: {monster.MaxHealth} HP, {monster.Damage} урона");
            }

            // ДАЕМ ИГРОКУ СТАРТОВЫЙ НАБОР
            Console.WriteLine("\nВЫДАЧА СТАРТОВОГО СНАРЯЖЕНИЯ:");
            player.AddItem(dagger);
            player.AddItem(smallHeal);
            player.AddItem(rage);

            Console.WriteLine("\n========================================\n");
            Console.WriteLine("Нажмите любую клавишу для начала...");
            Console.ReadKey();
            Console.Clear();
        }

        static void MainGameLoop()
        {
            int roomNumber = 1;

            while (gameRunning && player.Health > 0)
            {
                Console.WriteLine($"\n========== КОМНАТА {roomNumber} ==========");

                // С вероятностью 70% в комнате будет монстр
                if (new Random().NextDouble() < 0.7 && gameManager.allMonsters.Count > 0)
                {
                    // Выбираем случайного монстра
                    Monster monster = gameManager.GetRandomMonster();
                    Console.WriteLine($"\n⚠ В комнате МОНСТР: {monster.monsterName}!");
                    Console.WriteLine($"   Здоровье: {monster.MaxHealth} | Урон: {monster.Damage}");

                    // Начинаем бой
                    CombatLoop(monster);

                    if (player.Health <= 0)
                    {
                        Console.WriteLine("\n💀 ВЫ ПОГИБЛИ В БОЮ...");
                        break;
                    }

                    Console.WriteLine("\n🎉 МОНСТР ПОВЕРЖЕН!");

                    // Монстр оставляет сундук
                    Console.WriteLine("\n📦 Монстр оставил сундук! Открываем...");
                    TestChest(monster);
                }
                else
                {
                    Console.WriteLine("\n📦 В комнате стоит сундук!");
                    TestChest(null);
                }

                // Показываем состояние игрока
                ShowPlayerStatus();

                // Спрашиваем, идти дальше или закончить тест
                Console.WriteLine("\n1 - Идти дальше | 2 - Закончить тест");
                var key = Console.ReadKey(true);
                if (key.KeyChar == '2')
                {
                    gameRunning = false;
                }

                roomNumber++;
                Console.Clear();
            }

            Console.WriteLine("\n========================================");
            Console.WriteLine("            ТЕСТ ЗАВЕРШЕН");
            Console.WriteLine("========================================");
            ShowFinalStats();
        }

        static void CombatLoop(Monster monster)
        {
            int turn = 1;
            bool combatRunning = true;
            monster.OnMonsterMessage += (msg) => Console.WriteLine($"[МОНСТР] {msg}");

            while (combatRunning && player.Health > 0 && monster.Health > 0)
            {
                Console.WriteLine($"\n--- ХОД {turn} ---");

                // Ход игрока
                Console.WriteLine("\nТВОЙ ХОД. Выбери действие:");
                Console.WriteLine("1 - Атаковать (базовый урон)");
                Console.WriteLine("2 - Использовать карту из инвентаря");
                Console.WriteLine("3 - Посмотреть инвентарь");
                Console.WriteLine("4 - Пропустить ход");

                var choice = Console.ReadKey(true);

                switch (choice.KeyChar)
                {
                    case '1':
                        int damage = player.CalculateDamage();
                        Console.WriteLine($"  Атака! Нанесено {damage} урона");
                        monster.TakeDamage(damage, DamageType.Physical);
                        break;

                    case '2':
                        UseCardInCombat(monster);
                        break;

                    case '3':
                        ShowInventory();
                        continue; // Не засчитываем ход

                    case '4':
                        Console.WriteLine("  Игрок пропускает ход");
                        break;
                }

                if (monster.Health <= 0)
                {
                    combatRunning = false;
                    break;
                }

                // Ход монстра
                Console.WriteLine($"\n--- ХОД МОНСТРА ---");
                var monsterAction = monster.GetNextAction();
                Console.WriteLine($"  {monsterAction.GetDescription()}");
                monsterAction.Execute(monster, player);

                // Обновляем баффы игрока
                player.UpdateBuffs();

                turn++;
                Console.WriteLine("\nНажми любую клавишу для продолжения...");
                Console.ReadKey(true);
            }
        }

        static void UseCardInCombat(Monster target)
        {
            var inventory = player.Inventory.ToList();
            if (inventory.Count == 0)
            {
                Console.WriteLine("  Инвентарь пуст!");
                return;
            }

            Console.WriteLine("\nВЫБЕРИ КАРТУ:");
            for (int i = 0; i < inventory.Count; i++)
            {
                var card = inventory[i];
                Console.WriteLine($"  {i + 1} - {card.itemName}: {card.description}");
            }
            Console.WriteLine("  0 - Отмена");

            if (int.TryParse(Console.ReadKey(true).KeyChar.ToString(), out int index))
            {
                if (index == 0) return;
                if (index > 0 && index <= inventory.Count)
                {
                    var selectedCard = inventory[index - 1];

                    // Определяем, можно ли использовать карту на монстре
                    if (selectedCard is AttackCard || (selectedCard is BuffCard &&
                        ((BuffCard)selectedCard).effectType == BuffEffectType.MonsterDebuff))
                    {
                        selectedCard.Use(player, target);
                    }
                    else
                    {
                        Console.WriteLine("  Эту карту нельзя использовать в бою на монстре!");
                    }
                }
            }
        }

        static void TestChest(Monster guardian = null)
        {
            // Создаем тестовый сундук
            var chest = new Chest();

            // Добавляем случайные карты из пула
            var random = new Random();
            int cardCount = random.Next(2, 4);

            for (int i = 0; i < cardCount; i++)
            {
                if (gameManager.allCards.Count > 0)
                {
                    var card = gameManager.allCards[random.Next(gameManager.allCards.Count)];
                    chest.possibleItems.Add(card);
                }
            }

            // Если был страж, добавляем ключ
            if (guardian != null)
            {
                chest.hasKey = true;
                Console.WriteLine("  В сундуке блестит ключ!");
            }

            // Открываем сундук
            chest.Interact(player);
        }

        static void ShowInventory()
        {
            Console.WriteLine("\n=== ИНВЕНТАРЬ ===");
            if (player.Inventory.Count() == 0)
            {
                Console.WriteLine("  Пусто");
            }
            else
            {
                foreach (var item in player.Inventory)
                {
                    Console.WriteLine($"  • {item.itemName}: {item.description}");
                }
            }
            Console.WriteLine($"  Мест: {player.Inventory.Count()}/{player.InventorySize}");
        }

        static void ShowPlayerStatus()
        {
            Console.WriteLine("\n=== СОСТОЯНИЕ ИГРОКА ===");
            Console.WriteLine($"  Здоровье: {player.Health}/{player.MaxHealth}");
            Console.WriteLine($"  Базовый урон: {player.BaseDamage}");
            ShowInventory();
        }

        static void ShowFinalStats()
        {
            Console.WriteLine($"\nПройдено комнат: ???");
            Console.WriteLine($"Осталось здоровья: {player.Health}");
            Console.WriteLine($"Карт в инвентаре: {player.Inventory.Count()}");
        }
    }

    // =============================================
    // МИНИМАЛЬНЫЙ GameManager для теста
    // =============================================
    public class GameManager
    {
        public List<Item> allCards = new List<Item>();
        public List<Monster> allMonsters = new List<Monster>();

        public Monster GetRandomMonster()
        {
            if (allMonsters.Count == 0) return null;

            // Создаем копию монстра (важно для теста)
            var original = allMonsters[new Random().Next(allMonsters.Count)];

            if (original is CyclicMonster)
            {
                return new CyclicMonster
                {
                    monsterName = original.monsterName,
                    MaxHealth = original.MaxHealth,
                    Damage = original.Damage,
                    Vulnerabilities = original.Vulnerabilities,
                    Resistances = original.Resistances
                };
            }
            else if (original is RandomMonster)
            {
                return new RandomMonster
                {
                    monsterName = original.monsterName,
                    MaxHealth = original.MaxHealth,
                    Damage = original.Damage
                };
            }

            return original;
        }
    }

    // =============================================
    // МИНИМАЛЬНЫЙ Chest для теста
    // =============================================
    public class Chest
    {
        public List<Item> possibleItems = new List<Item>();
        public bool hasKey = true;

        public void Interact(Player player)
        {
            Console.WriteLine("  Сундук открывается...");

            if (hasKey)
            {
                var key = new Key();
                if (player.AddItem(key))
                {
                    Console.WriteLine("  ✓ Найден КЛЮЧ!");
                }
            }

            if (possibleItems.Count > 0)
            {
                var random = new Random();
                int itemsToGive = random.Next(1, possibleItems.Count + 1);

                for (int i = 0; i < itemsToGive; i++)
                {
                    var item = possibleItems[random.Next(possibleItems.Count)];

                    // Создаем копию карты
                    Item copy = null;
                    if (item is AttackCard attack)
                        copy = new AttackCard(attack.itemName, attack.damage, attack.damageType);
                    else if (item is HealCard heal)
                        copy = new HealCard(heal.itemName, heal.healAmount, heal.isPercentage);
                    else if (item is BuffCard buff)
                        copy = new BuffCard(buff.itemName, buff.effectType, buff.effectValue, buff.duration);
                    else
                        copy = item;

                    if (player.AddItem(copy))
                    {
                        Console.WriteLine($"  ✓ Найдена карта: {copy.itemName}");
                    }
                }
            }
        }
    }

    // =============================================
    // МИНИМАЛЬНЫЕ ВЕРСИИ МОНСТРОВ ДЛЯ ТЕСТА
    // =============================================
    public class CyclicMonster : Monster
    {
        private int currentAction = 0;

        public override MonsterAction GetNextAction()
        {
            currentAction = (currentAction + 1) % 3;

            switch (currentAction)
            {
                case 0: return new MonsterAttackAction(this);
                case 1: return new MonsterDefendAction(this);
                case 2: return new MonsterChargeAction(this);
                default: return new MonsterAttackAction(this);
            }
        }
    }

    public class RandomMonster : Monster
    {
        private Random random = new Random();

        public override MonsterAction GetNextAction()
        {
            int roll = random.Next(0, 100);

            if (roll < 50)
                return new MonsterAttackAction(this);
            else if (roll < 80)
                return new MonsterDefendAction(this);
            else
                return new MonsterSpecialAction(this, "использует ядовитое дыхание!");
        }
    }

    public class SmartMonster : Monster
    {
        private bool enraged = false;

        public override MonsterAction GetNextAction()
        {
            if (Health < MaxHealth * 0.3 && !enraged)
            {
                enraged = true;
                Damage *= 2;
                return new MonsterSpecialAction(this, "В ЯРОСТИ! Урон удвоен!");
            }

            return new MonsterAttackAction(this);
        }
    }
}