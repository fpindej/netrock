#!/usr/bin/env node
// PostToolUse hook: auto-formats files after Write|Edit operations

import { readFileSync, existsSync, readdirSync } from 'fs';
import { execSync } from 'child_process';
import { resolve, extname } from 'path';

let input;
try {
  input = JSON.parse(readFileSync(0, 'utf8'));
} catch {
  process.exit(0);
}

const filePath = input?.tool_input?.file_path;
if (!filePath || !existsSync(filePath)) process.exit(0);

const projectDir = process.env.CLAUDE_PROJECT_DIR;
if (!projectDir) process.exit(0);

const ext = extname(filePath);

try {
  if (ext === '.cs') {
    const backendDir = resolve(projectDir, 'src/backend');
    const slnx = readdirSync(backendDir).find((f) => f.endsWith('.slnx'));
    if (slnx) {
      execSync(
        `dotnet format "${resolve(backendDir, slnx)}" --include "${filePath}" --no-restore`,
        { stdio: 'ignore' },
      );
    }
  } else if (['.ts', '.svelte', '.js', '.json', '.css', '.html'].includes(ext)) {
    const frontendDir = resolve(projectDir, 'src/frontend');
    const prettierBin = resolve(frontendDir, 'node_modules/.bin/prettier');
    if (existsSync(prettierBin)) {
      execSync(`pnpm exec prettier --write "${filePath}"`, {
        cwd: frontendDir,
        stdio: 'ignore',
      });
    }
  }
} catch {
  // Formatting is best-effort
}

process.exit(0);
