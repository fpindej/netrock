#!/usr/bin/env node
// Stop hook: warns if there are uncommitted changes when Claude finishes

import { execSync } from 'child_process';

const projectDir = process.env.CLAUDE_PROJECT_DIR;
if (!projectDir) process.exit(0);

const git = (cmd) =>
  execSync(cmd, { cwd: projectDir, encoding: 'utf8', stdio: ['pipe', 'pipe', 'pipe'] });

let warnings = '';

try {
  // Check for uncommitted changes
  let hasChanges = false;
  try {
    git('git diff --quiet');
    git('git diff --cached --quiet');
  } catch {
    hasChanges = true;
  }

  if (hasChanges) {
    const staged = git('git diff --cached --name-only').trim();
    const unstaged = git('git diff --name-only').trim();
    const files = new Set(
      [...staged.split('\n'), ...unstaged.split('\n')].filter(Boolean),
    );
    if (files.size > 0) {
      warnings += `Uncommitted changes in ${files.size} file(s). Consider committing before ending. `;
    }
  }

  // Check for untracked files in src/
  const untracked = git(
    'git ls-files --others --exclude-standard -- src/',
  ).trim();
  if (untracked) {
    const count = untracked.split('\n').filter(Boolean).length;
    warnings += `${count} untracked file(s) in src/. May be new files that need committing.`;
  }
} catch {
  // Git commands failed - skip
}

if (warnings) {
  console.log(
    JSON.stringify({
      hookSpecificOutput: { hookEventName: 'Stop' },
      systemMessage: `Quality gate: ${warnings}`,
    }),
  );
}

process.exit(0);
