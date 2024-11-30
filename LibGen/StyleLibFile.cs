namespace Dasblinkenlight.LibGen;

internal class StyleLibFile : AbstractComponentFile {

    public string TestClass {get; private set;}

    public string TestProperty {get; private set;}

    public string TestValue {get; private set;}

    public StyleLibFile(string name,
        string componentName,
        string testClass,
        string testProperty,
        string testValue) : base(name, componentName) {
        TestClass = testClass;
        TestProperty = testProperty;
        TestValue = testValue;
    }

}
