import { describe, expect, it } from "vitest";
import { operateFrameClass, resolveOperateFrameInset } from "./operateFrameClass";

describe("operateFrameClass", () => {
  it("maps each generic frame variant to its class bundle", () => {
    expect(operateFrameClass("record")).toBe("record-frame");
    expect(operateFrameClass("registry")).toBe("datatable-frame registry-frame");
    expect(operateFrameClass("datatable")).toBe("datatable-frame");
    expect(operateFrameClass("ceremony")).toBe("ceremony-frame");
  });

  it("keeps additive frameClassName when frame is set", () => {
    expect(operateFrameClass("datatable", "manifest")).toBe("datatable-frame manifest");
  });

  it("defaults registry and datatable frames to flush inset", () => {
    expect(resolveOperateFrameInset("registry")).toBe("flush");
    expect(resolveOperateFrameInset("datatable")).toBe("flush");
    expect(resolveOperateFrameInset("record")).toBe("default");
    expect(resolveOperateFrameInset("ceremony")).toBe("default");
  });

  it("prefers an explicit frameInset override", () => {
    expect(resolveOperateFrameInset("registry", "default")).toBe("default");
  });
});
