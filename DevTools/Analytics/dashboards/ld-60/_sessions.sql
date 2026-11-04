-- One row per session: how long it lasted and how far it got.
SELECT properties.session_id AS session_id,
       any(distinct_id) AS player,
       max(toFloat(properties.time_sec)) AS duration_sec,
       ifNull(maxIf(toFloat(properties.filled_racks), event IN ('rack_filled', 'session_ended')), 0) AS filled_racks,
       countIf(event = 'building_built') AS buildings,
       countIf(event = 'upgrade_bought') AS upgrades,
       countIf(event = 'ability_unlocked') AS abilities,
       countIf(event = 'note_read') AS notes,
       countIf(event = 'ending_reached' AND properties.id = 'fake') > 0 AS fake_ending,
       countIf(event = 'ending_reached' AND properties.id = 'real') > 0 AS real_ending
FROM events
WHERE {build} AND {filters} AND properties.session_id IS NOT NULL
GROUP BY session_id
