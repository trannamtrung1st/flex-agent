# Vendored upstream: cyberphone/json-canonicalization

| Field | Value |
| --- | --- |
| Source repository | https://github.com/cyberphone/json-canonicalization |
| Exact commit | `19d51d7fe467d4706a3ff08adf8a748f29fc21e0` |
| RFC reference | RFC 8785 JSON Canonicalization Scheme |

## Per-source license attribution

License terms are taken from the pinned upstream file headers. Distributable
third-party notices must preserve the upstream Apache-2.0 project license,
retained per-file headers, and the Lucent permissive notice in `NumberDToA.cs`.

| Upstream path | Local compiled path | License(s) |
| --- | --- | --- |
| `dotnet/jsoncanonicalizer/JsonCanonicalizer.cs` | `Upstream/CyberphoneJsonCanonicalization/JsonCanonicalizer.cs` | Apache-2.0 |
| `dotnet/es6numberserializer/NumberCachedPowers.cs` | `Upstream/CyberphoneJsonCanonicalization/Es6NumberSerialization/NumberCachedPowers.cs` | BSD-3-Clause (V8 project) |
| `dotnet/es6numberserializer/NumberDToA.cs` | `Upstream/CyberphoneJsonCanonicalization/Es6NumberSerialization/NumberDToA.cs` | MPL-2.0; Lucent permissive (David M. Gay, see file header) |
| `dotnet/es6numberserializer/NumberDiyFp.cs` | `Upstream/CyberphoneJsonCanonicalization/Es6NumberSerialization/NumberDiyFp.cs` | BSD-3-Clause (V8 project) |
| `dotnet/es6numberserializer/NumberDoubleHelper.cs` | `Upstream/CyberphoneJsonCanonicalization/Es6NumberSerialization/NumberDoubleHelper.cs` | BSD-3-Clause (V8 project) |
| `dotnet/es6numberserializer/NumberFastDToA.cs` | `Upstream/CyberphoneJsonCanonicalization/Es6NumberSerialization/NumberFastDToA.cs` | BSD-3-Clause (V8 project) |
| `dotnet/es6numberserializer/NumberFastDToABuilder.cs` | `Upstream/CyberphoneJsonCanonicalization/Es6NumberSerialization/NumberFastDToABuilder.cs` | MPL-2.0 |
| `dotnet/es6numberserializer/NumberToJson.cs` | `Upstream/CyberphoneJsonCanonicalization/Es6NumberSerialization/NumberToJson.cs` | Apache-2.0 |
| `LICENSE` | `Upstream/LICENSE` | Apache-2.0 |

## Pristine source snapshot (provenance only, not compiled)

| Upstream path | Local path |
| --- | --- |
| `dotnet/jsoncanonicalizer/JsonCanonicalizer.cs` | `Upstream/Pristine/CyberphoneJsonCanonicalization/JsonCanonicalizer.cs` |
| `dotnet/es6numberserializer/NumberCachedPowers.cs` | `Upstream/Pristine/CyberphoneJsonCanonicalization/Es6NumberSerialization/NumberCachedPowers.cs` |
| `dotnet/es6numberserializer/NumberDToA.cs` | `Upstream/Pristine/CyberphoneJsonCanonicalization/Es6NumberSerialization/NumberDToA.cs` |
| `dotnet/es6numberserializer/NumberDiyFp.cs` | `Upstream/Pristine/CyberphoneJsonCanonicalization/Es6NumberSerialization/NumberDiyFp.cs` |
| `dotnet/es6numberserializer/NumberDoubleHelper.cs` | `Upstream/Pristine/CyberphoneJsonCanonicalization/Es6NumberSerialization/NumberDoubleHelper.cs` |
| `dotnet/es6numberserializer/NumberFastDToA.cs` | `Upstream/Pristine/CyberphoneJsonCanonicalization/Es6NumberSerialization/NumberFastDToA.cs` |
| `dotnet/es6numberserializer/NumberFastDToABuilder.cs` | `Upstream/Pristine/CyberphoneJsonCanonicalization/Es6NumberSerialization/NumberFastDToABuilder.cs` |
| `dotnet/es6numberserializer/NumberToJson.cs` | `Upstream/Pristine/CyberphoneJsonCanonicalization/Es6NumberSerialization/NumberToJson.cs` |
| `LICENSE` | `Upstream/Pristine/LICENSE` |

## Official RFC/upstream test vectors

Copied from `testdata/input` and `testdata/output` at the pinned commit into
`tests/CanonicalJson/FlexAgent.CanonicalJson.Tests/Fixtures/UpstreamVectors/`.
Hashes and exact file inventory are recorded in `upstream-manifest.json` under
`officialVectors`.

## Local modifications (compiled sources only)

1. `JsonCanonicalizer`: `public class` → `internal class` so only
   `CanonicalJsonProcessor` can be used across assembly boundaries.
2. `NumberToJson`: `public static class` → `internal static class` for the same
   reason.

Pristine snapshots under `Upstream/Pristine/` remain byte-identical to the
pinned upstream commit. `InternalsVisibleTo` grants `FlexAgent.CanonicalJson.Tests`
access for upstream reference-vector verification only.

Compilation is explicit in `FlexAgent.CanonicalJson.csproj`: only
`Upstream/CyberphoneJsonCanonicalization/**/*.cs` is compiled from the vendored
tree. Integrity is enforced by `upstream-manifest.json` and `ProvenanceTests`.
