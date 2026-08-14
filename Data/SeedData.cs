using Neo4j.Driver;

namespace GraphExplorer.Data;

public static class SeedData
{
    public static async Task RunAsync(string uri, string username, string password)
    {
        await using var driver = GraphDatabase.Driver(uri, AuthTokens.Basic(username, password));
        await driver.VerifyConnectivityAsync();

        await using var session = driver.AsyncSession();

        await session.RunAsync("""
            CREATE CONSTRAINT person_id IF NOT EXISTS
            FOR (p:Person) REQUIRE p.id IS UNIQUE
            """);

        await session.RunAsync("""
            CREATE CONSTRAINT skill_name IF NOT EXISTS
            FOR (s:Skill) REQUIRE s.name IS UNIQUE
            """);

        await session.RunAsync("""
            CREATE CONSTRAINT company_name IF NOT EXISTS
            FOR (c:Company) REQUIRE c.name IS UNIQUE
            """);

        await session.RunAsync("""
            CREATE CONSTRAINT project_name IF NOT EXISTS
            FOR (p:Project) REQUIRE p.name IS UNIQUE
            """);

        var people = new[]
        {
            new { id="P001", name="Aarav Mehta", role="Backend Engineer", location="Bengaluru", company="FinEdge", skills=new[]{"C#",".NET","Azure","SQL","Kafka"}, projects=new[]{"Payments Modernization","Risk Platform"} },
            new { id="P002", name="Isha Sharma", role="Full Stack Engineer", location="Pune", company="FinEdge", skills=new[]{"C#",".NET","Angular","Azure","SQL"}, projects=new[]{"Payments Modernization","Customer Portal"} },
            new { id="P003", name="Rohan Kapoor", role="Data Engineer", location="Mumbai", company="DataBridge", skills=new[]{"Python","SQL","Kafka","Azure","ETL"}, projects=new[]{"Risk Platform","Fraud Analytics"} },
            new { id="P004", name="Neha Verma", role="Cloud Engineer", location="Hyderabad", company="CloudWorks", skills=new[]{"Azure","Docker","Kubernetes","Terraform","Kafka"}, projects=new[]{"Risk Platform","Cloud Migration"} },
            new { id="P005", name="Vikram Rao", role="Frontend Engineer", location="Chennai", company="FinEdge", skills=new[]{"Angular","TypeScript","HTML","CSS","Azure"}, projects=new[]{"Customer Portal","Payments Modernization"} },
            new { id="P006", name="Ananya Singh", role="Security Engineer", location="Delhi", company="SecureBank", skills=new[]{"OAuth2","JWT","C#",".NET","Azure"}, projects=new[]{"Identity Platform","Payments Modernization"} },
            new { id="P007", name="Kabir Jain", role="Platform Engineer", location="Bengaluru", company="CloudWorks", skills=new[]{"Docker","Kubernetes","Azure","Terraform","CI/CD"}, projects=new[]{"Cloud Migration","Identity Platform"} },
            new { id="P008", name="Meera Nair", role="Product Engineer", location="Kochi", company="DataBridge", skills=new[]{"C#",".NET","SQL","Angular","Kafka"}, projects=new[]{"Customer Portal","Fraud Analytics"} },
            new { id="P009", name="Arjun Malhotra", role="Solution Architect", location="Noida", company="SecureBank", skills=new[]{"C#",".NET","Azure","Kafka","OAuth2"}, projects=new[]{"Identity Platform","Risk Platform"} },
            new { id="P010", name="Priya Iyer", role="QA Automation Engineer", location="Bengaluru", company="FinEdge", skills=new[]{"C#",".NET","Selenium","Azure","CI/CD"}, projects=new[]{"Payments Modernization","Customer Portal"} },
            new { id="P011", name="Dev Patel", role="DevOps Engineer", location="Ahmedabad", company="CloudWorks", skills=new[]{"Azure","Docker","Kubernetes","CI/CD","Terraform"}, projects=new[]{"Cloud Migration","Risk Platform"} },
            new { id="P012", name="Sana Khan", role="Business Analyst", location="Gurugram", company="SecureBank", skills=new[]{"SQL","ETL","OAuth2","Payments","Analytics"}, projects=new[]{"Fraud Analytics","Payments Modernization"} }
        };

        foreach (var person in people)
        {
            await session.RunAsync("""
                MERGE (p:Person {id: $id})
                SET p.name=$name, p.role=$role, p.location=$location
                MERGE (c:Company {name: $company})
                MERGE (p)-[:WORKED_AT]->(c)
                WITH p
                UNWIND $skills AS skillName
                MERGE (s:Skill {name: skillName})
                MERGE (p)-[:HAS_SKILL]->(s)
                """, new
            {
                person.id, person.name, person.role, person.location, person.company,
                skills = person.skills
            });

            await session.RunAsync("""
                MATCH (p:Person {id: $id})
                UNWIND $projects AS projectName
                MERGE (pr:Project {name: projectName})
                MERGE (p)-[:CONTRIBUTED_TO]->(pr)
                """, new { person.id, projects = person.projects });
        }

        await session.RunAsync("""
            MATCH (pr:Project)
            UNWIND CASE pr.name
                WHEN 'Payments Modernization' THEN ['C#','.NET','Azure','Kafka','SQL','OAuth2','Payments']
                WHEN 'Risk Platform' THEN ['C#','.NET','Azure','Kafka','SQL','Python','ETL','Kubernetes']
                WHEN 'Customer Portal' THEN ['C#','.NET','Angular','TypeScript','HTML','CSS','SQL']
                WHEN 'Fraud Analytics' THEN ['Python','SQL','Kafka','ETL','Analytics']
                WHEN 'Cloud Migration' THEN ['Azure','Docker','Kubernetes','Terraform','CI/CD']
                WHEN 'Identity Platform' THEN ['C#','.NET','Azure','OAuth2','JWT']
                ELSE []
            END AS skillName
            MATCH (s:Skill {name: skillName})
            MERGE (pr)-[:REQUIRES_SKILL]->(s)
            """);
    }
}
