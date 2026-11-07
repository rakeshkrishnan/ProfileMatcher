namespace ProfileMatcher;

internal static class SelfTests
{
    public static void Run()
    {
        var catalog = new SkillCatalog();
        var service = new MatchingService(catalog);
        var passed = 0;
        void Check(string name, bool condition)
        {
            if (!condition) throw new InvalidOperationException($"FAIL: {name}");
            passed++;
            Console.WriteLine($"PASS: {name}");
        }
        static Profile P(params SkillEvidence[] skills) => new("1", "Candidate", skills);
        static Job J(params Requirement[] requirements) => new("Test", "Reviewed", requirements);
        var csharp = new Requirement("C#", ["C#"], 35, true, 3);
        var azure = new Requirement("Azure", ["Azure"], 10, true);
        var job = J(csharp, azure);
        var qualified = P(new SkillEvidence("C Sharp", "Four years using C Sharp", 4));
        var result = service.Rank(job, [qualified]).Single();
        Check("Alias and weighted denominator", result.Percentage == 77.78m);
        Check("Missing required evidence is unknown", result.Requirements[1].Status == MatchStatus.Unknown);
        Check("Strict mode excludes unmet required requirements", service.Rank(job, [qualified], true).Length == 0);
        Check("Insufficient years", service.Rank(J(csharp), [P(new SkillEvidence("C#", "Two years", 2))])
            .Single().Requirements.Single().Status == MatchStatus.NotSatisfied);
        Check("Unknown years", service.Rank(J(csharp), [P(new SkillEvidence("C#", "Used C#"))])
            .Single().Requirements.Single().Status == MatchStatus.Unknown);
        Check("Duplicate mentions do not increase score", service.Rank(J(csharp),
            [P(new SkillEvidence("C#", "Project A", 4), new("CSharp", "Project B", 4))]).Single().Percentage == 100m);
        Check("Extra skills do not reduce coverage", service.Rank(J(csharp),
            [P(new SkillEvidence("C#", "C# project", 4), new("Python", "Python project"))]).Single().Percentage == 100m);
        Check("Alternative needs only one skill", service.Rank(J(new Requirement("Cloud", ["Azure", "AWS"], 10, true)),
            [P(new SkillEvidence("AWS", "AWS production deployment"))]).Single().AllRequiredSatisfied);
        Check("Blank evidence receives no credit", service.Rank(J(azure), [P(new SkillEvidence("Azure", ""))]).Single().Percentage == 0m);
        var mentions = catalog.FindMentions("JavaScript C++ C# ASP.NET Core MSSQL");
        Check("Phrase boundaries and symbols", mentions.Contains("JavaScript") && !mentions.Contains("Java")
            && mentions.Contains("C++") && mentions.Contains("C#")
            && mentions.Contains("ASP.NET Core") && !mentions.Contains(".NET") && mentions.Contains("SQL Server"));
        var valid = new Profile("2", "Qualified", [new("C#", "Four years", 4), new("Azure", "Azure deployment")]);
        Check("Required coverage takes priority in ranking", service.Rank(job, [qualified, valid])[0].ProfileId == "2");
        var rejected = false;
        try { service.Rank(J(), []); } catch (ArgumentException) { rejected = true; }
        Check("Empty JD cannot be scored", rejected);
        rejected = false;
        try { service.Rank(J(csharp, csharp with { Label = "Duplicate", AnyOf = ["CSharp"] }), []); }
        catch (ArgumentException) { rejected = true; }
        Check("Overlapping alias requirements rejected", rejected);
        Console.WriteLine($"{passed} checks passed.");
    }
}
