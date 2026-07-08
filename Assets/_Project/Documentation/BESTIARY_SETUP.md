# Настройка Бестиария и Иконок

## Как добавить иконки для кнопок

### 1. Иконка кнопки "Статистика"
- В Inspector на **PauseMenu** найди поле **"Statistics Button Icon"**
- Перетащи PNG иконку в это поле
- Если иконка не назначена, будет использоваться оранжевый градиентный спрайт с текстом

### 2. Иконка кнопки "Бестиарий"
- В Inspector на **PauseMenu** найди поле **"Bestiary Button Icon"**
- Перетащи PNG иконку в это поле
- Если иконка не назначена, будет использоваться синий градиентный спрайт с текстом

## Как добавить мобов в бестиарий с иконками

### Шаг 1: Создать ScriptableObject для моба

1. В Project окне перейди в папку `Assets/_Project/Data/Bestiary/` (создай если нет)
2. Правый клик → Create → Bestiary → Enemy Entry
3. Назови файл (например, `Goblin_Entry`)

### Шаг 2: Заполнить данные моба

В Inspector заполни поля:
- **Enemy Id**: уникальный ID (например, `goblin_01`)
- **Enemy Name**: имя моба (например, `Гоблин`)
- **Description**: описание (например, `Маленький враг, быстро атакует`)
- **Enemy Icon**: перетащи PNG иконку моба (рекомендуемый размер 128x128 или 256x256)
- **Base Level**: базовый уровень
- **Base Health**: базовое здоровье
- **Base Damage**: базовый урон

### Шаг 3: Использовать в коде

Когда моб умирает, в `AutoCombat.Die()` вызывается:
```csharp
Bestiary.RegisterKill(name);
```

Имя моба должно совпадать с **Enemy Name** в BestiaryData.

## Структура папок

```
Assets/_Project/
├── Data/
│   └── Bestiary/
│       ├── Goblin_Entry.asset
│       ├── Orc_Entry.asset
│       └── Dragon_Entry.asset
├── Sprites/
│   └── Enemies/
│       ├── goblin_icon.png
│       ├── orc_icon.png
│       └── dragon_icon.png
└── Scripts/
    └── UI/
        ├── BestiaryEntry.cs
        ├── BestiaryData.cs
        └── PauseMenu.cs
```

## Рекомендации по иконкам

- **Формат**: PNG с прозрачностью
- **Размер**: 128x128 или 256x256 пикселей
- **Стиль**: иконки должны быть узнаваемы и контрастны
- **Цвет фона**: прозрачный (Alpha = 0)

## Как отобразить бестиарий в меню

Кнопка "Бестиарий" уже добавлена в меню паузы. При нажатии открывается панель с записями убитых мобов.

Каждая запись показывает:
- Иконку моба
- Имя моба
- Количество убитых
- Описание (если заполнено)

## Префаб BestiaryEntry

Скрипт `BestiaryEntry.cs` используется для отображения одной записи в бестиарии.

Поля:
- `enemyIcon` (Image) — иконка моба
- `enemyName` (TextMeshProUGUI) — имя моба
- `killCount` (TextMeshProUGUI) — количество убитых
- `description` (TextMeshProUGUI) — описание

Метод:
```csharp
SetData(string name, int kills, Sprite icon, string desc = "")
```

Пример использования:
```csharp
BestiaryEntry entry = Instantiate(bestiaryEntryPrefab);
entry.SetData("Гоблин", 5, goblinIcon, "Маленький враг");
```
