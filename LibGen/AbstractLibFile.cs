using System;

namespace Dasblinkenlight.LibGen;

internal abstract class AbstractLibFile {

    public string Name { get; }

    internal LibLinkConfig Lib { get; set; } = null!;

    public string RemoteUrl => Lib.Provider switch {
        "cdnjs" => $"https://cdnjs.cloudflare.com/ajax/libs/{Lib.Name}/{Lib.Version}/{Name}",
        "jsdelivr" =>  $"https://cdn.jsdelivr.net/npm/{Lib.Name}@{Lib.Version}/{Name}",
        "jsdelivr-gh" =>$"https://cdn.jsdelivr.net/gh/{Lib.Name}@{Lib.Version}/{Name}",
        _ => throw new InvalidOperationException($"Unknown lib provider: {Lib.Provider}")
    };

    protected AbstractLibFile(string name) {
        Name = name;
    }

}
