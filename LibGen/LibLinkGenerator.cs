using System;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;
using System.Web.UI;
using Microsoft.Build.Framework;
using Task = Microsoft.Build.Utilities.Task;

[assembly: InternalsVisibleTo("Test-LibGen")]

namespace Dasblinkenlight.LibGen;

public class LibLinkGenerator : Task {

    // Root location of the project
    [Required]
    public string? RootFolder { get; set; }

    // Root location of fallback artifacts
    [Required]
    public string? FallbackRoot { get; set; }

    // Library definitions file
    [Required]
    public string? LibraryDefinitions { get; set; }

    // Result file is written on successful completion
    [Required]
    public string? LibraryResultFile { get; set; }

    private static readonly Assembly Asm = typeof(LibLinkGenerator).Assembly;

    private static readonly JavaScriptEncoder JavaScriptEncoder = JavaScriptEncoder.Default;

    private static readonly HtmlEncoder HtmlEncoder = HtmlEncoder.Default;

    private static string GetResourceName(string name) =>
        $"{Asm.GetName().Name!}.compiler.resources.{name}";

    private static Stream GetResourceStream(string name) =>
        Asm.GetManifestResourceStream(GetResourceName(name))!;

    private string GetLocalUrl(AbstractLibFile file) {
        var localHrefBase = FallbackRoot?.Substring(FallbackRoot.IndexOf('/'));
        var rawHref = $"{localHrefBase}/{file.Lib.Name}/{file.Name}";
        return JavaScriptEncoder.Encode(HtmlEncoder.Encode(rawHref));
    }

    internal string ArtifactPath => Path.Combine(RootFolder!, FallbackRoot!);

    internal string ComponentPath => Path.Combine(RootFolder!, "Pages", "Shared");

    private string LibDefFile => Path.Combine(RootFolder!, LibraryDefinitions!);

    public override bool Execute() {
        // Check properties and ensure the required paths exist
        if (string.IsNullOrWhiteSpace(RootFolder)) {
            Log.LogError("ProjectRootPath is not set");
            return false;
        }
        if (!Directory.Exists(RootFolder)) {
            Log.LogError("{0:RootPath} does not exist", RootFolder);
            return false;
        }
        if (string.IsNullOrWhiteSpace(LibraryDefinitions)) {
            Log.LogError("LibDefFile is not set");
            return false;
        }
        if (!File.Exists(LibDefFile)) {
            Log.LogError("{0:LibDefFile} does not exist", LibDefFile);
            return false;
        }
        if (string.IsNullOrWhiteSpace(FallbackRoot)) {
            Log.LogError("FallbackRoot is not set");
            return false;
        }
        if (string.IsNullOrWhiteSpace(LibraryResultFile)) {
            Log.LogError("Result file is not set");
            return false;
        }
        try {
            if (File.Exists(LibraryResultFile)) {
                File.Delete(LibraryResultFile!);
            }
            if (!Directory.Exists(ArtifactPath)) {
                Directory.CreateDirectory(ArtifactPath);
            }
            if (!Directory.Exists(ComponentPath)) {
                Directory.CreateDirectory(ComponentPath);
            }
            using var libDefStream = File.OpenRead(LibDefFile);
            var config = LibGenConfig.Parse(libDefStream);
            libDefStream.Close();
            foreach (var lib in config.Libraries) {
                var result = TryProcessLibraryAsync(lib).GetAwaiter().GetResult();
                if (!result) {
                    Log.LogError("Unable to process {0:LibName}", lib.Name);
                }
            }
            if (Log.HasLoggedErrors) {
                return false;
            }
            // Write the result file
            using var resFile = File.OpenWrite(LibraryResultFile!);
            using var resWriter = new StreamWriter(resFile);
            foreach (var lib in config.Libraries) {
                resWriter.WriteLine($"{lib.Name}:{lib.Version}");
            }
            resWriter.Close();
            resFile.Close();
        } catch (Exception e) {
            Log.LogErrorFromException(e);
            return false;
        }
        return true;
    }

    internal async Task<bool> TryProcessLibraryAsync(LibLinkConfig lib) {
        var libBasePath = Path.Combine(ArtifactPath, lib.Name);
        Directory.CreateDirectory(libBasePath);
        foreach (var file in lib.Files) {
            string pathToFile;
            string fileName;
            var separatorIndex = file.Name.LastIndexOf(Path.DirectorySeparatorChar);
            if (separatorIndex != -1) {
                var extraPath = file.Name.Substring(0, separatorIndex);
                pathToFile = Path.Combine(libBasePath, extraPath);
                Directory.CreateDirectory(pathToFile);
                fileName = file.Name.Substring(separatorIndex+1);
            } else {
                pathToFile = libBasePath;
                fileName = file.Name;
            }
            var destinationPath = Path.Combine(pathToFile, fileName);
            using var stream = File.OpenWrite(destinationPath);
            if (!await TryCopyContentToStream(file, stream)) {
                Log.LogError("Unable to copy library content to {0:DestinationPath}", destinationPath);
                return false;
            }
            stream.Close();
            var shaRes = TryGetIntegrityHash(pathToFile, fileName, out var sha512);
            if (!shaRes) {
                Log.LogError("Unable to make integrity hash for {0:DestinationPath}", destinationPath);
                return false;
            }
            if (file is not AbstractComponentFile component) {
                continue;
            }
            var viewName = Path.Combine(ComponentPath, $"{component.ComponentName}.cshtml");
            File.Delete(viewName);
            using var htmlStream = File.OpenWrite(viewName);
            using var htmlWriter = new StreamWriter(htmlStream);
            using var html = new HtmlTextWriter(htmlWriter);
            WritePartialViewForLibFile(file, sha512, html);
            html.Close();
            htmlWriter.Close();
            htmlStream.Close();
        }
        return true;
    }

    private bool TryGetIntegrityHash(string filePath, string fileName, out string hashStr) {
        var path = Path.Combine(filePath, fileName);
        if (!File.Exists(path)) {
            hashStr = string.Empty;
            return false;
        }
        using var fs = File.OpenRead(path);
        using var hash = System.Security.Cryptography.SHA512.Create();
        hashStr = $"sha512-{ Convert.ToBase64String(hash.ComputeHash(fs)) }";
        Log.LogMessage($"File: {fileName}, Hash: {{hashStr}}", fileName, hashStr);
        return true;
    }

    private async Task<bool> TryCopyContentToStream(AbstractLibFile file, Stream outputStream,
        CancellationToken cancellationToken = default) {
        using var httpClient = new HttpClient();
        using var responseMsg = await httpClient.GetAsync(new Uri(file.RemoteUrl), cancellationToken);
        if (!responseMsg.IsSuccessStatusCode) {
            Log.LogError("Unable to get '{0:RemoteUrl}': {1:Response}", file.RemoteUrl, responseMsg.ReasonPhrase);
            return false;
        }
        await responseMsg.Content.CopyToAsync(outputStream);
        return true;
    }

    internal void WritePartialViewForLibFile(AbstractLibFile link, string integrityHash, HtmlTextWriter html) {
        WritePartialView(link as dynamic, integrityHash, html);
    }

    private void WritePartialView(StyleLibFile cssLink, string integrityHash, HtmlTextWriter html) {
        html.AddAttribute("rel", "stylesheet");
        html.AddAttribute("href", HtmlEncoder.Encode(cssLink.RemoteUrl));
        html.AddAttribute("asp-fallback-href", GetLocalUrl(cssLink));
        html.AddAttribute("asp-suppress-fallback-integrity", "true");
        html.AddAttribute("asp-fallback-test-class", cssLink.TestClass);
        html.AddAttribute("asp-fallback-test-property", cssLink.TestProperty);
        html.AddAttribute("asp-fallback-test-value", cssLink.TestValue);
        html.AddAttribute("integrity", integrityHash);
        html.AddAttribute("crossorigin", "anonymous");
        html.AddAttribute("referrerpolicy", "no-referrer");
        html.RenderBeginTag("link");
        html.RenderEndTag();
    }

    private void WritePartialView(StyleRawLibFile cssLink, string integrityHash, HtmlTextWriter html) {
        var htmlText = new StringBuilder();
        using var jsStream = GetResourceStream("FallbackWithPseudoJavaScript.js");
        using var jsReader = new StreamReader(jsStream);
        var jsFunc = jsReader.ReadToEnd();
        // cut off the final ); which were added for minification
        jsFunc = jsFunc.Substring(0, jsFunc.Length - 2);
        const string tags = " rel=\"stylesheet\" crossorigin=\"anonymous\" referrerpolicy=\"no-referrer\"";
        htmlText
            .Append(jsFunc)
            .Append("\"")
            .Append(JavaScriptEncoder.Encode(cssLink.TestProperty))
            .Append("\",\"")
            .Append(JavaScriptEncoder.Encode(cssLink.TestValue))
            .Append("\",[\"")
            .Append(GetLocalUrl(cssLink))
            .Append("\"],\"")
            .Append(JavaScriptEncoder.Encode(tags))
            .Append("\",");
        if (!string.IsNullOrWhiteSpace(cssLink.TestElement)) {
            htmlText
                .Append("\"")
                .Append(JavaScriptEncoder.Encode(cssLink.TestElement!))
                .Append("\");");
        } else {
            htmlText.Append("null);");
        }
        html.AddAttribute("rel", "stylesheet");
        html.AddAttribute("href", HtmlEncoder.Encode(cssLink.RemoteUrl));
        html.AddAttribute("integrity", integrityHash);
        html.AddAttribute("crossorigin", "anonymous");
        html.AddAttribute("referrerpolicy", "no-referrer");
        html.RenderBeginTag("link");
        html.RenderEndTag();

        html.AddAttribute("name", "x-stylesheet-fallback-test");
        html.AddAttribute("content", "");
        html.AddAttribute(cssLink.TestAttribute ?? "class", cssLink.TestClass);
        html.RenderBeginTag("meta");
        html.RenderEndTag();
        html.RenderBeginTag("script");
        html.Write(htmlText.ToString());
        html.RenderEndTag();
    }

    private void WritePartialView(ScriptLibFile scriptLink, string integrityHash, HtmlTextWriter html) {
        html.AddAttribute("rel", "stylesheet");
        html.AddAttribute("src", HtmlEncoder.Encode(scriptLink.RemoteUrl));
        html.AddAttribute("asp-fallback-src", GetLocalUrl(scriptLink));
        html.AddAttribute("asp-fallback-test", scriptLink.TestClass);
        html.AddAttribute("asp-suppress-fallback-integrity", "true");
        html.AddAttribute("integrity", integrityHash);
        html.AddAttribute("crossorigin", "anonymous");
        html.AddAttribute("referrerpolicy", "no-referrer");
        html.RenderBeginTag("script");
        html.RenderEndTag();
    }

    private void WritePartialView(ResourceLibFile _1, string _2, HtmlTextWriter _3) {
    }

    private void WritePartialView(object cssLink,  string _1, HtmlTextWriter _2) =>
        throw new InvalidOperationException($"Unknown file type: {cssLink.GetType().FullName}");

}
