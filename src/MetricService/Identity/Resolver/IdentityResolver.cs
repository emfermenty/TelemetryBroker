namespace MetricService.Identity.Resolver;

public static class IdentityResolver
{
    public static bool TryResolve(OpenTelemetry.Proto.Resource.V1.Resource? resource, out ModuleIdentity identity)
    {
        var license = FindAttribute(resource, ResourceAttributeKeys.LicenseName);
        if (!string.IsNullOrEmpty(license))
        {
            identity = new ModuleIdentity("license", license);
            return true;
        }
        var hwid = FindAttribute(resource, ResourceAttributeKeys.Hwid);
        if (!string.IsNullOrEmpty(hwid))
        {
            identity = new ModuleIdentity("hwid", hwid);
            return true;
        }
        identity = default;
        return false;
    }
    private static string? FindAttribute(OpenTelemetry.Proto.Resource.V1.Resource? resource, string key)
    {
        if (resource == null)
        {
            return null;
        }
        foreach (var attribute in resource.Attributes)
        {
            if (attribute.Key == key && attribute.Value.HasStringValue)
            {
                return attribute.Value.StringValue;
            }
        }
        return null;
    }
}
