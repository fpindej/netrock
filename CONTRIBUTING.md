# Contributing to NETrock

Thanks for your interest in contributing! NETrock is a production-grade template, so we hold contributions to the same standard as the existing codebase.

## Getting Started

1. **Fork the repository** and clone your fork
2. **Run the init script** — `./init.sh` (macOS/Linux) or `.\init.ps1` (Windows)
3. **Start Docker** — `docker compose -f docker-compose.local.yml up -d --build`
4. **Read the docs** — at minimum, read [`CLAUDE.md`](CLAUDE.md) and [`AGENTS.md`](AGENTS.md) before making changes

## How to Contribute

### Reporting Bugs

Open a [GitHub issue](https://github.com/fpindej/web-app-template/issues) with:
- What you expected to happen
- What actually happened
- Steps to reproduce
- Your environment (OS, .NET version, Node version, Docker version)

### Suggesting Features

Open an issue with the `enhancement` label. Explain:
- The problem you're solving
- Why it belongs in a template (vs. being project-specific)
- How it fits with the existing architecture

Not every feature belongs in a template. NETrock provides a solid foundation — features that are highly project-specific or opinionated toward a particular business domain are better added after initialization.

### Submitting Code

1. **Create a branch** from `master` — never push directly to `master`
2. **Follow the conventions** documented in [`AGENTS.md`](AGENTS.md), [`src/backend/AGENTS.md`](src/backend/AGENTS.md), and [`src/frontend/AGENTS.md`](src/frontend/AGENTS.md)
3. **Run all checks** before opening a PR:

```bash
# Backend
dotnet build src/backend/MyProject.slnx
dotnet test src/backend/MyProject.slnx -c Release

# Frontend
cd src/frontend && pnpm run format && pnpm run lint && pnpm run check
```

4. **Open a PR** against `master` with a clear description of what changed and why
5. **Use Conventional Commits**: `type(scope): imperative description`
   - `feat(backend): add webhook support`
   - `fix(frontend): prevent double-submit on login`
   - `docs: update deployment guide`

## Code Standards

These are non-negotiable. PRs that don't follow them will be requested to change.

### Backend (.NET)

- `Result`/`Result<T>` for all fallible operations — never throw for business logic
- `TimeProvider` (injected) — never `DateTime.UtcNow` or `DateTimeOffset.UtcNow`
- Typed DTOs only — `ProblemDetails` (RFC 9457) for all error responses
- `internal` on all Infrastructure service implementations
- XML docs (`/// <summary>`) on all public and internal API surface
- NuGet versions in `Directory.Packages.props` only — never in `.csproj` files
- All tests must pass — no skipped tests, no `[Fact(Skip = ...)]`

### Frontend (SvelteKit)

- Svelte 5 Runes only: `$props`, `$state`, `$derived`, `$effect` — never `export let`
- `interface Props` + `$props()` — never `$props<{...}>()`
- Logical CSS only: `ms-*`/`me-*`/`ps-*`/`pe-*` — never `ml-*`/`mr-*`/`pl-*`/`pr-*`
- No `any` type — define proper interfaces
- Never hand-edit `v1.d.ts` — regenerate with `pnpm run api:generate`

### Cross-Cutting

- Security restrictive by default — deny first, open selectively
- Don't introduce dependencies without discussion — open an issue first
- Update documentation if your change affects conventions, file maps, or workflows

## What Makes a Good Contribution

**Great contributions:**
- Bug fixes with tests that reproduce the issue
- Security improvements
- Performance improvements with benchmarks
- Documentation improvements
- Test coverage for untested edge cases
- Accessibility improvements in the frontend

**Contributions that need discussion first:**
- New dependencies (NuGet packages, npm packages)
- Architectural changes
- New infrastructure services
- Changes to the auth/permission system
- Changes to the init script

**Probably doesn't belong in the template:**
- Business-domain-specific features (e-commerce, CMS, etc.)
- Alternative database providers
- Alternative frontend frameworks
- Features that only apply to specific hosting providers

## Development Tips

- Use the [SKILLS.md](SKILLS.md) recipes when adding entities, endpoints, or pages
- Check [FILEMAP.md](FILEMAP.md) for change impact — "when you change X, also update Y"
- The architecture tests will catch dependency violations at build time
- Run `pnpm run api:generate` after any backend API change

## Questions?

- Open a [GitHub Discussion](https://github.com/fpindej/web-app-template/discussions)
- Join the [Discord](https://discord.gg/5rHquRptSh)

## License

By contributing, you agree that your contributions will be licensed under the [MIT License](LICENSE).
