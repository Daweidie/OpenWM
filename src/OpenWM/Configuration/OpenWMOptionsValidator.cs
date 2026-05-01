using Microsoft.Extensions.Options;

namespace OpenWM.Configuration;

public sealed class OpenWMOptionsValidator : IValidateOptions<OpenWMOptions>
{
    public ValidateOptionsResult Validate(string? name, OpenWMOptions options)
    {
        if (options.WorkspaceCount is < 1 or > 20)
        {
            return ValidateOptionsResult.Fail("WorkspaceCount must be between 1 and 20.");
        }

        if (options.Gaps is < 0 or > 80)
        {
            return ValidateOptionsResult.Fail("Gaps must be between 0 and 80.");
        }

        if (options.MasterRatio is < 0.2 or > 0.8)
        {
            return ValidateOptionsResult.Fail("MasterRatio must be between 0.2 and 0.8.");
        }

        if (options.PollIntervalMs is < 50 or > 2000)
        {
            return ValidateOptionsResult.Fail("PollIntervalMs must be between 50 and 2000.");
        }

        if (string.IsNullOrWhiteSpace(options.VirtualDesktop.Name))
        {
            return ValidateOptionsResult.Fail("VirtualDesktop.Name cannot be empty.");
        }

        if (options.VirtualDesktop.StartupCommands.Any(string.IsNullOrWhiteSpace))
        {
            return ValidateOptionsResult.Fail("VirtualDesktop.StartupCommands cannot include empty commands.");
        }

        return ValidateOptionsResult.Success;
    }
}
