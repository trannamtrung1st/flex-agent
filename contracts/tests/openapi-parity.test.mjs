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

function collectPropertyNames(schema) {
  if (!schema || typeof schema !== "object") {
    return new Set();
  }

  if (schema.properties) {
    return new Set(Object.keys(schema.properties));
  }

  if (schema.allOf) {
    const names = new Set();
    for (const branch of schema.allOf) {
      for (const name of collectPropertyNames(branch)) {
        names.add(name);
      }
    }
    return names;
  }

  return new Set();
}

function collectRequired(schema) {
  if (!schema || typeof schema !== "object") {
    return new Set();
  }

  const required = new Set(schema.required ?? []);
  if (schema.allOf) {
    for (const branch of schema.allOf) {
      for (const name of collectRequired(branch)) {
        required.add(name);
      }
    }
  }

  return required;
}

function assertSchemaParity(openApiSchema, jsonSchema, label) {
  const openApiProperties = collectPropertyNames(openApiSchema);
  const jsonProperties = collectPropertyNames(jsonSchema);
  for (const property of jsonProperties) {
    assert.ok(
      openApiProperties.has(property),
      `${label}: OpenAPI missing property ${property}`,
    );
  }

  const openApiRequired = collectRequired(openApiSchema);
  const jsonRequired = collectRequired(jsonSchema);
  for (const property of jsonRequired) {
    assert.ok(
      openApiRequired.has(property),
      `${label}: OpenAPI missing required property ${property}`,
    );
  }

  assert.equal(
    openApiSchema.additionalProperties ?? true,
    jsonSchema.additionalProperties ?? true,
    `${label}: additionalProperties mismatch`,
  );
}

test("OpenAPI representative components mirror canonical JSON Schema structure", async () => {
  const openApi = YAML.parse(
    await readFile(path.join(contractsRoot, "projections/openapi.v3.1.yaml"), "utf8"),
  );

  for (const mapping of representativeMappings) {
    const jsonSchema = JSON.parse(
      await readFile(path.join(contractsRoot, mapping.schemaPath), "utf8"),
    );
    const openApiSchema = openApi.components.schemas[mapping.openApiComponent];
    assert.ok(openApiSchema, `Missing OpenAPI component ${mapping.openApiComponent}`);
    assertSchemaParity(openApiSchema, jsonSchema, mapping.openApiComponent);
  }
});

test("OpenAPI command projection advertises all command variants", async () => {
  const openApi = YAML.parse(
    await readFile(path.join(contractsRoot, "projections/openapi.v3.1.yaml"), "utf8"),
  );
  const commandUnion = openApi.components.schemas.SessionCommandEnvelopeV1;
  assert.ok(commandUnion.oneOf, "SessionCommandEnvelopeV1 must be a oneOf union");
  const refs = commandUnion.oneOf.map((entry) => entry.$ref);
  assert.deepEqual(
    refs.sort(),
    [
      "#/components/schemas/SessionCompleteCommandV1",
      "#/components/schemas/SessionMessageSendCommandV1",
      "#/components/schemas/SessionPauseCommandV1",
      "#/components/schemas/SessionReconcileCommandV1",
      "#/components/schemas/SessionResumeCommandV1",
      "#/components/schemas/SessionTerminateCommandV1",
    ].sort(),
  );
});

test("OpenAPI int64 wire fields use string projection", async () => {
  const openApi = YAML.parse(
    await readFile(path.join(contractsRoot, "projections/openapi.v3.1.yaml"), "utf8"),
  );
  const stateEvent = openApi.components.schemas.SessionStateEventEnvelopeV1;
  assert.equal(stateEvent.properties.session_sequence.$ref, "#/components/schemas/Int64WireString");
  const int64 = openApi.components.schemas.Int64WireString;
  assert.equal(int64.type, "string");
  assert.match(int64.pattern, /\^/);
});
