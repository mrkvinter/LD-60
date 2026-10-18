# Страница itch.io — The Last Overtime

- `itch-page.css`: вставить в **Edit theme → Custom CSS**.
- `description.html`: описание игры. В редакторе описания переключись в режим HTML и вставь целиком. Текст там черновой, перепиши под себя.
- `screenshots/`: скриншоты в PNG для загрузки на itch.
- `preview/index.html`: локальный превью. Открывается через сервер `itch-page-preview` (`.claude/launch.json`) по адресу http://localhost:8765/preview/. Кнопки в правом нижнем углу переключают вид «скачиваемая игра» / «веб-билд».

## Theme panel

| Поле | Значение |
|---|---|
| BG | `#0d0b08` |
| BG 2 | `#16120c` |
| Text | `#f2ebd9` |
| Link | `#2ff3ff` |
| More options → кнопки | `#ffd21a`, текст кнопок `#0d0b08`, рамки `#45381f` |
| Font | Lato (шрифты подменяет CSS) |
| Size | Large |
| Screenshots | Auto |
| Background | не нужен: адрес текстуры стены (загружена через описание, там она скрыта классом `custom-hidden`) прописан прямо в CSS. Тайлинг, масштаб x2 и затемнение делает CSS |
| Banner | не загружать: заголовок рисует CSS поверх стены |

## Как устроено описание

- `<blockquote>` превращается в записку, как в руке в игре (кнопка «Quote» в визуальном редакторе делает то же самое).
- `<blockquote class="custom-card">`: карточка управления, текст прижат вправо.
- `<h2>`: заголовок раздела с полоской «hazard».
- `<hr>`: разделитель в виде сигнальной ленты.
- `<code>LMB</code>`: клавиша.
- `.custom-lead`, `.custom-shift`, `.custom-blink`, `.custom-stamp`, `.custom-center`, `.custom-note-title`, `.custom-note-sub`, `.custom-note-sign`: мелкие стили.

itch пропускает только классы, которые начинаются с `custom-`.
Если `@import` шрифтов вдруг не сработает, страница откатится на запасные шрифты. Тогда выбери шрифт в панели темы.
