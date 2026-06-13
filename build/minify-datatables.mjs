// Minifies the OpenDataTables JS assets to *.min.js (with source maps).
import { build } from 'esbuild';

const dir = 'src/OpenDataTables.AspNetCore/wwwroot/js/';
const files = [
  'opendatatables-core',
  'opendatatables-datatable',
  'opendatatables-filtercard',
  'opendatatables-init'
];

await Promise.all(
  files.map((name) =>
    build({
      entryPoints: [`${dir}${name}.js`],
      outfile: `${dir}${name}.min.js`,
      minify: true,
      sourcemap: true,
      legalComments: 'inline',
      logLevel: 'info'
    })
  )
);

console.log(`Minified ${files.length} OpenDataTables asset(s).`);
