require("esbuild").build({
    entryPoints: [
        "./modules/avalonia.ts",
        "./modules/storage.ts",
        "./modules/sw.ts"
    ],
    outdir: "../staticwebassets",
    bundle: true,
    minify: true,
    format: "esm",
    target: "es2018",
    platform: "browser",
    sourcemap: "linked",
    loader: { ".ts": "ts" }
})
    .then(() => console.log("⚡ Done"))
    .catch(() => process.exit(1));
