using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using MyProject.Application.Features.Jobs;
using MyProject.Application.Identity.Constants;
using MyProject.WebApi.Authorization;
using MyProject.WebApi.Features.Admin.Dtos.Jobs;
using MyProject.WebApi.Shared;

namespace MyProject.WebApi.Features.Admin;

/// <summary>
/// Administrative endpoints for managing background jobs.
/// Provides visibility into recurring job schedules, execution history,
/// and the ability to trigger, pause, resume, and remove jobs.
/// </summary>
[Route("api/v1/admin")]
[Tags("Jobs")]
public class JobsController(IJobManagementService jobManagementService) : ApiController
{
    /// <summary>
    /// Gets all registered recurring jobs.
    /// </summary>
    /// <returns>A list of recurring job summaries</returns>
    /// <response code="200">Returns the list of recurring jobs</response>
    /// <response code="401">If the user is not authenticated</response>
    /// <response code="403">If the user does not have the required permission</response>
    [HttpGet("jobs")]
    [RequirePermission(AppPermissions.Jobs.View)]
    [ProducesResponseType(typeof(IReadOnlyList<RecurringJobResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<RecurringJobResponse>>> ListJobs()
    {
        var jobs = await jobManagementService.GetRecurringJobsAsync();
        return Ok(jobs.Select(j => j.ToResponse()).ToList());
    }

    /// <summary>
    /// Gets detailed information about a recurring job, including recent execution history.
    /// </summary>
    /// <param name="jobId">The recurring job identifier</param>
    /// <returns>The job details with execution history</returns>
    /// <response code="200">Returns the job details</response>
    /// <response code="401">If the user is not authenticated</response>
    /// <response code="403">If the user does not have the required permission</response>
    /// <response code="404">If the job was not found</response>
    [HttpGet("jobs/{jobId:jobId}")]
    [RequirePermission(AppPermissions.Jobs.View)]
    [ProducesResponseType(typeof(RecurringJobDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RecurringJobDetailResponse>> GetJob(string jobId)
    {
        var result = await jobManagementService.GetRecurringJobDetailAsync(jobId);

        if (!result.IsSuccess)
        {
            return ProblemFactory.Create(result.Error, result.ErrorType);
        }

        return Ok(result.Value.ToResponse());
    }

    /// <summary>
    /// Re-registers all recurring job definitions, restoring any jobs deleted from the dashboard.
    /// Paused jobs are re-registered with a disabled schedule to preserve their pause state.
    /// </summary>
    /// <returns>No content on success</returns>
    /// <response code="204">Jobs restored successfully</response>
    /// <response code="400">If the restore operation failed</response>
    /// <response code="401">If the user is not authenticated</response>
    /// <response code="403">If the user does not have the required permission</response>
    [HttpPost("jobs/restore")]
    [RequirePermission(AppPermissions.Jobs.Manage)]
    [EnableRateLimiting(RateLimitPolicies.AdminMutations)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult> RestoreJobs()
    {
        var result = await jobManagementService.RestoreJobsAsync();

        if (!result.IsSuccess)
        {
            return ProblemFactory.Create(result.Error, result.ErrorType);
        }

        return NoContent();
    }

    /// <summary>
    /// Manually triggers an immediate execution of a recurring job.
    /// </summary>
    /// <param name="jobId">The recurring job identifier</param>
    /// <returns>No content on success</returns>
    /// <response code="204">Job triggered successfully</response>
    /// <response code="400">If the trigger operation failed</response>
    /// <response code="401">If the user is not authenticated</response>
    /// <response code="403">If the user does not have the required permission</response>
    /// <response code="404">If the job was not found</response>
    [HttpPost("jobs/{jobId:jobId}/trigger")]
    [RequirePermission(AppPermissions.Jobs.Manage)]
    [EnableRateLimiting(RateLimitPolicies.AdminMutations)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult> TriggerJob(string jobId)
    {
        var result = await jobManagementService.TriggerJobAsync(jobId);

        if (!result.IsSuccess)
        {
            return ProblemFactory.Create(result.Error, result.ErrorType);
        }

        return NoContent();
    }

    /// <summary>
    /// Removes a recurring job from the scheduler.
    /// </summary>
    /// <param name="jobId">The recurring job identifier</param>
    /// <returns>No content on success</returns>
    /// <response code="204">Job removed successfully</response>
    /// <response code="401">If the user is not authenticated</response>
    /// <response code="403">If the user does not have the required permission</response>
    /// <response code="404">If the job was not found</response>
    /// <response code="429">If too many requests have been made</response>
    [HttpDelete("jobs/{jobId:jobId}")]
    [RequirePermission(AppPermissions.Jobs.Manage)]
    [EnableRateLimiting(RateLimitPolicies.AdminMutations)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult> RemoveJob(string jobId)
    {
        var result = await jobManagementService.RemoveJobAsync(jobId);

        if (!result.IsSuccess)
        {
            return ProblemFactory.Create(result.Error, result.ErrorType);
        }

        return NoContent();
    }

    /// <summary>
    /// Pauses a recurring job by disabling its schedule.
    /// </summary>
    /// <param name="jobId">The recurring job identifier</param>
    /// <returns>No content on success</returns>
    /// <response code="204">Job paused successfully</response>
    /// <response code="401">If the user is not authenticated</response>
    /// <response code="403">If the user does not have the required permission</response>
    /// <response code="404">If the job was not found</response>
    /// <response code="429">If too many requests have been made</response>
    [HttpPost("jobs/{jobId:jobId}/pause")]
    [RequirePermission(AppPermissions.Jobs.Manage)]
    [EnableRateLimiting(RateLimitPolicies.AdminMutations)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult> PauseJob(string jobId)
    {
        var result = await jobManagementService.PauseJobAsync(jobId);

        if (!result.IsSuccess)
        {
            return ProblemFactory.Create(result.Error, result.ErrorType);
        }

        return NoContent();
    }

    /// <summary>
    /// Resumes a previously paused recurring job.
    /// </summary>
    /// <param name="jobId">The recurring job identifier</param>
    /// <returns>No content on success</returns>
    /// <response code="204">Job resumed successfully</response>
    /// <response code="401">If the user is not authenticated</response>
    /// <response code="403">If the user does not have the required permission</response>
    /// <response code="404">If the job was not found</response>
    /// <response code="429">If too many requests have been made</response>
    [HttpPost("jobs/{jobId:jobId}/resume")]
    [RequirePermission(AppPermissions.Jobs.Manage)]
    [EnableRateLimiting(RateLimitPolicies.AdminMutations)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult> ResumeJob(string jobId)
    {
        var result = await jobManagementService.ResumeJobAsync(jobId);

        if (!result.IsSuccess)
        {
            return ProblemFactory.Create(result.Error, result.ErrorType);
        }

        return NoContent();
    }
}
