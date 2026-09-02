namespace Dasblinkenlight.LibGen;

internal class AbstractComponentFile : AbstractLibFile {

    public string ComponentName { get; }

    protected AbstractComponentFile(string name, string componentName) : base(name) {
        ComponentName = componentName;
    }

}
