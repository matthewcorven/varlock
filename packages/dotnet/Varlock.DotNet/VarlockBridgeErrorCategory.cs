namespace Varlock.DotNet;

public enum VarlockBridgeErrorCategory
{
  ExecutableNotFound,
  ExecutableVersionMismatch,
  SchemaMissing,
  SchemaInvalid,
  ResolutionFailed,
  PluginLoadFailed,
  BridgeInternalError,
}