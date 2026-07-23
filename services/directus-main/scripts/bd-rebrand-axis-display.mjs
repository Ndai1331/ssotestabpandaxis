#!/usr/bin/env node
/**
 * BD lab: replace standalone word Directus → Axis in specific files.
 * Does NOT touch DirectusUser / @directus / etc.
 */
import fs from 'node:fs';

const files = process.argv.slice(2);
if (files.length === 0) {
	console.error('Usage: node bd-rebrand-axis-display.mjs <file>...');
	process.exit(1);
}

let total = 0;
for (const file of files) {
	const before = fs.readFileSync(file, 'utf8');
	// Word-boundary Directus not followed/preceded by identifier chars
	const after = before.replace(/(?<![A-Za-z0-9_])Directus(?![A-Za-z0-9_])/g, 'Axis');
	if (after === before) {
		console.log(`unchanged: ${file}`);
		continue;
	}
	const count = (before.match(/(?<![A-Za-z0-9_])Directus(?![A-Za-z0-9_])/g) || []).length;
	fs.writeFileSync(file, after);
	total += count;
	console.log(`updated: ${file} (${count})`);
}
console.log(`total Directus→Axis: ${total}`);
