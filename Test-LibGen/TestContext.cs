namespace Test_LibGen;

// ReSharper disable once ClassNeverInstantiated.Global
public class TestContext : IDisposable {

    public DirectoryInfo TempDir { get; } = Directory.CreateTempSubdirectory("LibGen-");

    public void Dispose() {
        Directory.Delete(TempDir.FullName, true);
    }

}
