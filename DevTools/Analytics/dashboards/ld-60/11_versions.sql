-- name: Versions and platforms
-- description: Players and sessions per game version and platform, to tell jam updates apart.
SELECT toString(properties.game_version) AS version,
       toString(properties.platform) AS platform,
       count(DISTINCT distinct_id) AS players,
       count(DISTINCT properties.session_id) AS sessions,
       countIf(event = 'ending_reached' AND properties.id = 'real') AS real_endings
FROM events
WHERE {build} AND {filters}
GROUP BY version, platform
ORDER BY sessions DESC
