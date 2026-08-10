import { readFile } from "node:fs/promises";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { test } from "node:test";
import assert from "node:assert/strict";
import YAML from "yaml";

const contractsRoot = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "..");

const representativeMappings = [
  {
    openApiComponent: "SessionStateEventEnvelopeV1",
    schemaPath: "schemas/v1/session/state-event-envelope.v1.schema.json",
  },
  {
    openApiComponent: "ResolvedExecutionManifestV1",
    schemaPath: "schemas/v1/manifest/resolved-execution-manifest.v1.schema.json",
  },
  {
    openApiComponent: "EvidenceLocatorV1",
    schemaPath: "schemas/v1/evidence/evidence-locator.v1.schema.json",
  },
  {
    openApiComponent: "AuditEventV1",
    schemaPath: "schemas/v1/audit/audit-event.v1.schema.json",
  },
  {
    openApiComponent: "SafeErrorResponseV1",
    schemaPath: "schemas/v1/transport/safe-error-response.v1.schema.json",
  },
  {
    openApiComponent: "SseSessionEventV1",
    schemaPath: "schemas/v1/transport/sse-event.v1.schema.json",
  },
];

const commandVariantMappings = [
  ["message_send_command", "SessionMessageSendCommandV1"],
  ["pause_command", "SessionPauseCommandV1"],
  ["resume_command", "SessionResumeCommandV1"],
  ["complete_command", "SessionCompleteCommandV1"],
  ["terminate_command", "SessionTerminateCommandV1"],
  ["reconcile_command", "SessionReconcileCommandV1"],
];

const CONSTRAINT_KEYS = [
  "type",
  "const",
  "enum",
  "minimum",
  "maximum",
  "minLength",
  "maxLength",
  "minItems",
  "maxItems",
  "pattern",
  "format",
  "additionalProperties",
  "maxProperties",
];

async function loadJson(relativePath) {
  return JSON.parse(await readFile(path.join(contractsRoot, relativePath), "utf8"));
}

function resolveRef(ref, context, owner) {
  if (ref.startsWith("#/$defs/")) {
    const name = ref.slice("#/$defs/".length);
    const ownerDef = owner?.$defs?.[name];
    if (ownerDef) return ownerDef;
    const jsonDef = context.jsonSchema?.$defs?.[name];
    if (jsonDef) return jsonDef;
    const primitiveDef = context.primitives?.$defs?.[name];
    assert.ok(primitiveDef, `Missing $defs/${name}`);
    return primitiveDef;
  }

  if (ref.startsWith("#/components/schemas/")) {
    const name = ref.slice("#/components/schemas/".length);
    const schema = context.openApiComponents[name];
    assert.ok(schema, `Missing OpenAPI component ${name}`);
    return schema;
  }

  if (ref.includes("primitives.v1.schema.json#/$defs/")) {
    const name = ref.split("#/$defs/")[1];
    const schema = context.primitives.$defs?.[name];
    assert.ok(schema, `Missing primitive $defs/${name}`);
    return schema;
  }

  throw new Error(`Unsupported $ref: ${ref}`);
}

function dereference(schema, context, owner = context.jsonSchema, seen = new Set()) {
  if (!schema || typeof schema !== "object") {
    return schema;
  }

  if (schema.$ref) {
    const ref = schema.$ref;
    if (seen.has(ref)) {
      return { $ref: ref };
    }
    seen.add(ref);
    const resolved = resolveRef(ref, context, owner);
    const resolvedOwner = ref.includes("primitives.v1.schema.json")
      ? context.primitives
      : ref.startsWith("#/components/schemas/")
        ? context.openApiComponents
        : owner;
    return dereference(resolved, context, resolvedOwner, seen);
  }

  if (schema.allOf) {
    return mergeAllOf(
      schema.allOf.map((branch) => dereference(branch, context, owner, new Set(seen))),
    );
  }

  if (schema.oneOf) {
    return {
      oneOf: schema.oneOf.map((branch) => dereference(branch, context, owner, new Set(seen))),
    };
  }

  const copy = { ...schema };
  if (schema.properties) {
    copy.properties = Object.fromEntries(
      Object.entries(schema.properties).map(([key, value]) => [
        key,
        dereference(value, context, owner, new Set(seen)),
      ]),
    );
  }

  if (schema.items) {
    copy.items = dereference(schema.items, context, owner, new Set(seen));
  }

  return copy;
}

function mergeAllOf(branches) {
  const merged = {
    type: undefined,
    required: [],
    properties: {},
    additionalProperties: undefined,
  };

  for (const branch of branches) {
    if (!branch || typeof branch !== "object") continue;
    if (branch.type) merged.type = branch.type;
    if (branch.required) merged.required.push(...branch.required);
    if (branch.properties) Object.assign(merged.properties, branch.properties);
    if (branch.additionalProperties !== undefined) {
      merged.additionalProperties = branch.additionalProperties;
    }
    for (const key of CONSTRAINT_KEYS) {
      if (branch[key] !== undefined) merged[key] = branch[key];
    }
  }

  merged.required = [...new Set(merged.required)].sort();
  return merged;
}

function normalizeComparableSchema(schema, context) {
  const base = { ...schema };
  delete base.$schema;
  delete base.$id;
  delete base.title;
  delete base.description;
  delete base.allOf;
  delete base.if;
  delete base.then;
  return dereference(base, context, context.jsonSchema);
}

function commandTypeOf(schema) {
  return schema.properties?.command_type?.const ?? null;
}

function branchFingerprint(schema) {
  const picked = {};
  for (const key of [...CONSTRAINT_KEYS, "$ref"]) {
    if (schema?.[key] !== undefined) picked[key] = schema[key];
  }
  return JSON.stringify(picked);
}

function assertConstraintParity(openApiSchema, jsonSchema, label, context) {
  const openApi = normalizeComparableSchema(openApiSchema, {
    ...context,
    jsonSchema: openApiSchema,
  });
  const json = normalizeComparableSchema(jsonSchema, context);

  if (openApi.oneOf || json.oneOf) {
    assert.ok(openApi.oneOf && json.oneOf, `${label}: oneOf presence mismatch`);
    assert.equal(openApi.oneOf.length, json.oneOf.length, `${label}: oneOf length mismatch`);

    if (json.oneOf.some((branch) => commandTypeOf(branch))) {
      for (const jsonBranch of json.oneOf) {
        const jsonType = commandTypeOf(jsonBranch);
        const match = openApi.oneOf.find((branch) => commandTypeOf(branch) === jsonType);
        assert.ok(match, `${label}: missing OpenAPI oneOf branch for ${jsonType}`);
        assertConstraintParity(match, jsonBranch, `${label}.${jsonType}`, context);
      }
      return;
    }

    const openFingerprints = openApi.oneOf.map(branchFingerprint).sort();
    const jsonFingerprints = json.oneOf.map(branchFingerprint).sort();
    assert.deepEqual(openFingerprints, jsonFingerprints, `${label}: oneOf branch mismatch`);
    return;
  }

  for (const key of CONSTRAINT_KEYS) {
    if (json[key] !== undefined) {
      assert.deepEqual(openApi[key], json[key], `${label}: mismatch for ${key}`);
    }
  }

  if (json.required) {
    assert.deepEqual(
      [...(openApi.required ?? [])].sort(),
      [...json.required].sort(),
      `${label}: required mismatch`,
    );
  }

  if (json.properties) {
    for (const property of Object.keys(json.properties)) {
      assert.ok(openApi.properties?.[property], `${label}: OpenAPI missing property ${property}`);
      assertConstraintParity(
        openApi.properties[property],
        json.properties[property],
        `${label}.${property}`,
        context,
      );
    }
  }

  if (json.items) {
    assert.ok(openApi.items, `${label}: OpenAPI missing items schema`);
    assertConstraintParity(openApi.items, json.items, `${label}[]`, context);
  }
}

test("OpenAPI representative components mirror canonical JSON Schema constraints", async () => {
  const openApi = YAML.parse(
    await readFile(path.join(contractsRoot, "projections/openapi.v3.1.yaml"), "utf8"),
  );
  const primitives = await loadJson("schemas/v1/common/primitives.v1.schema.json");

  for (const mapping of representativeMappings) {
    const jsonSchema = await loadJson(mapping.schemaPath);
    const context = {
      openApiComponents: openApi.components.schemas,
      jsonSchema,
      primitives,
    };
    const openApiSchema = openApi.components.schemas[mapping.openApiComponent];
    assert.ok(openApiSchema, `Missing OpenAPI component ${mapping.openApiComponent}`);
    assertConstraintParity(openApiSchema, jsonSchema, mapping.openApiComponent, context);
  }
});

test("OpenAPI command variants mirror canonical command envelope constraints", async () => {
  const openApi = YAML.parse(
    await readFile(path.join(contractsRoot, "projections/openapi.v3.1.yaml"), "utf8"),
  );
  const jsonSchema = await loadJson("schemas/v1/session/command-envelope.v1.schema.json");
  const primitives = await loadJson("schemas/v1/common/primitives.v1.schema.json");
  const context = {
    openApiComponents: openApi.components.schemas,
    jsonSchema,
    primitives,
  };

  for (const [jsonDef, openApiComponent] of commandVariantMappings) {
    assertConstraintParity(
      openApi.components.schemas[openApiComponent],
      jsonSchema.$defs[jsonDef],
      openApiComponent,
      context,
    );
  }
});

test("OpenAPI int64 wire primitives enforce signed-int64 bounds and positive semantics", async () => {
  const openApi = YAML.parse(
    await readFile(path.join(contractsRoot, "projections/openapi.v3.1.yaml"), "utf8"),
  );
  const primitives = await loadJson("schemas/v1/common/primitives.v1.schema.json");
  const context = {
    openApiComponents: openApi.components.schemas,
    jsonSchema: primitives,
    primitives,
  };

  assertConstraintParity(
    openApi.components.schemas.PositiveInt64WireString,
    primitives.$defs.positive_int64_wire_string,
    "PositiveInt64WireString",
    context,
  );
  assertConstraintParity(
    openApi.components.schemas.NonnegativeInt64WireString,
    primitives.$defs.nonnegative_int64_wire_string,
    "NonnegativeInt64WireString",
    context,
  );
});
