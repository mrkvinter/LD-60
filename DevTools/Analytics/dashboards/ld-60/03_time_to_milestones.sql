-- name: Time to milestones
-- description: Minutes from the game start to the first time a session reaches each milestone: median and the
--   slowest 10%.
SELECT milestone,
       count() AS sessions,
       round(quantile(0.5)(t) / 60, 1) AS median_min,
       round(quantile(0.9)(t) / 60, 1) AS p90_min
FROM (
    SELECT properties.session_id AS session_id,
           if(event = 'ending_reached', concat('ending: ', toString(properties.id)), event) AS milestone,
           min(toFloat(properties.time_sec)) AS t
    FROM events
    WHERE {build} AND {filters}
      AND event IN ('rack_filled', 'building_built', 'upgrade_bought', 'ability_unlocked', 'note_read', 'ending_reached')
    GROUP BY session_id, milestone
)
GROUP BY milestone
ORDER BY median_min
