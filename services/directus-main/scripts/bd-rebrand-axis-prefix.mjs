#!/usr/bin/env node
/**
 * BD lab rebrand script: directus_ → axis_ (tables/collections)
 * Run from services/directus-main
 */
import fs from 'node:fs';
import path from 'node:path';

const ROOT = process.cwd();
const EXCLUDE_DIRS = new Set([
	'node_modules',
	'.git',
	'dist',
	'.turbo',
	'coverage',
	'.cache',
	'uploads',
	'data',
]);
const INCLUDE_EXT = new Set([
	'.ts',
	'.js',
	'.vue',
	'.yaml',
	'.yml',
	'.json',
	'.sql',
	'.md',
	'.txt',
	'.html',
	'.css',
	'.scss',
]);

function walk(dir, out = []) {
	for (const entry of fs.readdirSync(dir, { withFileTypes: true })) {
		if (entry.name.startsWith('.') && entry.name !== '.changeset') continue;
		const full = path.join(dir, entry.name);
		if (entry.isDirectory()) {
			if (EXCLUDE_DIRS.has(entry.name)) continue;
			walk(full, out);
		} else if (INCLUDE_EXT.has(path.extname(entry.name))) {
			out.push(full);
		}
	}
	return out;
}

const files = walk(ROOT);
let changed = 0;
let replacements = 0;

for (const file of files) {
	const before = fs.readFileSync(file, 'utf8');
	if (!before.includes('directus_')) continue;
	const after = before.replaceAll('directus_', 'axis_');
	if (after === before) continue;
	const count = before.split('directus_').length - 1;
	fs.writeFileSync(file, after);
	changed++;
	replacements += count;
}

console.log(JSON.stringify({ filesChanged: changed, replacements }, null, 2));
