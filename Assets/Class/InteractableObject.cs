using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// =============================================
// БАЗОВЫЙ ИНТЕРФЕЙС - всё, с чем можно взаимодействовать
// =============================================
public interface IInteractable
{
    void Interact(Player player);
    string GetInteractionPrompt(); // Текст при наведении (например "Открыть сундук")
}

// =============================================
// АБСТРАКТНЫЙ КЛАСС для объектов в комнатах
// Наследуйся от него, если хочешь создать новый объект (лаву, ловушку, алтарь)
// =============================================
public abstract class InteractableObject : MonoBehaviour, IInteractable
{
    [Header("Базовые настройки интерактивного объекта")]
    [SerializeField] protected string objectName = "Объект";
    [SerializeField] protected Sprite objectIcon; // Для UI

    public abstract void Interact(Player player);
    public abstract string GetInteractionPrompt();
}