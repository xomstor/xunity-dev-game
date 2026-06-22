# xunity-dev-game

2D Android RPG — сайдскроллер с развитием персонажа и исследованием мира.

---

## Команда

| Роль | Кто |
|---|---|
| Программирование / геймдизайн | Николай, Кирилл |
| Звуки | Владислав |
| Диалоги + левел-дизайн | Константин, Валера |
| Арбитр при спорах | Павел |

---

## Что нужно установить

1. **Unity Hub** — [download](https://unity.com/download)
2. В Unity Hub → вкладка **Installs** → установи **Unity 6.4** (та же версия что у всех!)
3. При установке Unity добавь модуль **Android Build Support** (галочка в установщике)
4. **Git** — [download](https://git-scm.com/downloads) (если не установлен)
5. **GitHub Desktop** (опционально, если не хочешь терминал) — [download](https://desktop.github.com)

---

## Клонировать проект

```bash
git clone https://github.com/xomstor/xunity-dev-game.git
```

Затем в **Unity Hub** → **Projects** → **Add** → **Add project from disk** → выбери папку `xunity-dev-game`.

> ⚠️ Не создавай новый Unity проект. Открывай только через Add from disk.

---

## Настройка Unity (один раз после открытия)

**Edit → Project Settings → Editor:**
- `Version Control Mode` → **Visible Meta Files**
- `Asset Serialization Mode` → **Force Text**

**Edit → Project Settings → Player** (иконка Android):
- `Scripting Backend` → **IL2CPP**
- `Target Architectures` → ✅ **ARM64**

---

## Структура папок

```
Assets/
└── _Project/
    ├── Scripts/
    │   ├── Character/   ← скрипты персонажа
    │   ├── Combat/      ← бой, враги
    │   ├── World/       ← GameManager, SceneLoader, камера
    │   └── UI/          ← HUD, меню
    ├── Scenes/          ← все сцены
    ├── Audio/           ← звуки (Владислав сюда)
    ├── Prefabs/         ← префабы
    └── Art/             ← спрайты, тайлсеты
```

> Свои файлы клади строго в нужную папку. Не кидай ничего прямо в `Assets/` корень.

---

## Как работать с Git

### Получить последние изменения (делай КАЖДЫЙ раз перед работой)

```bash
git pull
```

### Отправить свои изменения

```bash
git add .
git commit -m "что ты сделал, кратко"
git push
```

### Пример нормального коммита

```bash
git commit -m "feat: добавил звук атаки игрока"
git commit -m "fix: исправил баг с камерой"
git commit -m "level: добавил тайлмап для уровня 1"
```

### Если при push выдаёт ошибку — сначала pull

```bash
git pull
# реши конфликты если есть
git push
```

---

## Правила

- **Не пушьломаный проект** — проверь что Unity не выдаёт ошибки перед push
- **Не удаляй чужие файлы** без договорённости
- **Не коммить папку Library/** — она в .gitignore, это нормально
- Все в одной версии Unity — **6.4**, не обновляй самовольно

---

## Основные скрипты (каркас)

| Скрипт | Где | Что делает |
|---|---|---|
| `GameManager.cs` | World/ | Хранит золото, опыт, уровень игрока |
| `SceneLoader.cs` | World/ | Переходы между сценами |
| `CameraController.cs` | World/ | Пан и зум камеры (мышь + тач) |
| `PlayerController.cs` | Character/ | HP игрока, смерть |
| `AutoCombat.cs` | Combat/ | Автоатака врагов в радиусе |
| `EnemyBase.cs` | Combat/ | Базовый враг, дропает exp и золото |
| `HUDController.cs` | UI/ | HP-бар, exp-бар, золото, кнопка атаки |
