# Analytics — PostHog из командной строки

`posthog.py` выполняет HogQL-запросы к PostHog Cloud EU (организация и проект LD-60, id `311321`) и заливает
дашборды из папки `dashboards/`. Нужен только стандартный Python 3. Скрипт перенесён из Die to Play, отличается
только проектом (его можно сменить переменной `KVINTER_POSTHOG_PROJECT`).

События шлёт `Game.Scripts/Analytics/AnalyticsTracker.cs`: `game_started`, `rack_filled`, `building_built`,
`upgrade_bought`, `ability_unlocked`, `note_read`, `ending_reached` (`id`: `fake` / `real`), `session_ended`.
У каждого есть `time_sec` (секунд от старта игры), `session_id` (один запуск игры), `game_version`,
`platform` и `source` (`build` / `editor` / `test`).

## Ключ

Личный API-ключ PostHog (`phx_…`) **в репозитории не лежит**. Скрипт берёт его из переменной
`KVINTER_POSTHOG_KEY`, иначе из файла `~/.config/kvinter/posthog-key`.

Создать ключ: eu.posthog.com → аватар → Settings → Personal API keys → Create. Доступ — проект LD-60 (можно
вместе с Die to Play, тогда один ключ подходит обоим скриптам), права: **Project: read**, **Query: read**,
**Insight: write**, **Dashboard: write**. Ключ открывает все данные проекта, его не отправляют в чат.

Сохранить скопированный ключ на Windows (PowerShell):

```powershell
New-Item -ItemType Directory -Force "$HOME\.config\kvinter" | Out-Null; Get-Clipboard | Set-Content -NoNewline "$HOME\.config\kvinter\posthog-key"
```

## Команды

```
python DevTools/Analytics/posthog.py query "<hogql>"     запрос, результат как TSV
python DevTools/Analytics/posthog.py query <file.sql>    файл дашборда с подставленными плейсхолдерами
python DevTools/Analytics/posthog.py render <file.sql>   запрос в том виде, в каком уходит в PostHog
python DevTools/Analytics/posthog.py check [--source test]  прогнать все запросы дашбордов
python DevTools/Analytics/posthog.py sync                создать или обновить дашборды в PostHog
python DevTools/Analytics/posthog.py url                 ссылки на дашборды
```

## Дашборды как код

Одна папка — один дашборд, один `.sql`-файл — один график, как в Die to Play:

- шапка `-- name:`, `-- description:` и необязательная `-- chart: bar|line|area x=<колонка> y=<колонки>`;
- `{build}` — фильтр `properties.source = 'build'`, `{sessions}` — подзапрос из `_sessions.sql` (одна строка
  на сессию), `{filters}` заполняет PostHog датами и фильтрами дашборда;
- руками в PostHog графики не правят: следующий `sync` перезапишет. Порядок: поправить файл, `check`, `sync`.

Грабли HogQL те же, что в Die to Play: вместо `ARRAY JOIN` — `arrayJoin()`, числа из свойств приводить через
`toFloat()` / `toInt()`.
