# Vendored upstream: cyberphone/json-canonicalization

| Field | Value |
| --- | --- |
| Source repository | https://github.com/cyberphone/json-canonicalization |
| Exact commit | `19d51d7fe467d4706a3ff08adf8a748f29fc21e0` |
| License | Apache License 2.0 (see `Upstream/LICENSE`) |
| RFC reference | RFC 8785 JSON Canonicalization Scheme |

## Copied files

| Upstream path | Local path |
| --- | --- |
| `dotnet/jsoncanonicalizer/JsonCanonicalizer.cs` | `Upstream/CyberphoneJsonCanonicalization/JsonCanonicalizer.cs` |
| `dotnet/es6numberserializer/NumberCachedPowers.cs` | `Upstream/CyberphoneJsonCanonicalization/Es6NumberSerialization/NumberCachedPowers.cs` |
| `dotnet/es6numberserializer/NumberDToA.cs` | `Upstream/CyberphoneJsonCanonicalization/Es6NumberSerialization/NumberDToA.cs` |
| `dotnet/es6numberserializer/NumberDiyFp.cs` | `Upstream/CyberphoneJsonCanonicalization/Es6NumberSerialization/NumberDiyFp.cs` |
| `dotnet/es6numberserializer/NumberDoubleHelper.cs` | `Upstream/CyberphoneJsonCanonicalization/Es6NumberSerialization/NumberDoubleHelper.cs` |
| `dotnet/es6numberserializer/NumberFastDToA.cs` | `Upstream/CyberphoneJsonCanonicalization/Es6NumberSerialization/NumberFastDToA.cs` |
| `dotnet/es6numberserializer/NumberFastDToABuilder.cs` | `Upstream/CyberphoneJsonCanonicalization/Es6NumberSerialization/NumberFastDToABuilder.cs` |
| `dotnet/es6numberserializer/NumberToJson.cs` | `Upstream/CyberphoneJsonCanonicalization/Es6NumberSerialization/NumberToJson.cs` |
| `LICENSE` | `Upstream/LICENSE` |

Official RFC/upstream test vectors used by `FlexAgent.CanonicalJson.Tests` are copied
under `tests/CanonicalJson/FlexAgent.CanonicalJson.Tests/Fixtures/UpstreamVectors/`
from `testdata/input` and `testdata/output` at the same commit.

## Local modifications

None. Upstream C# sources are copied verbatim. Application-owned parsing limits,
failure categories, digest helpers, and tests live outside `Upstream/`.

Integrity of copied files is enforced by `upstream-manifest.json` and
`ProvenanceTests` in `FlexAgent.CanonicalJson.Tests`.
