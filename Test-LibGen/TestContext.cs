namespace Test_LibGen;

public class TestContext : IDisposable {

    public DirectoryInfo TempDir { get; } = Directory.CreateTempSubdirectory("LibGen-");

    public void Dispose() {
        Directory.Delete(TempDir.FullName, true);
    }

}
