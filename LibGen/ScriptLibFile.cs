namespace LibGen;

internal class ScriptLibFile : AbstractComponentFile {

    public string TestClass { get; }

    public ScriptLibFile(string filename, string componentName, string testClass) : base(filename, componentName) {
        TestClass = testClass;
    }

}
