namespace InvokeSsrsDotnetCore.Models.Config;

public record AppSettings
{
    public Report ReportSettings { get; init; }
    public Credential CredentialSettings { get; init; }
}

public record Credential
{
    public string Domain { get; init; }
    public string User { get; init; }
    public string Password { get; init; }
}

public record Report
{
    public string Url { get; init; }
    public string Path { get; init; }
    public string Width { get; init; }
    public string Height { get; init; }
    public string Format { get; init; }
}
