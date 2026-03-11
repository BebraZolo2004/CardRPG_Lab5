using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

// =============================================
// ГЛАВНЫЙ МЕНЕДЖЕР ИГРЫ
// Отвечает за:
// - Инициализацию игрока
// - Управление комнатами и переходами
// - Глобальные пулы карт и монстров
// - Сохранение прогресса
// =============================================
public class GameManager : MonoBehaviour
{
    // ========== СИНГЛТОН ==========
    private static GameManager _instance;
    public static GameManager Instance
    {
        get
        {
            if (_instance == null)
            {
                Debug.LogError("GameManager не найден в сцене! Создаю...");
                GameObject go = new GameObject("GameManager");
                _instance = go.AddComponent<GameManager>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    // ========== СОБЫТИЯ ДЛЯ UI ==========
    // На эти события может подписаться фронтенд-разработчик
    public System.Action<Player> OnPlayerCreated;
    public System.Action<Room> OnRoomEntered;
    public System.Action<Monster> OnCombatStarted;
    public System.Action<Monster> OnCombatEnded;
    public System.Action<string> OnGameMessage;
    public System.Action<GameState> OnGameStateChanged;

    // ========== СОСТОЯНИЕ ИГРЫ ==========
    [Header("Текущее состояние")]
    [SerializeField] private GameState currentState = GameState.Menu;
    public GameState CurrentState => currentState;

    [Header("=== ИГРОК ===")]
    [Tooltip("Стартовые характеристики игрока")]
    [SerializeField] private int startHealth = 30;
    [SerializeField] private int startInventorySize = 5;

    private Player _currentPlayer;
    public Player CurrentPlayer
    {
        get { return _currentPlayer; }
        private set
        {
            _currentPlayer = value;
            OnPlayerCreated?.Invoke(_currentPlayer);
        }
    }

    // ========== ГЛОБАЛЬНЫЕ ПУЛЫ ==========
    [Header("=== ГЛОБАЛЬНЫЙ ПУЛ КАРТ ===")]
    [Tooltip("ПЕРЕТАЩИ СЮДА ВСЕ КАРТЫ ИЗ ПАПКИ Resources/Cards")]
    //[SerializeField] public List<Item> allCards = new List<Item>();
    [SerializeField] private AssetGenerator assetGenerator; //добавляем генератор карт в гейм менеджер

    [Header("=== ГЛОБАЛЬНЫЙ ПУЛ МОНСТРОВ ===")]
    [Tooltip("ПЕРЕТАЩИ СЮДА ВСЕХ МОНСТРОВ (префабы) ИЗ ПАПКИ Resources/Monsters")]
    [SerializeField] public List<Monster> allMonsters = new List<Monster>();

    // ========== КОМНАТЫ ==========
    [Header("=== КОМНАТЫ ===")]
    [Tooltip("Все комнаты в подземелье")]
    [SerializeField] public List<Room> allRooms = new List<Room>();

    private Room _currentRoom;
    public Room CurrentRoom => _currentRoom;

    // Словарь: какая комната -> какой монстр там живет
    private Dictionary<Room, Monster> roomMonsters = new Dictionary<Room, Monster>();

    // Словарь: какая комната -> какие предметы в сундуке
    private Dictionary<Room, List<Item>> roomChestItems = new Dictionary<Room, List<Item>>();

    // ========== НАСТРОЙКИ ГЕНЕРАЦИИ ==========
    [Header("=== НАСТРОЙКИ ПОДЗЕМЕЛЬЯ ===")]
    [Tooltip("Шанс появления монстра в комнате (0-1)")]
    [Range(0, 1)]
    [SerializeField] private float monsterSpawnChance = 0.7f;

    [Tooltip("Минимальное количество карт в сундуке")]
    [SerializeField] private int minChestItems = 1;

    [Tooltip("Максимальное количество карт в сундуке")]
    [SerializeField] private int maxChestItems = 3;

    [Tooltip("Всегда ли есть ключ в сундуке у босса")]
    [SerializeField] private bool bossChestAlwaysHasKey = true;

    // ========== СИСТЕМА СОХРАНЕНИЯ ==========
    private bool isLoadingSave = false;

    // ========== МЕТОДЫ ЖИЗНЕННОГО ЦИКЛА ==========

    private void Awake()
    {
        // Реализация синглтона
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        Debug.Log("✅ GameManager инициализирован");
    }

    private void Start()
    {
        // Проверяем, загружены ли карты и монстры
        ValidatePools();
    }

    // ========== ИНИЦИАЛИЗАЦИЯ ==========

    /// <summary>
    /// НОВАЯ ИГРА - создаем игрока и генерируем подземелье
    /// </summary>
    public void StartNewGame()
    {
        Debug.Log("🎮 Начинаем новую игру!");
        OnGameMessage?.Invoke("Новое приключение начинается...");

        // Создаем игрока
        CurrentPlayer = new Player(startHealth, startInventorySize);

        // Подписываемся на события игрока
        CurrentPlayer.OnPlayerMessage += (msg) => OnGameMessage?.Invoke(msg);
        CurrentPlayer.OnHealthChanged += (current, max) =>
            Debug.Log($"❤️ Здоровье: {current}/{max}");

        // ======== НОВОЕ: выдаём стартовые карты ========
        var startCards = GameManager.Instance.GetRandomCards(startInventorySize);

        foreach (var card in startCards)
        {
            CurrentPlayer.AddItem(card);
            Debug.Log($"🎴 Игрок получил стартовую карту: {card.itemName}");
        }
        // =============================================


        // Генерируем подземелье
        GenerateDungeon();

        // Переходим в первую комнату
        ChangeState(GameState.Exploration);
        EnterRoom(0);
    }

    /// <summary>
    /// ПРОВЕРКА ПУЛОВ - выводит информацию о загруженных ресурсах
    /// </summary>
    private void ValidatePools()
    {
        Debug.Log("===================================");
        Debug.Log("📊 ПРОВЕРКА РЕСУРСОВ:");
        Debug.Log($"📦 Карт загружено: {allCards.Count}");

        //if (allCards.Count == 0)
        //{
        //    Debug.LogWarning("⚠ Нет карт! Загрузи карты в allCards в инспекторе!");
        //    // Пытаемся загрузить из Resources
        //    LoadCardsFromResources();

        //}
        //else
        //{
        //    foreach (var card in allCards)
        //    {
        //        Debug.Log($"   - {card.itemName}");
        //    }
        //} на всякий оставлю, что бы иметь возможность сбросить 

        if (assetGenerator != null)
        {
            // Используем assetGenerator
            var cards = assetGenerator.generatedCards;
            Debug.Log($"Количество карт: {cards.Count}");
        }
        else
        {
            Debug.LogError("AssetGenerator не назначен в Inspector!");
        }

        Debug.Log($"👾 Монстров загружено: {allMonsters.Count}");
        if (allMonsters.Count == 0)
        {
            Debug.LogWarning("⚠ Нет монстров! Загрузи монстров в allMonsters в инспекторе!");
            LoadMonstersFromResources();
        }
        else
        {
            foreach (var monster in allMonsters)
            {
                Debug.Log($"   - {monster.monsterName}");
            }
        }
        Debug.Log("===================================");
    }

    /// <summary>
    /// Загрузка карт из папки Resources (на случай, если забыли добавить в инспектор)
    /// </summary>
    //private void LoadCardsFromResources()
    //{
    //    Debug.Log("Пытаюсь загрузить карты из Resources/Cards...");
    //    // TODO: фронтендщик должен положить карты в папку Resources
    //}

    /// <summary>
    /// Загрузка монстров из папки Resources
    /// </summary>
    private void LoadMonstersFromResources()
    {
        Debug.Log("Пытаюсь загрузить монстров из Resources/Monsters...");
        // TODO: фронтендщик должен положить префабы монстров в папку Resources
    }

    // ========== ГЕНЕРАЦИЯ ПОДЗЕМЕЛЬЯ ==========

    /// <summary>
    /// Генерирует подземелье: распределяет монстров и предметы по комнатам
    /// </summary>
    public void GenerateDungeon()
    {
        Debug.Log("🏗 Генерация подземелья...");

        roomMonsters.Clear();
        roomChestItems.Clear();

        if (allRooms.Count == 0)
        {
            Debug.LogError("❌ Нет комнат! Добавь комнаты в allRooms!");
            return;
        }

        // Для каждой комнаты генерируем содержимое
        for (int i = 0; i < allRooms.Count; i++)
        {
            Room room = allRooms[i];

            // 1. Генерируем монстра (с вероятностью monsterSpawnChance)
            if (UnityEngine.Random.value < monsterSpawnChance && allMonsters.Count > 0)
            {
                Monster monsterPrefab = allMonsters[UnityEngine.Random.Range(0, allMonsters.Count)];
                // Важно: создаем ЭКЗЕМПЛЯР монстра, чтобы не изменять оригинал
                Monster monsterInstance = InstantiateMonster(monsterPrefab);
                roomMonsters[room] = monsterInstance;

                Debug.Log($"   Комната {i + 1}: монстр {monsterInstance.monsterName}");
            }
            else
            {
                Debug.Log($"   Комната {i + 1}: без монстра");
            }

            // 2. Генерируем предметы для сундука
            List<Item> chestItems = new List<Item>();
            int itemCount = UnityEngine.Random.Range(minChestItems, maxChestItems + 1);

            for (int j = 0; j < itemCount; j++)
            {
                if (allCards.Count > 0)
                {
                    //меняю на assetGenerator
                    Item cardPrefab = assetGenerator[UnityEngine.Random.Range(0, assetGenerator.generatedcard.Count)];
                    // Создаем копию карты
                    Item cardCopy = CreateCardCopy(cardPrefab);
                    chestItems.Add(cardCopy);
                }
            }

            // Для последней комнаты (босса) добавляем ключ
            if (i == allRooms.Count - 1 && bossChestAlwaysHasKey)
            {
                chestItems.Add(new Key());
                Debug.Log($"   Комната {i + 1}: БОСС! В сундуке будет ключ");
            }

            roomChestItems[room] = chestItems;
        }

        Debug.Log($"✅ Подземелье сгенерировано! Комнат: {allRooms.Count}");
    }

    /// <summary>
    /// Создает копию монстра
    /// </summary>
    private Monster InstantiateMonster(Monster original)
    {
        // Здесь должен быть код создания через Instantiate в Unity
        // Для теста просто возвращаем оригинал
        return original;
    }

    /// <summary>
    /// Создает копию карты
    /// </summary>
    private Item CreateCardCopy(Item original)
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

        Debug.LogError($"❌ Неизвестный тип карты: {original.GetType()}");
        return null;
    }

    // ========== УПРАВЛЕНИЕ КОМНАТАМИ ==========

    /// <summary>
    /// Вход в комнату по индексу
    /// </summary>
    public void EnterRoom(int roomIndex)
    {
        if (roomIndex < 0 || roomIndex >= allRooms.Count)
        {
            Debug.LogError($"❌ Неверный индекс комнаты: {roomIndex}");
            return;
        }

        _currentRoom = allRooms[roomIndex];
        Debug.Log($"🚪 Вход в комнату {roomIndex + 1}: {_currentRoom.roomName}");
        OnRoomEntered?.Invoke(_currentRoom);

        // Проверяем, есть ли в комнате монстр
        if (roomMonsters.ContainsKey(_currentRoom))
        {
            Monster monster = roomMonsters[_currentRoom];
            Debug.Log($"⚠ В комнате МОНСТР: {monster.monsterName}!");
            StartCombat(monster);
        }
        else
        {
            Debug.Log("📦 В комнате сундук с сокровищами!");
            OnGameMessage?.Invoke("Комната безопасна. Можно открыть сундук.");
        }
    }

    /// <summary>
    /// Получить сундук для текущей комнаты
    /// </summary>
    public Chest GetChestForCurrentRoom()
    {
        if (_currentRoom == null) return null;

        // Создаем сундук с предметами для этой комнаты
        GameObject chestGO = new GameObject("RoomChest");
        Chest chest = chestGO.AddComponent<Chest>();

        if (roomChestItems.ContainsKey(_currentRoom))
        {
            chest.possibleItems = roomChestItems[_currentRoom];
        }

        return chest;
    }

    // ========== БОЕВАЯ СИСТЕМА ==========

    /// <summary>
    /// Начинает бой с монстром
    /// </summary>
    public void StartCombat(Monster monster)
    {
        Debug.Log($"⚔ НАЧАЛО БОЯ с {monster.monsterName}!");
        ChangeState(GameState.Combat);

        OnCombatStarted?.Invoke(monster);

        // Подписываемся на смерть монстра
        monster.OnMonsterDeath += () => EndCombat(monster);
    }

    /// <summary>
    /// Завершает бой (победа или поражение)
    /// </summary>
    private void EndCombat(Monster monster)
    {
        Debug.Log($"🏆 БОЙ ЗАВЕРШЕН! {monster.monsterName} повержен!");
        OnCombatEnded?.Invoke(monster);

        // Убираем монстра из комнаты
        if (roomMonsters.ContainsKey(_currentRoom))
        {
            roomMonsters.Remove(_currentRoom);
        }

        ChangeState(GameState.Exploration);

        // Даем награду
        SpawnReward(monster);
    }

    /// <summary>
    /// Создает награду после победы
    /// </summary>
    private void SpawnReward(Monster monster)
    {
        Debug.Log($"📦 {monster.monsterName} оставил сундук!");

        // Создаем сундук с наградой
        Chest rewardChest = GetChestForCurrentRoom();
        if (rewardChest != null)
        {
            // Добавляем ключ, если это был босс
            if (IsBossRoom(_currentRoom))
            {
                rewardChest.hasKey = true;
                Debug.Log("🔑 В сундуке блестит КЛЮЧ!");
            }

            // Автоматически открываем сундук (для теста)
            // В реальной игре игрок подойдет сам
            rewardChest.Interact(CurrentPlayer);
        }
    }

    /// <summary>
    /// Проверка, является ли комната босс-комнатой
    /// </summary>
    private bool IsBossRoom(Room room)
    {
        return allRooms.IndexOf(room) == allRooms.Count - 1;
    }

    // ========== УПРАВЛЕНИЕ СОСТОЯНИЕМ ==========

    /// <summary>
    /// Изменяет состояние игры
    /// </summary>
    public void ChangeState(GameState newState)
    {
        if (currentState == newState) return;

        Debug.Log($"🔄 Состояние: {currentState} -> {newState}");
        currentState = newState;
        OnGameStateChanged?.Invoke(newState);
    }

    // ========== СИСТЕМА СОХРАНЕНИЯ ==========

    /// <summary>
    /// Сохраняет игру
    /// </summary>
    public void SaveGame()
    {
        Debug.Log("💾 Сохранение игры...");

        // TODO: Реализовать сериализацию
        // Сохраняем:
        // - состояние игрока (HP, инвентарь)
        // - текущую комнату
        // - какие монстры еще живы

        OnGameMessage?.Invoke("Игра сохранена");
    }

    /// <summary>
    /// Загружает игру
    /// </summary>
    public void LoadGame()
    {
        Debug.Log("📂 Загрузка игры...");
        isLoadingSave = true;

        // TODO: Загрузить сохранение

        isLoadingSave = false;
        OnGameMessage?.Invoke("Игра загружена");
    }

    // ========== ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ ==========

    /// <summary>
    /// Получить случайную карту из глобального пула
    /// </summary>
    public Item GetRandomCard()
    {
        if (assetGenerator.generatedCard.Count == 0) return null;
        return allCards[UnityEngine.Random.Range(0, assetGenerator.generatedCard.Count)];
    }

    /// <summary>
    /// Получить несколько случайных карт
    /// </summary>
    public List<Item> GetRandomCards(int count)
    {
        List<Item> result = new List<Item>();
        for (int i = 0; i < count; i++)
        {
            result.Add(GetRandomCard());
        }
        return result;
    }

    /// <summary>
    /// Получить случайного монстра
    /// </summary>
    public Monster GetRandomMonster()
    {
        if (allMonsters.Count == 0) return null;
        return allMonsters[UnityEngine.Random.Range(0, allMonsters.Count)];
    }
}

// =============================================
// КЛАСС КОМНАТЫ
// =============================================
[System.Serializable]
public class Room
{
    [Header("Основная информация")]
    public string roomName = "Новая комната";

    [Header("Визуал")]
    public GameObject roomPrefab;      // Префаб комнаты (для фронтенда)
    public Sprite roomIcon;            // Иконка для карты

    [Header("Точки входа/выхода")]
    public Transform playerSpawnPoint;  // Где появляется игрок
    public List<Door> doors;            // Двери из этой комнаты

    [Header("Контент")]
    public bool hasChest = true;        // Есть ли сундук

    public override string ToString()
    {
        return roomName;
    }
}

// =============================================
// СОСТОЯНИЯ ИГРЫ
// =============================================
public enum GameState
{
    Menu,           // Главное меню
    Exploration,    // Исследование подземелья
    Combat,         // Бой
    Inventory,      // Просмотр инвентаря
    Paused,         // Пауза
    GameOver,       // Поражение
    Victory         // Победа
}