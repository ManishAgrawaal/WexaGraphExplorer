// 1. Search people by name, role, skill or company.
// Parameter: $term
MATCH (p:Person)-[:HAS_SKILL]->(s:Skill)
OPTIONAL MATCH (p)-[:WORKED_AT]->(c:Company)
WHERE toLower(p.name) CONTAINS toLower($term)
   OR toLower(p.role) CONTAINS toLower($term)
   OR toLower(s.name) CONTAINS toLower($term)
   OR toLower(c.name) CONTAINS toLower($term)
RETURN p.id AS id, p.name AS name, p.role AS role,
       p.location AS location, coalesce(c.name,'Independent') AS company,
       collect(DISTINCT s.name) AS skills
ORDER BY p.name
LIMIT 30;

// 2. Multi-hop recommendation.
// Parameter: $personId
MATCH (me:Person {id: $personId})-[:HAS_SKILL]->(shared:Skill)<-[:HAS_SKILL]-(other:Person)
WHERE me <> other
WITH me, other, collect(DISTINCT shared.name) AS sharedSkills
OPTIONAL MATCH (me)-[:CONTRIBUTED_TO]->(project:Project)<-[:CONTRIBUTED_TO]-(other)
WITH other, sharedSkills, collect(DISTINCT project.name) AS sharedProjects
OPTIONAL MATCH (other)-[:WORKED_AT]->(company:Company)
RETURN other.id AS id, other.name AS name, other.role AS role,
       coalesce(company.name,'Independent') AS company,
       sharedSkills,
       [x IN sharedProjects WHERE x IS NOT NULL] AS sharedProjects,
       size(sharedSkills)*2 + size([x IN sharedProjects WHERE x IS NOT NULL]) AS connectionScore
ORDER BY connectionScore DESC, other.name
LIMIT 12;

// 3. Graph statistics.
MATCH (n)
WITH labels(n)[0] AS label, count(n) AS count
RETURN label, count;
