namespace Mandrillus.Kernel;

/// <summary>
/// Centralized version identity for Mandrillus OS.
///
/// MANUAL VERSIONING (for now): there is no CI/build pipeline generating
/// this automatically yet. Bump Version by hand as part of the same PR
/// that closes a roadmap Issue representing a complete feature (SemVer
/// MINOR bump), or fixes a bug (PATCH bump). After merging to master,
/// tag the merge commit to match, e.g.:
///
///   git tag v0.1.0
///   git push origin v0.1.0
///
/// Suggested cadence while pre-1.0 (0.x.x = unstable, API may still
/// change freely):
///   - MINOR bump: a roadmap Issue is closed representing a full
///     feature (e.g. #8 Drill shell -> 0.1.0, #9 PIT timer -> 0.2.0).
///   - PATCH bump: a fix within an already-released MINOR version.
///   - MAJOR bump: reserved for 1.0.0 (first complete milestone of the
///     kernel + minimal app ecosystem) or a later breaking change.
///
/// Revisit this file's header comment if/when a build pipeline is set
/// up to auto-generate BuildMetadata instead of leaving it manual.
/// </summary>
public static class MandrillusVersion
{
    /// <summary>
    /// SemVer MAJOR.MINOR.PATCH. Update by hand - see class remarks.
    /// </summary>
    public const string Version = "0.2.0";

    /// <summary>
    /// Optional per-release codename, for portfolio/changelog narration.
    /// Leave empty string if not using one for a given release.
    /// </summary>
    public const string Codename = "Drill";

    /// <summary>
    /// Optional build metadata (e.g. short commit hash), appended after
    /// a '+' per SemVer convention when present. Left empty for now -
    /// no CI pipeline generates this yet. Fill in by hand only if you
    /// want a specific build tracked (e.g. "a1b2c3d"); otherwise leave
    /// as string.Empty.
    /// </summary>
    public const string BuildMetadata = "";

    /// <summary>
    /// Copyright year shown alongside the version banner. Update by
    /// hand at year boundaries — see class remarks.
    ///
    /// This is a plain constant, not DateTime.Now.Year: the full
    /// System.DateTime type (Now, Today, etc.) has no implementation
    /// anywhere in the MOSA bare-metal stack (neither Mosa.Korlib nor
    /// Mosa.TinyCoreLib) — unlike the Dictionary/string.Join gaps, this
    /// isn't a "wrong corlib" issue, it's simply not provided, since
    /// DateTime.Now would need an OS-level time source to mean anything.
    /// MOSA instead exposes real time via Mosa.Kernel.BareMetal.Time,
    /// obtained through Platform.GetTime() — a plain struct with
    /// Year/Month/Day/Hour/Minute/Second byte/ushort fields, backed by
    /// the RTC chip on x86 (confirmed via
    /// Mosa.Kernel.BareMetal.x86/PlatformPlug.cs). That's a real,
    /// meaningful clock read, worth wiring up later for the 'time'-type
    /// command or a boot timestamp — but it's a bigger step than what
    /// this banner constant needs, so kept as a plain hand-updated year
    /// here for now. Revisit if/when a live clock read is wanted instead.
    /// </summary>
    public const string CopyrightYear = "2026";

    /// <summary>
    /// Returns the full display string, e.g. "0.1.0 (Drill)" or
    /// "0.1.0+a1b2c3d (Drill)". Built at runtime with a simple method
    /// rather than a const expression, to avoid relying on compile-time
    /// constant-folding of conditional (?:) string expressions, which
    /// isn't worth the risk of an unexpected Mosa.Korlib-related compile
    /// issue for something this low-stakes. Call this instead of
    /// concatenating Version/BuildMetadata/CodeName by hand elsewhere.
    /// </summary>
    public static string GetDisplayVersion()
    {
        var  display = Version;

        if (BuildMetadata.Length > 0)
            display += "+" + BuildMetadata;

        if (Codename.Length > 0)
            display += " (" + Codename + ")";

        return display;
    }
}
