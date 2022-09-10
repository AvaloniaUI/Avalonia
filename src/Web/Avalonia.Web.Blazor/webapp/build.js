require("esbuild").build({
    entryPoints: [
        "./modules/Avalonia.ts",
        "./modules/Storage.ts"
    ],
    outdir: "../wwwroot",
    bundle: true,
    minify: true,
    format: "esm",
    target: "es2016",
    platform: "browser",
    sourcemap: "linked",
    loader: {".ts": "ts"}
  })
  .then(() => console.log("⚡ Done"))
  .catch(() => process.exit(1));