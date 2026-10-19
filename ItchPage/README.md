# Страница itch.io — The Last Overtime

Страница: https://mrkvinter.itch.io/the-last-overtime

- `itch-page.min.css`: вставить в **Edit theme → Custom CSS**. itch сохраняет только первые 16 384 символа, поэтому вставляется сжатая версия.
- `itch-page.css`: читаемый исходник. После правок пересобери сжатую версию: `python minify.py` (скрипт выведет длину и предупредит, если не влезает).
- `description.html`: описание игры. В редакторе описания переключись в режим HTML и вставь целиком.
- `screenshots/`: скриншоты в PNG для загрузки на itch. В `screenshots/small/` копии 480×270 для описания на ldjam.com (там картинки растягиваются до ширины колонки, если они больше).
- `ldjam.txt`: описание для страницы на ldjam.com (Markdown, открывай как текст и копируй как есть).
- `cover/cover.png`: временная обложка 1260×1000 (собрана из `cover/cover.html`).
- `preview/index.html`: локальный превью. Открывается через сервер `itch-page-preview` (`.claude/launch.json`) по адресу http://localhost:8765/preview/. Кнопки в правом нижнем углу переключают вид «скачиваемая игра» / «веб-билд».

## Theme panel

| Поле | Значение |
|---|---|
| BG | `#0d0b08` |
| BG 2 | `#16120c` |
| Text | `#f2ebd9` |
| Link | `#2ff3ff` |
| More options → кнопки | `#ffd21a`, текст кнопок `#0d0b08`, рамки `#45381f` |
| Font | Barlow |
| More options → Header font | Saira Stencil One |
| Size | Large |
| Screenshots | Auto |
| Background | не нужен: адрес текстуры стены (img.itch.zone) прописан прямо в CSS |
| Banner | не загружать: заголовок рисует CSS поверх стены |

## Как устроено описание

- `<blockquote>`: записка, как в руке в игре. itch переписывает её содержимое в строки через `<br>` и выкидывает `<p>` вместе с классами. Поэтому части записки размечены так:
  - `<span class="custom-note-title">` — заголовок по центру;
  - `<strong>` — строка красной ручкой;
  - `<span class="custom-note-sign">` — подпись справа.
  Если itch вырежет и их, первая строка всё равно станет заголовком.
- `<blockquote class="custom-card">`: карточка управления, текст прижат вправо.
- `<h2>`: заголовок раздела с полоской «hazard».
- `<hr>`: разделитель в виде сигнальной ленты.
- `<code>LMB</code>`: клавиша.
- `<li class="custom-warn">`: пункт списка красным.
- `.custom-lead`, `.custom-shift`, `.custom-blink`, `.custom-stamp`, `.custom-center`: мелкие стили для абзацев вне записок.

itch пропускает только классы, которые начинаются с `custom-`.
Шрифты (Saira Stencil One, Barlow Condensed, Barlow, Caveat) грузятся через `@import` в начале CSS — на itch он работает. Выбор в панели — запасной вариант.
На страницах с веб-билдом itch прячет название в шапке (`.embedded_game .header .game_title`), CSS возвращает его.
