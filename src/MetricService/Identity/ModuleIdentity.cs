namespace MetricService.Identity;

public readonly record struct ModuleIdentity(string Kind, string Value);

public static class ResourceAttributeKeys
{
    public const string LicenseName = "license.name";
    public const string Hwid = "hwid";
}
