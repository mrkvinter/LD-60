-- name: Session length
-- description: Sessions by how many whole minutes passed between the game start and their last event, in
--   10-minute blocks; sessions shorter than a minute get their own bar.
-- chart: bar x=minutes y=sessions
SELECT multiIf(m < 1, '<1',
               m < 10, '1–9',
               m < 60, concat(toString(intDiv(m, 10) * 10), '–', toString(intDiv(m, 10) * 10 + 9)),
               m < 120, '60–119',
               '120+') AS minutes,
       count() AS sessions
FROM (SELECT toInt(floor(duration_sec / 60)) AS m FROM ({sessions}))
GROUP BY minutes
ORDER BY min(m)
