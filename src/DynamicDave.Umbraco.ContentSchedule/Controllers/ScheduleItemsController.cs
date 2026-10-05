using Asp.Versioning;
using DynamicDave.Umbraco.ContentSchedule.Models;
using DynamicDave.Umbraco.ContentSchedule.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Core.Security;

namespace DynamicDave.Umbraco.ContentSchedule.Controllers;

[ApiVersion("1.0")]
[ApiExplorerSettings(GroupName = "ScheduleItems")]
public class ScheduleItemsController(ScheduleReader reader, IBackOfficeSecurityAccessor security)
    : DynamicDaveUmbracoContentScheduleApiControllerBase
{
    [HttpGet("items", Name = "GetScheduleItems")]
    [ProducesResponseType<ScheduleItemsResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetScheduleItems([FromQuery] ScheduleRange range = ScheduleRange.Next7Days)
    {
        if (!Enum.IsDefined(range)) return BadRequest();

        var user = security.BackOfficeSecurity?.CurrentUser;
        if (user is null) return Unauthorized();

        return Ok(await reader.ReadAsync(range, user, DateTime.UtcNow, TimeZoneInfo.Local));
    }
}
