import { build } from 'esbuild';
import htmlPlugin from '@chialab/esbuild-plugin-html';
import { compress } from 'esbuild-plugin-compress';

build({
    entryPoints: ['src/index.html'],
    chunkNames: '[name]-[hash]',
    logLevel: "warning",
    outdir: "./build",
    bundle: true,
    treeShaking: true,
    minify: true,
    write: false,
    format: "esm",
    target: "es6",
    platform: "browser",
    sourcemap: "linked",
    loader: {".ts": "tsx"},
    plugins: [
        htmlPlugin(),
        compress({
            brotli: false,
            gzip: true
        })
    ]
  })
  .then(() => console.log("⚡ Done"))
  .catch(() => process.exit(1));