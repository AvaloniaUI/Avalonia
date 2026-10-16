require("esbuild").build({
    entryPoints: [
        "./modules/avalonia.ts",
        "./modules/storage.ts",
        "./modules/avalonia-sw.ts"
    ],
    outdir: "../wwwroot",
    bundle: true,
    minify: true,
    format: "esm",
    target: "es2020",
    platform: "browser",
    sourcemap: "linked",
    loader: { ".ts": "ts" }
})
    .then(() => console.log("⚡ Done"))
    .catch(() => process.exit(1));
