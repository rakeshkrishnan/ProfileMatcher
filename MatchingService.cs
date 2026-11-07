namespace ProfileMatcher;

public sealed class MatchingService(SkillCatalog catalog)
{
    public MatchResult[] Rank(Job job, Profile[] profiles, bool strict = false)
    {
        Validate(job, profiles);
        var denominator = job.Requirements.Sum(r => r.Weight);
        return profiles.Select(profile =>
        {
            var details = job.Requirements.Select(r => Evaluate(r, profile)).ToArray();
            var earned = details.Where(r => r.Status == MatchStatus.Satisfied).Sum(r => r.Weight);
            var required = details.Where(r => r.Required).ToArray();
            return new MatchResult(profile.Id, profile.Name,
                Math.Round(100m * earned / denominator, 2),
                required.Count(r => r.Status == MatchStatus.Satisfied), required.Length,
                required.All(r => r.Status == MatchStatus.Satisfied), details);
        })
        .Where(result => !strict || result.AllRequiredSatisfied)
        .OrderByDescending(result => result.AllRequiredSatisfied)
        .ThenByDescending(result => result.Percentage)
        .ThenBy(result => result.ProfileId, StringComparer.Ordinal)
        .ToArray();
    }

    private RequirementMatch Evaluate(Requirement requirement, Profile profile)
    {
        RequirementMatch Result(MatchStatus status, string explanation, string? evidence) =>
            new(requirement.Label, requirement.Weight, requirement.Required, status, explanation, evidence);

        var evidenced = FindEvidencedSkills(requirement, profile);

        var meetsYears = evidenced.FirstOrDefault(s =>
            requirement.MinimumYears is null || s.Years >= requirement.MinimumYears);
        if (meetsYears is not null)
            return Result(MatchStatus.Satisfied,
                $"Supported by {catalog.Normalize(meetsYears.Skill)} evidence.", meetsYears.Evidence);

        if (evidenced.Length == 0)
            return Result(MatchStatus.Unknown, "No reviewed supporting evidence found.", null);

        var unknownYears = evidenced.FirstOrDefault(s => s.Years is null);
        if (unknownYears is not null)
            return Result(MatchStatus.Unknown,
                $"Skill found; {requirement.MinimumYears} years cannot be verified.", unknownYears.Evidence);

        var best = evidenced.MaxBy(s => s.Years)!;
        return Result(MatchStatus.NotSatisfied,
            $"Requires {requirement.MinimumYears} years; best reviewed evidence states {best.Years}.",
            best.Evidence);
    }

    // Profile skills that match any alternative of the requirement and carry reviewed evidence.
    private SkillEvidence[] FindEvidencedSkills(Requirement requirement, Profile profile)
    {
        var alternatives = requirement.AnyOf.Select(catalog.Normalize).ToHashSet();
        return profile.Skills
            .Where(s => alternatives.Contains(catalog.Normalize(s.Skill)))
            .Where(s => !string.IsNullOrWhiteSpace(s.Evidence))
            .ToArray();
    }

    private void Validate(Job job, Profile[] profiles)
    {
        if (job.Requirements is not { Length: > 0 })
            throw new ArgumentException("Unable to score: provide at least one reviewed requirement.");
        var seen = new HashSet<string>();
        foreach (var r in job.Requirements)
        {
            if (r is null || string.IsNullOrWhiteSpace(r.Label) || r.AnyOf is not { Length: > 0 }
                || r.Weight <= 0 || r.MinimumYears < 0)
                throw new ArgumentException("Requirements need a label, skills, positive weight and nonnegative years.");
            var normalized = r.AnyOf.Select(catalog.Normalize).ToArray();
            if (normalized.Distinct().Count() != normalized.Length || normalized.Any(s => !seen.Add(s)))
                throw new ArgumentException("A canonical skill may appear in only one requirement; combine alternatives to avoid double counting.");
        }
        if (profiles.Any(p => p is null || string.IsNullOrWhiteSpace(p.Id)
            || string.IsNullOrWhiteSpace(p.Name) || p.Skills is null))
            throw new ArgumentException("Each profile needs an ID, name and skills array.");
        if (profiles.Select(p => p.Id).Distinct(StringComparer.Ordinal).Count() != profiles.Length)
            throw new ArgumentException("Profile IDs must be unique.");
        foreach (var skill in profiles.SelectMany(p => p.Skills))
        {
            if (skill is null || skill.Years < 0) throw new ArgumentException("Invalid profile skill or years.");
            catalog.Normalize(skill.Skill);
        }
    }
}
