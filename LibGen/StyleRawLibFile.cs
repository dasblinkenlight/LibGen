namespace Dasblinkenlight.LibGen;

internal class StyleRawLibFile : StyleLibFile {

    public string? TestElement { get; }

    public string? TestAttribute { get; }

    public StyleRawLibFile(string name,
        string componentName,
        string testClass,
        string testProperty,
        string testValue,
        string? testElement,
        string? testAttribute) : base(name, componentName, testClass, testProperty, testValue) {
        TestElement = testElement;
        TestAttribute = testAttribute;
    }

}
