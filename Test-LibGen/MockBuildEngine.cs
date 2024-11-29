using Microsoft.Build.Framework;

namespace Test_LibGen;

public class MockBuildEngine : IBuildEngine {
    public bool BuildProjectFile(
        string projectFileName, string[] targetNames, 
        System.Collections.IDictionary globalProperties, 
        System.Collections.IDictionary targetOutputs) {
        throw new NotImplementedException();
    }
    public int ColumnNumberOfTaskNode => 0;
    public bool ContinueOnError => throw new NotImplementedException();
    public int LineNumberOfTaskNode => 0;
    public void LogCustomEvent(CustomBuildEventArgs e) {
    }
    public void LogErrorEvent(BuildErrorEventArgs e) {
    }
    public void LogMessageEvent(BuildMessageEventArgs e) {
    }
    public void LogWarningEvent(BuildWarningEventArgs e) {
    }
    public string ProjectFileOfTaskNode => "fake ProjectFileOfTaskNode";
}
