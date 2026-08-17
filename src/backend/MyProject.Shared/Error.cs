namespace MyProject.Shared;

/// <summary>
/// A client-facing error consisting of a stable, machine-readable <see cref="Code"/> and a
/// human-readable <see cref="Message"/>.
/// </summary>
/// <remarks>
/// <para>
/// Instances are declared once in <see cref="ErrorMessages"/> and passed to <c>Result.Failure()</c>.
/// The code surfaces as the <c>code</c> extension of every <c>ProblemDetails</c> response so that
/// clients can branch on it (or use it as a translation key) instead of matching English text.
/// </para>
/// <para>
/// Codes are snake_case and derived from the declaring location: <c>{NestedClass}_{FieldName}</c>
/// (for example <c>ErrorMessages.ExternalAuth.StateExpired</c> is <c>external_auth_state_expired</c>).
/// Treat codes as a public contract - renaming one is a breaking change for API consumers.
/// </para>
/// <para>Pattern documented in .claude/skills/backend-conventions/SKILL.md.</para>
/// </remarks>
/// <param name="Code">The stable, snake_case, machine-readable identifier of the error condition.</param>
/// <param name="Message">The human-readable, client-facing description of the error.</param>
public sealed record Error(string Code, string Message);
