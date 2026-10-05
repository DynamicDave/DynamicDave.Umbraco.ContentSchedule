export const manifests: Array<UmbExtensionManifest> = [
  {
    name: "Dynamic Dave Umbraco Content Schedule Entrypoint",
    alias: "DynamicDave.Umbraco.ContentSchedule.Entrypoint",
    type: "backofficeEntryPoint",
    js: () => import("./entrypoint.js"),
  },
];
