# xunity-dev-game
2d android rpg

Клонировать и инициализировать Unity проект
bash
git clone https://github.com/xomstor/xunity-dev-game.git
cd xunity-game

Настройка Unity для Git
В Unity: Edit → Project Settings → Editor:
Version Control Mode → Visible Meta Files
Asset Serialization Mode → Force Text

Рекомендуемая структура папок в Unity
Assets/
├── _Project/
│   ├── Scripts/
│   │   ├── Character/
│   │   ├── Combat/
│   │   ├── World/
│   │   └── UI/
│   ├── Scenes/
│   ├── Audio/
│   ├── Prefabs/
│   └── Art/
