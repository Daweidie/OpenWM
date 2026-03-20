using OpenWM.Config;

namespace OpenWM.Tests.Config;

public class ConfigurationTests
{
    [Fact]
    public void Defaults_AreReasonable()
    {
        var config = new Configuration();
        Assert.True(config.Gaps >= 0);
        Assert.True(config.WorkspaceCount > 0);
        Assert.NotEmpty(config.DefaultLayout);
        Assert.InRange(config.MasterRatio, 0.1, 0.9);
    }

    [Fact]
    public void Load_MissingFile_ReturnsDefaults()
    {
        var config = Configuration.Load("/tmp/does_not_exist_openwm.json");
        Assert.NotNull(config);
        Assert.Equal("dwindle", config.DefaultLayout);
    }

    [Fact]
    public void SaveAndLoad_RoundTrips()
    {
        var path = Path.Combine(Path.GetTempPath(), $"openwm_test_{Guid.NewGuid()}.json");
        try
        {
            var original = new Configuration
            {
                DefaultLayout  = "master",
                Gaps           = 16,
                WorkspaceCount = 5,
                MasterRatio    = 0.6,
            };
            original.Save(path);

            var loaded = Configuration.Load(path);
            Assert.Equal("master", loaded.DefaultLayout);
            Assert.Equal(16,       loaded.Gaps);
            Assert.Equal(5,        loaded.WorkspaceCount);
            Assert.Equal(0.6,      loaded.MasterRatio);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void Load_InvalidJson_ReturnsDefaults()
    {
        var path = Path.Combine(Path.GetTempPath(), $"openwm_invalid_{Guid.NewGuid()}.json");
        try
        {
            File.WriteAllText(path, "{ invalid json !!!");
            var config = Configuration.Load(path);
            Assert.NotNull(config);
            Assert.Equal("dwindle", config.DefaultLayout);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
