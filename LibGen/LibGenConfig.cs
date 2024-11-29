using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace LibGen;

internal class LibGenConfig {

    public IList<LibLinkConfig> Libraries { get; }

    private LibGenConfig(IEnumerable<LibLinkConfig> libs) {
        Libraries = libs.ToList().AsReadOnly();
    }

    public static LibGenConfig Parse(Stream stream) {
        var doc = JsonDocument.Parse(stream);
        if (doc.RootElement.TryGetProperty("version", out var ver)) {
            if (ver.ValueKind != JsonValueKind.String ||  ver.GetString() != "1.0") {
                throw new InvalidOperationException($"Unknown version: {ver}");
            }
        } else {
            throw new InvalidOperationException("Unspecified element: version");
        }
        if (!doc.RootElement.TryGetProperty("libraries", out var libraryElements) ||
            libraryElements.ValueKind != JsonValueKind.Array) {
            throw new InvalidOperationException("Unspecified element: libraries");
        }
        var libs = new List<LibLinkConfig>();
        foreach (var libElem in libraryElements.EnumerateArray()) {
            if (!libElem.TryGetProperty("library", out var libName) ||
                libName.ValueKind != JsonValueKind.String) {
                throw new InvalidOperationException("Incorrect element: library");
            }
            if (!libElem.TryGetProperty("version", out var libVer) ||
                libVer.ValueKind != JsonValueKind.String) {
                throw new InvalidOperationException("Incorrect element: version");
            }
            if (!libElem.TryGetProperty("provider", out var libProvider) ||
                libProvider.ValueKind != JsonValueKind.String) {
                throw new InvalidOperationException("Incorrect element: provider");
            }
            if (!libElem.TryGetProperty("files", out var libFiles) ||
                libFiles.ValueKind != JsonValueKind.Array) {
                throw new InvalidOperationException("Incorrect element: files");
            }
            var files = new List<AbstractLibFile>();
            foreach (var fileElem in libFiles.EnumerateArray()) {
                if (fileElem.ValueKind == JsonValueKind.Object) {
                    if (!fileElem.TryGetProperty("file", out var fileName)) {
                        throw new InvalidOperationException("Incorrect element: file:file");
                    }
                    if (!fileElem.TryGetProperty("component", out var componentName)) {
                        throw new InvalidOperationException("Incorrect element: file:component");
                    }
                    var hasTestClass = fileElem.TryGetProperty("test-class", out var testClass);
                    var hasTestValue = fileElem.TryGetProperty("test-value", out var testValue);
                    var hasTestElement = fileElem.TryGetProperty("test-element", out var testElement);
                    var hasTestProperty = fileElem.TryGetProperty("test-property", out var testProperty);
                    var hasTestAttribute = fileElem.TryGetProperty("test-attribute", out var testAttribute);
                    if (hasTestClass && !hasTestValue && !hasTestElement && !hasTestProperty) {
                        files.Add(new ScriptLibFile(
                            fileName.GetString()!, componentName.GetString()!, testClass.GetString()!));
                    } else if (hasTestClass && hasTestProperty && hasTestValue) {
                        if (!hasTestElement && !hasTestAttribute) {
                            files.Add(new StyleLibFile(fileName.GetString()!, componentName.GetString()!,
                                testClass.GetString()!, testProperty.GetString()!, testValue.GetString()!));
                        } else {
                            files.Add(new StyleRawLibFile(fileName.GetString()!, componentName.GetString()!,
                                testClass.GetString()!, testProperty.GetString()!, testValue.GetString()!,
                                testElement.ValueKind == JsonValueKind.String ? testElement.GetString() : null,
                                testAttribute.ValueKind == JsonValueKind.String ? testAttribute.GetString() : null));
                        }
                    } else {
                        throw new InvalidOperationException("incomplete element: file/tags");
                    }
                } else if (fileElem.ValueKind == JsonValueKind.String) {
                    files.Add(new ResourceLibFile(fileElem.GetString()!));
                } else {
                    throw new InvalidOperationException("Incorrect element type: file");
                }
            }
            libs.Add(new LibLinkConfig(
                libName.GetString()!,
                libVer.GetString()!,
                libProvider.GetString()!,
                files));
        }
        return new LibGenConfig(libs);
    }
}
