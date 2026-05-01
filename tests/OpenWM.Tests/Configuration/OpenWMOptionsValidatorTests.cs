using OpenWM.Configuration;

namespace OpenWM.Tests.Configuration;

public class OpenWMOptionsValidatorTests
{
    [Fact]
    public void Validate_DefaultOptions_Succeeds()
    {
        var validator = new OpenWMOptionsValidator();
        var result = validator.Validate(null, new OpenWMOptions());

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Validate_InvalidWorkspaceCount_Fails()
    {
        var validator = new OpenWMOptionsValidator();
        var result = validator.Validate(null, new OpenWMOptions { WorkspaceCount = 0 });

        Assert.False(result.Succeeded);
    }

    [Fact]
    public void Validate_EmptyVirtualDesktopName_Fails()
    {
        var validator = new OpenWMOptionsValidator();
        var options = new OpenWMOptions
        {
            VirtualDesktop = new VirtualDesktopOptions
            {
                Name = "   ",
            },
        };

        var result = validator.Validate(null, options);

        Assert.False(result.Succeeded);
    }
}
