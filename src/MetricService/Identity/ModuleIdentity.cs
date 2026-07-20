namespace MetricService.Identity;

public readonly record struct ModuleIdentity(string Kind, string Value);

public static class ResourceAttributeKeys
{
    public const string ServiceName = "service.name";
    public const string LicenseName = "l2.license_name";
}
