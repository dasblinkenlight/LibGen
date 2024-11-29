namespace LibGen;

internal class AbstractComponentFile : AbstractLibFile {

    public string ComponentName { get; }

    public AbstractComponentFile(string name, string componentName) : base(name) {
        ComponentName = componentName;
    }

}
