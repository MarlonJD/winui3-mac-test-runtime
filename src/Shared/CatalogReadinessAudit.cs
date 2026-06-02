namespace WinUI3.MacCompatibility;

/// <summary>
/// Deterministic, per-entry production readiness audit for the full
/// compatibility catalog.
/// </summary>
/// <remarks>
/// Phase 6 ("All-126 catalog closure") requires every catalog entry to carry an
/// explicit production disposition, owner phase, primary blocker, evidence
/// profile, and release-candidate gate, with no unknown or silent behavior. The
/// audit is derived purely from <see cref="CompatibilityCatalog"/> so it cannot
/// drift from the catalog source of truth: status drives the disposition,
/// kind/status drives the primary blocker (reproducing the published
/// PB-001..PB-012 counts), and area/status drives the owner phase.
/// </remarks>
public static class CatalogReadinessAudit
{
    public const string SchemaVersion = "0.1";

    public const string DispositionSourceLevelImplementation = "source-level-production-implementation";
    public const string DispositionBoundedImplementation = "bounded-source-level-production-implementation";
    public const string DispositionDiagnosticExclusion = "production-ready-diagnostic-exclusion-until-promoted";
    public const string DispositionWindowsOnlyExclusion = "production-ready-windows-only-exclusion";
    public const string DispositionNonGoalExclusion = "production-ready-non-goal-exclusion";

    // One production disposition per catalog status. The counts stay
    // 55/35/31/3/2 = 126 to match the catalog snapshot.
    private static readonly IReadOnlyDictionary<string, string> DispositionByStatus = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        [CompatibilityStatuses.Supported] = DispositionSourceLevelImplementation,
        [CompatibilityStatuses.Partial] = DispositionBoundedImplementation,
        [CompatibilityStatuses.Planned] = DispositionDiagnosticExclusion,
        [CompatibilityStatuses.WindowsOnly] = DispositionWindowsOnlyExclusion,
        [CompatibilityStatuses.NotSupported] = DispositionNonGoalExclusion,
    };

    // Primary blocker keyed by "kind|status". This reproduces the published
    // blocker totals exactly: PB-001:14, PB-002:28, PB-003:62, PB-004:8,
    // PB-012:14 = 126.
    private static readonly IReadOnlyDictionary<string, string> BlockerByKindStatus = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["api|not supported"] = "PB-012",
        ["api|partial"] = "PB-003",
        ["api|planned"] = "PB-001",
        ["api|supported"] = "PB-003",
        ["api|windows-only"] = "PB-012",
        ["fluent-resource|partial"] = "PB-003",
        ["fluent-resource|planned"] = "PB-004",
        ["project-item|supported"] = "PB-002",
        ["project-item|windows-only"] = "PB-012",
        ["project-property|partial"] = "PB-002",
        ["project-property|planned"] = "PB-012",
        ["project-property|supported"] = "PB-002",
        ["visual-state|partial"] = "PB-003",
        ["visual-state|planned"] = "PB-004",
        ["xaml-attached-property|supported"] = "PB-002",
        ["xaml-directive|planned"] = "PB-012",
        ["xaml-directive|supported"] = "PB-002",
        ["xaml-element|partial"] = "PB-003",
        ["xaml-element|planned"] = "PB-012",
        ["xaml-element|supported"] = "PB-003",
        ["xaml-event|supported"] = "PB-002",
        ["xaml-markup|partial"] = "PB-002",
        ["xaml-property|partial"] = "PB-002",
        ["xaml-property|planned"] = "PB-012",
        ["xaml-property-element|planned"] = "PB-004",
        ["xaml-resource|partial"] = "PB-002",
        ["xaml-resource|supported"] = "PB-002",
    };

    private static readonly IReadOnlyDictionary<string, string> EvidenceByDisposition = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        [DispositionSourceLevelImplementation] = "tests-fixtures-macos-artifacts-native-reference-when-visual-interaction-accessibility-when-applicable-docs",
        [DispositionBoundedImplementation] = "tests-fixtures-macos-artifacts-native-reference-when-visual-interaction-accessibility-when-applicable-docs",
        [DispositionDiagnosticExclusion] = "deterministic-diagnostic-docs-owner-promotion-exit-criteria",
        [DispositionWindowsOnlyExclusion] = "deterministic-exclusion-windows-validation-where-applicable-support-policy",
        [DispositionNonGoalExclusion] = "deterministic-non-goal-diagnostic-support-policy",
    };

    private static readonly IReadOnlyDictionary<string, string> ReleaseGateByDisposition = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        [DispositionSourceLevelImplementation] = "implemented-support-with-required-evidence",
        [DispositionBoundedImplementation] = "bounded-support-with-exact-limits-and-required-evidence",
        [DispositionDiagnosticExclusion] = "deterministic-diagnostic-with-promotion-exit-criteria",
        [DispositionWindowsOnlyExclusion] = "deterministic-windows-only-exclusion-with-support-policy",
        [DispositionNonGoalExclusion] = "deterministic-non-goal-exclusion-with-support-policy",
    };

    // Materials, composition, and motion are owned by the high-fidelity phase
    // regardless of status; everything else is owned by the phase that delivers
    // its support (Ring 0/Ring 1) or by the catalog closure phase.
    private static readonly HashSet<string> MaterialsMotionAreas = new(StringComparer.Ordinal)
    {
        "materials",
        "composition",
        "motion",
    };

    /// <summary>
    /// Builds the audit from the embedded compatibility catalog. Callers that
    /// reference multiple assemblies linking the catalog source can use this to
    /// avoid naming the duplicated <see cref="CompatibilityCatalog"/> type.
    /// </summary>
    public static CatalogReadinessAuditDocument BuildFromCurrentCatalog()
    {
        return Build(CompatibilityCatalog.Current);
    }

    public static CatalogReadinessAuditDocument Build(CompatibilityCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);

        var entries = catalog.Entries
            .OrderBy(entry => entry.Id, StringComparer.Ordinal)
            .Select(BuildEntry)
            .ToArray();

        var unassigned = entries.Count(entry => string.IsNullOrWhiteSpace(entry.Disposition));

        return new CatalogReadinessAuditDocument(
            SchemaVersion,
            "docs/compatibility/winui-api-compatibility.catalog.json",
            entries.Length,
            unassigned,
            CountBy(entries, entry => entry.Disposition),
            CountBy(entries, entry => entry.OwnerPhase),
            CountBy(entries, entry => entry.PrimaryBlocker),
            entries);
    }

    private static CatalogReadinessAuditEntry BuildEntry(CompatibilityCatalogEntry entry)
    {
        if (!DispositionByStatus.TryGetValue(entry.Status, out var disposition))
        {
            throw new InvalidOperationException(
                $"Catalog entry '{entry.Id}' has status '{entry.Status}' with no production disposition mapping.");
        }

        var blockerKey = string.Concat(entry.Kind, "|", entry.Status);
        if (!BlockerByKindStatus.TryGetValue(blockerKey, out var blocker))
        {
            throw new InvalidOperationException(
                $"Catalog entry '{entry.Id}' has kind/status '{blockerKey}' with no primary blocker mapping.");
        }

        return new CatalogReadinessAuditEntry(
            entry.Id,
            entry.Kind,
            entry.Status,
            entry.Area,
            entry.Api,
            disposition,
            ResolveOwnerPhase(entry),
            blocker,
            EvidenceByDisposition[disposition],
            ReleaseGateByDisposition[disposition]);
    }

    private static string ResolveOwnerPhase(CompatibilityCatalogEntry entry)
    {
        if (MaterialsMotionAreas.Contains(entry.Area))
        {
            return "Phase 8: Materials, motion, and high-fidelity polish";
        }

        return entry.Status switch
        {
            CompatibilityStatuses.Supported => "Phase 4: Ring 0 Windows chrome completion",
            CompatibilityStatuses.Partial => "Phase 5: Ring 1 E2E visual completion",
            _ => "Phase 6: All-catalog closure",
        };
    }

    private static IReadOnlyDictionary<string, int> CountBy(
        IEnumerable<CatalogReadinessAuditEntry> entries,
        Func<CatalogReadinessAuditEntry, string> selector)
    {
        var counts = new SortedDictionary<string, int>(StringComparer.Ordinal);
        foreach (var entry in entries)
        {
            var key = selector(entry);
            counts[key] = counts.TryGetValue(key, out var current) ? current + 1 : 1;
        }

        return counts;
    }
}

public sealed record CatalogReadinessAuditDocument(
    string SchemaVersion,
    string GeneratedFrom,
    int AccountedEntries,
    int UnassignedDispositionCount,
    IReadOnlyDictionary<string, int> DispositionCounts,
    IReadOnlyDictionary<string, int> OwnerPhaseCounts,
    IReadOnlyDictionary<string, int> BlockerCounts,
    IReadOnlyList<CatalogReadinessAuditEntry> Entries);

public sealed record CatalogReadinessAuditEntry(
    string Id,
    string Kind,
    string Status,
    string Area,
    string Api,
    string Disposition,
    string OwnerPhase,
    string PrimaryBlocker,
    string EvidenceProfile,
    string ReleaseGate);
