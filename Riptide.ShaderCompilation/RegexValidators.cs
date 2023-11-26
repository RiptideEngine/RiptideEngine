using System.Text.RegularExpressions;

namespace Riptide.ShaderCompilation;

public static partial class RegexValidators {
    [GeneratedRegex("[_a-zA-Z]\\w*", RegexOptions.Compiled)]
    public static partial Regex GetIdentifierRegex();
    
    // TODO: lib compilation.
    [GeneratedRegex("[vs|ps|gs|hs|ds|cs|as|ms]_[0-9]_[0-9]", RegexOptions.Compiled)]
    public static partial Regex GetTargetRegex();
}