import { readFile } from "node:fs/promises";
import { readFileSync } from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { test } from "node:test";
import assert from "node:assert/strict";
import YAML from "yaml";
import Ajv from "ajv/dist/2020.js";
import addFormats from "ajv-formats";

const contractsRoot = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "..");

const representativeMappings = [
  {
    schemaComponent: "SessionStateEventEnvelopeV1",
    schemaPath: "schemas/v1/session/state-event-envelope.v1.schema.json",
  },
  {
    schemaComponent: "ResolvedExecutionManifestV1",
    schemaPath: "schemas/v1/manifest/resolved-execution-manifest.v1.schema.json",
  },
  {
    schemaComponent: "EvidenceLocatorV1",
    schemaPath: "schemas/v1/evidence/evidence-locator.v1.schema.json",
  },
  {
    schemaComponent: "AuditEventV1",
    schemaPath: "schemas/v1/audit/audit-event.v1.schema.json",
  },
  {
    schemaComponent: "SafeErrorResponseV1",
    schemaPath: "schemas/v1/transport/safe-error-response.v1.schema.json",
  },
  {
    schemaComponent: "SseSessionEventV1",
    schemaPath: "schemas/v1/transport/sse-event.v1.schema.json",
  },
  {
    schemaComponent: "GrantAccommodationCommandV2",
    schemaPath: "schemas/v2/enrollment/grant-accommodation-command.v2.schema.json",
  },
  {
    schemaComponent: "DecideAccommodationCommandV2",
    schemaPath: "schemas/v2/enrollment/decide-accommodation-command.v2.schema.json",
  },
  {
    schemaComponent: "RevokeAccommodationCommandV2",
    schemaPath: "schemas/v2/enrollment/revoke-accommodation-command.v2.schema.json",
  },
  {
    schemaComponent: "AccommodationMutationOutcomeV2",
    schemaPath: "schemas/v2/enrollment/accommodation-mutation-outcome.v2.schema.json",
  },
  {
    schemaComponent: "EnrollmentTimingV2",
    schemaPath: "schemas/v2/enrollment/enrollment-timing.v2.schema.json",
  },
  {
    schemaComponent: "MyWorkTimingV2",
    schemaPath: "schemas/v2/enrollment/my-work-timing.v2.schema.json",
  },
  {
    schemaComponent: "BeginIntakeCommandV2",
    schemaPath: "schemas/v2/submission/begin-intake-command.v2.schema.json",
  },
  {
    schemaComponent: "CompleteIntakeItemCommandV2",
    schemaPath: "schemas/v2/submission/complete-intake-item-command.v2.schema.json",
  },
  {
    schemaComponent: "IntakeRevisionCommandV2",
    schemaPath: "schemas/v2/submission/intake-revision-command.v2.schema.json",
  },
  {
    schemaComponent: "IntakeMutationOutcomeV2",
    schemaPath: "schemas/v2/submission/intake-mutation-outcome.v2.schema.json",
  },
  {
    schemaComponent: "MyWorkSubmissionV2",
    schemaPath: "schemas/v2/submission/my-work-submission.v2.schema.json",
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

const projectionNegativeCases = [
  {
    fixture: "fixtures/schema/v1/manifest/resolved-execution-manifest/invalid-active-with-seal.json",
    schemaComponent: "ResolvedExecutionManifestV1",
    canonicalSchemaPath: "schemas/v1/manifest/resolved-execution-manifest.v1.schema.json",
  },
  {
    fixture: "fixtures/schema/v1/manifest/resolved-execution-manifest/invalid-completed-without-seal.json",
    schemaComponent: "ResolvedExecutionManifestV1",
    canonicalSchemaPath: "schemas/v1/manifest/resolved-execution-manifest.v1.schema.json",
  },
  {
    fixture: "fixtures/schema/v1/evidence/evidence-locator/invalid-configuration-whole-item.json",
    schemaComponent: "EvidenceLocatorV1",
    canonicalSchemaPath: "schemas/v1/evidence/evidence-locator.v1.schema.json",
  },
  {
    fixture: "fixtures/schema/v1/session/command-envelope/invalid-pause-with-message-payload.json",
    schemaComponent: "SessionCommandEnvelopeV1",
    canonicalSchemaPath: "schemas/v1/session/command-envelope.v1.schema.json",
  },
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

  if (ref.startsWith("../schemas/")) {
    const filePath = path.resolve(contractsRoot, "projections", ref);
    return JSON.parse(readFileSync(filePath, "utf8"));
  }

  throw new Error(`Unsupported $ref: ${ref}`);
}

function normalizeNode(schema, context, owner = context.jsonSchema, seen = new Set()) {
  if (schema === false) {
    return false;
  }

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
        : ref.startsWith("../schemas/")
          ? resolved
        : ref.startsWith("#/$defs/")
          ? owner
          : owner;
    return normalizeNode(resolved, context, resolvedOwner, seen);
  }

  const normalized = {};
  for (const key of CONSTRAINT_KEYS) {
    if (schema[key] !== undefined) {
      normalized[key] = schema[key];
    }
  }

  if (schema.required) {
    normalized.required = [...schema.required].sort();
  }

  if (schema.properties) {
    normalized.properties = Object.fromEntries(
      Object.entries(schema.properties)
        .sort(([left], [right]) => left.localeCompare(right))
        .map(([key, value]) => [key, normalizeNode(value, context, owner, new Set(seen))]),
    );
  }

  if (schema.items) {
    normalized.items = normalizeNode(schema.items, context, owner, new Set(seen));
  }

  if (schema.allOf) {
    normalized.allOf = schema.allOf.map((branch) =>
      normalizeNode(branch, context, owner, new Set(seen)),
    );
  }

  if (schema.oneOf) {
    normalized.oneOf = schema.oneOf.map((branch) =>
      normalizeNode(branch, context, owner, new Set(seen)),
    );
  }

  if (schema.anyOf) {
    normalized.anyOf = schema.anyOf.map((branch) =>
      normalizeNode(branch, context, owner, new Set(seen)),
    );
  }

  if (schema.if) {
    normalized.if = normalizeNode(schema.if, context, owner, new Set(seen));
  }

  if (schema.then) {
    normalized.then = normalizeNode(schema.then, context, owner, new Set(seen));
  }

  if (schema.else) {
    normalized.else = normalizeNode(schema.else, context, owner, new Set(seen));
  }

  return normalized;
}

function normalizeComparableSchema(schema, context) {
  const base = { ...schema };
  delete base.$schema;
  delete base.$id;
  delete base.title;
  delete base.description;
  delete base.$defs;
  return normalizeNode(base, context, context.jsonSchema);
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

function assertPropertySetsMatch(openApi, json, label) {
  const openNames = new Set(Object.keys(openApi.properties ?? {}));
  const jsonNames = new Set(Object.keys(json.properties ?? {}));
  assert.deepEqual(
    [...openNames].sort(),
    [...jsonNames].sort(),
    `${label}: property name set mismatch`,
  );
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
      openApi.required ?? [],
      json.required,
      `${label}: required mismatch`,
    );
  }

  if (json.properties || openApi.properties) {
    assertPropertySetsMatch(openApi, json, label);
    for (const property of Object.keys(json.properties ?? {})) {
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

  if (json.allOf || openApi.allOf) {
    assert.ok(openApi.allOf && json.allOf, `${label}: allOf presence mismatch`);
    assert.equal(openApi.allOf.length, json.allOf.length, `${label}: allOf length mismatch`);
    for (let index = 0; index < json.allOf.length; index += 1) {
      assertConstraintParity(
        openApi.allOf[index],
        json.allOf[index],
        `${label}.allOf[${index}]`,
        context,
      );
    }
  }

  for (const conditionalKey of ["if", "then", "else"]) {
    if (json[conditionalKey] !== undefined) {
      assert.ok(openApi[conditionalKey], `${label}: OpenAPI missing ${conditionalKey}`);
      assertConstraintParity(
        openApi[conditionalKey],
        json[conditionalKey],
        `${label}.${conditionalKey}`,
        context,
      );
    }
  }
}

function inlineOpenApiComponent(componentName, components) {
  function resolve(node, stack = new Set()) {
    if (!node || typeof node !== "object") {
      return node;
    }

    if (Array.isArray(node)) {
      return node.map((entry) => resolve(entry, stack));
    }

    if (node.$ref?.startsWith("#/components/schemas/")) {
      const refName = node.$ref.slice("#/components/schemas/".length);
      assert.ok(components[refName], `Missing OpenAPI component ${refName}`);
      if (stack.has(refName)) {
        return { $ref: node.$ref };
      }
      stack.add(refName);
      const resolved = resolve(structuredClone(components[refName]), stack);
      stack.delete(refName);
      return resolved;
    }

    const resolved = {};
    for (const [key, value] of Object.entries(node)) {
      resolved[key] = resolve(value, stack);
    }
    return resolved;
  }

  return resolve(structuredClone(components[componentName]));
}

function compileOpenApiComponent(ajv, componentName, openApi) {
  return ajv.compile(inlineOpenApiComponent(componentName, openApi.components.schemas));
}

function createAjv() {
  const ajv = new Ajv({
    strict: false,
    allErrors: true,
    validateFormats: true,
  });
  addFormats(ajv);
  return ajv;
}

async function registerCanonicalValidators(ajv, schemaPaths) {
  const primitives = await loadJson("schemas/v1/common/primitives.v1.schema.json");
  ajv.addSchema(primitives);
  const validators = new Map();
  for (const schemaPath of schemaPaths) {
    const schema = await loadJson(schemaPath);
    ajv.addSchema(schema);
    validators.set(schemaPath, ajv.getSchema(schema.$id));
  }
  return validators;
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
    const openApiSchema = openApi.components.schemas[mapping.schemaComponent];
    assert.ok(openApiSchema, `Missing OpenAPI component ${mapping.schemaComponent}`);
    assertConstraintParity(openApiSchema, jsonSchema, mapping.schemaComponent, context);
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

  for (const [jsonDef, schemaComponent] of commandVariantMappings) {
    assertConstraintParity(
      openApi.components.schemas[schemaComponent],
      jsonSchema.$defs[jsonDef],
      schemaComponent,
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

test("OpenAPI projection rejects canonical negative fixtures", async () => {
  const openApi = YAML.parse(
    await readFile(path.join(contractsRoot, "projections/openapi.v3.1.yaml"), "utf8"),
  );
  const ajv = createAjv();
  const canonicalValidators = await registerCanonicalValidators(ajv, [
    ...new Set(projectionNegativeCases.map((entry) => entry.canonicalSchemaPath)),
  ]);

  for (const negativeCase of projectionNegativeCases) {
    const instance = await loadJson(negativeCase.fixture);
    const openApiValidate = compileOpenApiComponent(
      ajv,
      negativeCase.schemaComponent,
      openApi,
    );
    const canonicalValidate = canonicalValidators.get(negativeCase.canonicalSchemaPath);
    assert.ok(canonicalValidate, `Missing canonical validator for ${negativeCase.canonicalSchemaPath}`);

    assert.equal(
      openApiValidate(instance),
      false,
      `${negativeCase.fixture} should be rejected by OpenAPI projection`,
    );
    assert.equal(
      canonicalValidate(instance),
      false,
      `${negativeCase.fixture} should be rejected by canonical schema`,
    );
  }
});
