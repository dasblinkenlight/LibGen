using System.Collections.Generic;
using System.Linq;

namespace LibGen;

internal class LibLinkConfig {

    public string Name { get; }

    public string Version { get; }

    public string Provider { get; }

    public IList<AbstractLibFile> Files { get; }

    public LibLinkConfig(
        string name,
        string version,
        string provider,
        IEnumerable<AbstractLibFile> files) {
        Name = name;
        Version = version;
        Provider = provider;
        Files = files.ToList().AsReadOnly();
        foreach (var file in Files) {
            file.Lib =this;
        }
    }

}
