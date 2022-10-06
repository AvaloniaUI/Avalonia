require("esbuild").build({
    entryPoints: [
        "./modules/avalonia.ts",
        "./modules/storage.ts"
    ],
    outdir: "../wwwroot",
    bundle: true,
    minify: false,
    format: "esm",
    target: "es2016",
    platform: "browser",
    sourcemap: "linked",
    loader: { ".ts": "ts" }
})
    .then(() => console.log("⚡ Done"))
    .catch(() => process.exit(1));
