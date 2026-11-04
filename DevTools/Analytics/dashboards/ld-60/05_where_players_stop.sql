-- name: Where players stop
-- description: Sessions that reached no ending, by how many racks they had filled when they left.
-- chart: bar x=racks y=sessions
SELECT toInt(filled_racks) AS racks, count() AS sessions
FROM ({sessions})
WHERE NOT fake_ending AND NOT real_ending
GROUP BY racks
ORDER BY racks
