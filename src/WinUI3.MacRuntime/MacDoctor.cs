using System.Runtime.InteropServices;

namespace WinUI3.MacRuntime;

public sealed record DoctorReport(
    string SchemaVersion,
    string Status,
    string Host,
    bool PrimaryPathRequiresWine,
    string DotNetVersion,
    string OsDescription,
    string Architecture,
    WineDependency Wine);

public sealed record WineDependency(
    bool Required,
    string Status,
    bool Found,
    string? Path);

public static class MacDoctor
{
    public static DoctorReport Check()
    {
        var winePath = CommandLocator.FindOnPath("wine");

        return new DoctorReport(
            SchemaVersion: ArtifactSchemas.DoctorReport,
            Status: "ok",
            Host: "managed-macos-dotnet",
            PrimaryPathRequiresWine: false,
            DotNetVersion: Environment.Version.ToString(),
            OsDescription: RuntimeInformation.OSDescription,
            Architecture: RuntimeInformation.OSArchitecture.ToString(),
            Wine: new WineDependency(
                Required: false,
                Status: "optional",
                Found: winePath is not null,
                Path: winePath));
    }
}
