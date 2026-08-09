# Vendored upstream: cyberphone/json-canonicalization

| Field | Value |
| --- | --- |
| Source repository | https://github.com/cyberphone/json-canonicalization |
| Exact commit | `19d51d7fe467d4706a3ff08adf8a748f29fc21e0` |
| RFC reference | RFC 8785 JSON Canonicalization Scheme |

## License composition

| Component | Upstream path(s) | License(s) |
| --- | --- | --- |
| JSON canonicalizer | `dotnet/jsoncanonicalizer/JsonCanonicalizer.cs` | Apache-2.0 (`Upstream/LICENSE`, file header) |
| ES6 number serialization | `dotnet/es6numberserializer/*.cs` | Apache-2.0 (`NumberToJson.cs` header); BSD-3-Clause terms in V8-derived files (`NumberCachedPowers.cs`, `NumberDToA.cs`, `NumberDiyFp.cs`, `NumberDoubleHelper.cs`, `NumberFastDToA.cs`); MPL-2.0 header in `NumberFastDToA.cs` |
| Upstream project license | `LICENSE` | Apache-2.0 |

Distributable third-party notices must preserve the upstream Apache-2.0 terms and the per-file V8/Mozilla headers retained in the copied ES6 serializer sources.

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

## Compiled sources (built into `FlexAgent.CanonicalJson`)

| Upstream path | Local path |
| --- | --- |
| `dotnet/jsoncanonicalizer/JsonCanonicalizer.cs` | `Upstream/CyberphoneJsonCanonicalization/JsonCanonicalizer.cs` |
| `dotnet/es6numberserializer/*.cs` | `Upstream/CyberphoneJsonCanonicalization/Es6NumberSerialization/*.cs` |
| `LICENSE` | `Upstream/LICENSE` |

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

Integrity is enforced by `upstream-manifest.json` and `ProvenanceTests`.
