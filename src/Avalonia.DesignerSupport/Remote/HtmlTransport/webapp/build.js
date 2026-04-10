import { build } from 'esbuild';
import { compress } from 'esbuild-plugin-compress';
import { readFileSync } from 'fs';
import { resolve, basename } from 'path';

// Bundle the TypeScript entry point
build({
    entryPoints: ['src/index.tsx'],
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
    entryNames: '[name]-[hash]',
    loader: {".ts": "tsx"},
    plugins: [
        {
            name: 'html',
            setup(pluginBuild) {
                pluginBuild.onEnd(result => {
                    if (result.errors.length > 0) return;

                    // Find the JS output file name
                    const jsFile = result.outputFiles.find(f => f.path.endsWith('.js'));
                    const jsName = basename(jsFile.path);

                    // Read the HTML template and replace the script reference
                    let html = readFileSync('src/index.html', 'utf8');
                    html = html.replace('src="index.tsx"', `src="${jsName}" type="module"`);

                    // Add the HTML to outputFiles so the compress plugin handles it
                    result.outputFiles.push({
                        path: resolve('./build/index.html'),
                        contents: new TextEncoder().encode(html),
                        text: html,
                    });
                });
            }
        },
        compress({
            brotli: false,
            gzip: true
        })
    ]
  })
  .then(() => console.log("⚡ Done"))
  .catch(() => process.exit(1));
