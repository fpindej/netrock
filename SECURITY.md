# Security Policy

## Reporting a Vulnerability

If you discover a security vulnerability in NETrock, **please report it responsibly**. Do not open a public issue.

### Preferred: GitHub Private Vulnerability Reporting

Use [GitHub's private vulnerability reporting](https://github.com/fpindej/netrock/security/advisories/new) to submit your report directly through the repository. This keeps the conversation private and structured.

### Fallback: Email

If you can't use GitHub's reporting, email **contact@mail.pindej.cz** with:

- A description of the vulnerability
- Steps to reproduce
- Affected versions (if known)
- Any suggested fix (optional but appreciated)

## What Qualifies

Security vulnerabilities include (but are not limited to):

- Authentication or authorization bypass
- Cross-site scripting (XSS)
- SQL injection or other injection attacks
- Cross-site request forgery (CSRF)
- Sensitive data exposure
- Insecure cryptographic implementation
- Privilege escalation

## What Doesn't Qualify

- General bugs (use [GitHub Issues](https://github.com/fpindej/netrock/issues))
- Feature requests (use [GitHub Issues](https://github.com/fpindej/netrock/issues))
- Questions about usage (use [Discord](https://discord.gg/5rHquRptSh) or [Discussions](https://github.com/fpindej/netrock/discussions))

## Response Timeline

- **Acknowledgment**: within 48 hours of report
- **Target fix**: within 90 days, depending on severity and complexity
- **Disclosure**: coordinated with the reporter — we won't publish details until a fix is available

## Credit

Reporters are credited in the release notes by default. If you prefer to remain anonymous, let us know in your report.

## Scope

This policy covers the **NETrock template repository** itself. Once you run the init script and customize the project, security of your deployed application is your responsibility — though the template gives you a strong starting point. See [docs/security.md](docs/security.md) for the security architecture overview.
