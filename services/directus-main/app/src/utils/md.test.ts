// @vitest-environment jsdom
import { expect, test } from 'vitest';
import { md } from './md.js';

test.each([
	{ value: 'test', expected: '<p>test</p>\n' },
	{
		value: `[Axis](https://directus.example.com)`,
		expected: '<p><a target="_self" href="https://directus.example.com">Axis</a></p>\n',
	},
	{
		value: `[Axis](https://directus.example.com)`,
		expected: '<p><a target="_blank" href="https://directus.example.com" rel="noopener noreferrer">Axis</a></p>\n',
		options: { target: '_blank' } as const,
	},
	{ value: `test<script>alert('alert')</script>`, expected: '<p>test</p>\n' },
])('should sanitize "$str" into "$expected"', ({ value, expected, options }) => {
	expect(md(value, options)).toBe(expected);
});
