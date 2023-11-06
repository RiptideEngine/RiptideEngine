namespace RiptideDatabase;

public enum ImportingError {
    /// <summary>
    /// No error, resource import successfully.
    /// </summary>
    None = 0,

    /// <summary>
    /// Unknown error.
    /// </summary>
    Unknown = -1,

    /// <summary>
    /// Unregistered protocol string used.
    /// </summary>
    UnknownProtocol = 1,

    /// <summary>
    /// Null resource type.
    /// </summary>
    NullResourceType = 2,

    /// <summary>
    /// Resource identifier catalogue cannot map resource GUID into resource path.
    /// </summary>
    UnmappedResourceGuid = 3,

    /// <summary>
    /// Resource identifier catalogue cannot map resource path to resource GUID.
    /// </summary>
    UnmappedResourcePath = 4,

    /// <summary>
    /// Stream provider returns null resource stream.
    /// </summary>
    NullResourceStream = 5,

    /// <summary>
    /// <see cref="ImportingResult"/> contains no error, but also contains no resource object.
    /// </summary>
    EmptyResult = 6,

    /// <summary>
    /// Resource data is valid, but contains an unsupported property.
    /// </summary>
    UnsupportedProperties = 7,

    MissingCatalogue = 256,
    MissingProtocolProvider = 257,
    MissingResourceProvider = 258,

    /// <summary>
    /// Missing API that handle resource importing.
    /// </summary>
    MissingImportingAPI = 259,

    /// <summary>
    /// Missing API that handle operations other than resource importing.
    /// </summary>
    MissingAPI = 260,

    CorruptedResourceData = 513,
}

public struct ImportingResult {
    public object? Result;
    public ImportingError Error;

    public readonly bool HasError => Error != ImportingError.None;

    public static ImportingResult FromResult(object result) => new() { Result = result, };
    public static ImportingResult FromError(ImportingError error) => new() { Error = error };
}