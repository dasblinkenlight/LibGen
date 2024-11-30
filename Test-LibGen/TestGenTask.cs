using System.Reflection;
using System.Web.UI;
using Dasblinkenlight.LibGen;
using Xunit.Abstractions;

namespace Test_LibGen;

public class TestGenTask : IClassFixture<TestContext> {

    private static readonly Assembly Asm = typeof(TestGenTask).Assembly;

    private static string GetResourceName(string name) =>
        $"{Asm.GetName().Name!.Replace('-', '_')}.TestConfig.{name}";

    private static Stream GetResourceStream(string name) =>
        Asm.GetManifestResourceStream(GetResourceName(name))!;

    private readonly TestContext context;

    private readonly ITestOutputHelper output;

    public TestGenTask(TestContext context, ITestOutputHelper output) {
        this.context = context;
        this.output = output;
    }

    [Fact]
    public void TestReadConfig() {
        var config = LibGenConfig.Parse(GetResourceStream("Test-Bootstrap.json"));
        Assert.NotNull(config);
        Assert.Equal(4, config.Libraries.Count);
        var lib1 = config.Libraries[1];
        Assert.Equal("bootstrap", lib1.Name);
        Assert.Equal("5.3.3", lib1.Version);
        Assert.Equal("cdnjs", lib1.Provider);
        var lib1files = lib1.Files;
        Assert.Equal(2, lib1files.Count);
        Assert.IsType<ScriptLibFile>(lib1files[0]);
        Assert.IsType<StyleLibFile>(lib1files[1]);
    }

    [Fact]
    public async Task TestCopyContentToStream() {
        var gen = CreateTestGen();
        var config = LibGenConfig.Parse(GetResourceStream("Test-Bootstrap.json"));
        Directory.CreateDirectory(gen.ArtifactPath);
        Directory.CreateDirectory(gen.ComponentPath);

        foreach (var lib in config.Libraries) {
            var res = await gen.TryProcessLibraryAsync(lib);
            Assert.True(res, $"Cannot process library {lib.Name}");
        }
    }

    [Fact]
    public void TestWritePartial() {
        using var textWriter = new StringWriter();
        using var html = new HtmlTextWriter(textWriter);
        var gen = CreateTestGen();
        var config = LibGenConfig.Parse(GetResourceStream("Test-Bootstrap.json"));
        foreach (var lib in config.Libraries) {
            foreach (var file in lib.Files) {
                gen.WritePartialViewForLibFile(file, "test-integrity", html);
            }
        }
    }

    private LibLinkGenerator CreateTestGen() {
        var res = new LibLinkGenerator {
            FallbackRoot = "wwwroot/assets/vendor",
            RootFolder = Path.Combine(context.TempDir.FullName, "Projects", "GreenwichBotanicalArt"),
            BuildEngine = new MockBuildEngine()
        };
        return res;
    }

}
