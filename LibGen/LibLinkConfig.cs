using System.Collections.Generic;
using System.Linq;

namespace Dasblinkenlight.LibGen;

internal class LibLinkConfig {

    public string Name { get; }

    public string Version { get; }

    public string Provider { get; }

    public bool PreserveVersion { get; }

    public IList<AbstractLibFile> Files { get; }

    public LibLinkConfig(
        string name,
        string version,
        string provider,
        IEnumerable<AbstractLibFile> files,
        bool preserveVersion = false) {
        Name = name;
        Version = version;
        Provider = provider;
        PreserveVersion = preserveVersion;
        Files = files.ToList().AsReadOnly();
        foreach (var file in Files) {
            file.Lib =this;
        }
    }

}
