namespace Riptide.ShaderCompilation;

public static partial class ArgumentValidator {
    [GeneratedRegex("^[a-zA-Z_]\\w*$", RegexOptions.Compiled)]
    public static partial Regex GetEntrypointValidator();
    
    [GeneratedRegex("^([vphdc]s_6_[0-7]|lib_6_[3-7])$", RegexOptions.Compiled)]
    public static partial Regex GetDxcTargetValidator();
}