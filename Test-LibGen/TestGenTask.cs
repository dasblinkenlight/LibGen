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

    [Theory]
    // Default shape: the web root segment ('wwwroot') is stripped from the front.
    [InlineData("wwwroot/assets/vendor", null, "assets/vendor")]
    // No web root segment present at all - FallbackRoot is already web-root-relative, so
    // nothing should be stripped from it (this used to throw ArgumentOutOfRangeException).
    [InlineData("vendor", null, "vendor")]
    // Doesn't start with 'wwwroot' - used to silently strip "assets" instead and produce the
    // wrong URL ("vendor" rather than "assets/vendor").
    [InlineData("assets/vendor", null, "assets/vendor")]
    // A non-default WebRootFolder is honored instead of the hardcoded default.
    [InlineData("public/assets/vendor", "public", "assets/vendor")]
    public void TestGetWebRelativeFallbackRoot(string fallbackRoot, string? webRootFolder, string expected) {
        var gen = new LibLinkGenerator {
            FallbackRoot = fallbackRoot,
            WebRootFolder = webRootFolder,
            RootFolder = Path.Combine(context.TempDir.FullName, "Projects", "GreenwichBotanicalArt"),
            BuildEngine = new MockBuildEngine()
        };
        Assert.Equal(expected, gen.GetWebRelativeFallbackRoot());
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
