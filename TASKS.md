# Задачи для команды

## Как работать с этим файлом

- Каждый делает только свои задачи
- По готовности — кладёшь файлы в указанную папку и пишешь в Telegram/Discord
- Если срок сорвётся — предупреди заранее, чтобы перераспределить

---

## Владислав — звуки

### К дедлайну: среда

| ID | Что | Формат | Куда класть | Примечание |
|---|---|---|---|---|
| SND-01 | Звук автоатаки игрока | `.wav` или `.ogg` | `Assets/_Project/Audio/SFX/Attack_Player.wav` | Короткий, удар, не перегружать басами |
| SND-02 | Звук получения урона врагом | `.wav` или `.ogg` | `Assets/_Project/Audio/SFX/Hit_Enemy.wav` | Слой для всех врагов |
| SND-03 | Звук получения урона игроком | `.wav` или `.ogg` | `Assets/_Project/Audio/SFX/Hit_Player.wav` | Легкий хруст/всплеск |
| SND-04 | Звук шагов | `.wav` или `.ogg` | `Assets/_Project/Audio/SFX/Footstep.wav` | Короткий, можно зациклить |
| SND-05 | Звук нажатия кнопки UI | `.wav` или `.ogg` | `Assets/_Project/Audio/SFX/UI_Click.wav` | Приятный клик |
| SND-06 | Ambient для Hub | `.wav` или `.ogg` | `Assets/_Project/Audio/Music/Ambient_Hub.ogg` | Спокойный, тихий, loop |
| SND-07 | Ambient для уровней | `.wav` или `.ogg` | `Assets/_Project/Audio/Music/Ambient_Level.ogg` | Мрачный, loop |
| SND-08 | Звук смерти врага | `.wav` или `.ogg` | `Assets/_Project/Audio/SFX/Enemy_Death.wav` | Распад, исчезновение |
| SND-09 | Звук подбора награды | `.wav` или `.ogg` | `Assets/_Project/Audio/SFX/Reward.wav` | Золото + exp |
| SND-10 | Звук открытия диалога | `.wav` или `.ogg` | `Assets/_Project/Audio/SFX/Dialog_Open.wav` | Лёгкий, не навязчивый |

> Формат: лучше `.ogg` для музыки и `.wav` для SFX. Частота 44100 Hz, моно или стерео — не важно.

---

## Художник (спрайты) — уточни имя

### К дедлайну: среда

### Персонажи

| ID | Что | Размер | Анимации | Куда класть |
|---|---|---|---|---|
| ART-01 | Главный герой (игрок) | 64x64 px | Idle (4 кадра), Run (6-8 кадров), Attack (4 кадра), Hurt (2 кадра), Death (4 кадра) | `Assets/_Project/Art/Characters/Hero/` |
| ART-02 | Базовый враг (3-4 вида) | 64x64 px | Idle (2 кадра), Run (4 кадра), Attack (4 кадра), Death (4 кадра) | `Assets/_Project/Art/Characters/Enemies/` |
| ART-03 | NPC торговец | 64x64 px | Idle (2 кадра) | `Assets/_Project/Art/Characters/NPC/Merchant.png` |
| ART-04 | NPC на уровнях | 64x64 px | Idle (2 кадра) | `Assets/_Project/Art/Characters/NPC/LevelGuide.png` |
| ART-05 | Босс на уровне 5 | 96x96 px | Idle (4 кадра), Attack (6 кадров), Death (6 кадров) | `Assets/_Project/Art/Characters/Boss/` |

### Мир

| ID | Что | Размер | Куда класть |
|---|---|---|---|
| ART-06 | Тайлсет пола Hub (магазин) | 32x32 px | `Assets/_Project/Art/World/Tilesets/Hub_Floor.png` |
| ART-07 | Тайлсет стен / платформ для уровней | 32x32 px | `Assets/_Project/Art/World/Tilesets/Level_Walls.png` |
| ART-08 | Задник для Hub | 1920x1080 или 2048x1024 | `Assets/_Project/Art/World/Backgrounds/Hub_BG.png` |
| ART-09 | Задник для уровней (3-4 варианта) | 2048x1024 | `Assets/_Project/Art/World/Backgrounds/Level_BG_1.png` |
| ART-10 | Спрайт выхода / портала | 64x64 px | `Assets/_Project/Art/World/Portal.png` |
| ART-11 | Спрайт прилавка / контейнера | 128x64 px | `Assets/_Project/Art/World/ShopCounter.png` |

> PPU = 32 на всех спрайтах. Фон прозрачный для персонажей. Тайлсет — без прозрачности.

---

## Константин + Валера — левел-дизайн и диалоги

### К дедлайну: пятница

### Левел-дизайн

| ID | Что | Где | Примечание |
|---|---|---|---|
| LD-01 | Покрасить Hub тайлмап | Сцена `GameScene` | Заменить placeholder-зону Hub на нормальные тайлы |
| LD-02 | Покрасить 5 уровней | Сцена `GameScene` | Каждый уровень должен визуально отличаться |
| LD-03 | Расставить спрайты задников | Сцена `GameScene` | Слой Background, Sorting Layer = Background |
| LD-04 | Проверить коллизии уровней | Сцена `GameScene` | Убедиться что Player не проваливается |

### Диалоги

| ID | Что | Куда класть | Примечание |
|---|---|---|---|
| DIA-01 | Диалог торговца в Hub | `Assets/_Project/Dialogues/Merchant.json` | Приветствие, меню покупки, прощание |
| DIA-02 | Диалог NPC на уровне 1 | `Assets/_Project/Dialogues/NPC_Level1.json` | Подсказка где враги |
| DIA-03 | Диалог NPC на уровне 2 | `Assets/_Project/Dialogues/NPC_Level2.json` | Предупреждение о сильном враге |
| DIA-04 | Диалог NPC на уровне 3 | `Assets/_Project/Dialogues/NPC_Level3.json` | Лор/история мира |
| DIA-05 | Диалог NPC на уровне 4 | `Assets/_Project/Dialogues/NPC_Level4.json` | Подсказка про босса |
| DIA-06 | Диалог NPC на уровне 5 | `Assets/_Project/Dialogues/NPC_Level5.json` | Мотивация перед боссом |

Формат диалога:
```json
{
  "lines": [
    { "speaker": "NPC", "text": "Привет, путник!" },
    { "speaker": "Player", "text": "Что тут продаётся?" }
  ]
}
```

---

## Николай + Кирилл — программирование

### К дедлайну: среда-пятница

| ID | Что | Где | Статус |
|---|---|---|---|
| CODE-01 | Доработать движение Player | `PlayerMovement.cs` | В процессе |
| CODE-02 | Добавить коллизии мира | `World/LevelCollision.cs` или тайлмап | В плане |
| CODE-03 | Доработать камеру (кламп, зум) | `CameraController.cs` | В процессе |
| CODE-04 | Система автобоя | `AutoCombat.cs` | Готово базово |
| CODE-05 | Награды за убийство врагов | `EnemyBase.cs` + `GameManager.cs` | В плане |
| CODE-06 | Диалоговая система | `UI/DialogueSystem.cs` | В плане |
| CODE-07 | Переходы между уровнями | `World/ZoneTrigger.cs` | В плане |
| CODE-08 | Система спавна игрока на уровнях | `World/SpawnManager.cs` | В плане |

---

## Павел — арбитр

- Проверяет спорные моменты по дизайну
- Помогает с prioritization если кто-то не укладывается в срок

---

## Глоссарий

- **PPU** — pixels per unit, выставляется в Inspector при импорте спрайта (ставим 32)
- **SFX** — sound effects (короткие звуки)
- **Ambient** — фоновая музыка/атмосфера
- **Loop** — звук зациклен
- **Тайлсет** — картинка с маленькими квадратами для Tilemap
