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
        Assert.False(lib1.PreserveVersion);
        var lib1files = lib1.Files;
        Assert.Equal(2, lib1files.Count);
        Assert.IsType<ScriptLibFile>(lib1files[0]);
        Assert.IsType<StyleLibFile>(lib1files[1]);
    }

    [Fact]
    public void TestReadConfigPreserveVersion() {
        var config = LibGenConfig.Parse(GetResourceStream("Test-PreserveVersion.json"));
        Assert.Equal(2, config.Libraries.Count);
        Assert.True(config.Libraries[0].PreserveVersion);
        Assert.False(config.Libraries[1].PreserveVersion);
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
    public async Task TestCopyContentToStreamPreserveVersion() {
        var gen = CreateTestGen();
        var config = LibGenConfig.Parse(GetResourceStream("Test-PreserveVersion.json"));
        Directory.CreateDirectory(gen.ArtifactPath);
        Directory.CreateDirectory(gen.ComponentPath);

        var versionedLib = config.Libraries.Single(lib => lib.PreserveVersion);
        var unversionedLib = config.Libraries.Single(lib => !lib.PreserveVersion);

        Assert.True(await gen.TryProcessLibraryAsync(versionedLib),
            $"Cannot process library {versionedLib.Name}");
        Assert.True(await gen.TryProcessLibraryAsync(unversionedLib),
            $"Cannot process library {unversionedLib.Name}");

        var versionedPath = Path.Combine(
            gen.ArtifactPath, versionedLib.Name, versionedLib.Version, "imagesloaded.pkgd.min.js");
        var unversionedPath = Path.Combine(
            gen.ArtifactPath, unversionedLib.Name, "imagesloaded.pkgd.min.js");

        Assert.True(File.Exists(versionedPath), $"Expected artifact at {versionedPath}");
        Assert.True(File.Exists(unversionedPath), $"Expected artifact at {unversionedPath}");
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

    [Fact]
    public void TestWritePartialPreserveVersion() {
        var gen = CreateTestGen();
        var config = LibGenConfig.Parse(GetResourceStream("Test-PreserveVersion.json"));

        var versionedLib = config.Libraries.Single(lib => lib.PreserveVersion);
        var versionedOutput = RenderPartialView(gen, versionedLib.Files[0]);
        Assert.Contains($"/assets/vendor/{versionedLib.Name}/{versionedLib.Version}/", versionedOutput);

        var unversionedLib = config.Libraries.Single(lib => !lib.PreserveVersion);
        var unversionedOutput = RenderPartialView(gen, unversionedLib.Files[0]);
        Assert.DoesNotContain($"/assets/vendor/{unversionedLib.Name}/{unversionedLib.Version}/", unversionedOutput);
    }

    private static string RenderPartialView(LibLinkGenerator gen, AbstractLibFile file) {
        using var textWriter = new StringWriter();
        using var html = new HtmlTextWriter(textWriter);
        gen.WritePartialViewForLibFile(file, "test-integrity", html);
        return textWriter.ToString();
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
