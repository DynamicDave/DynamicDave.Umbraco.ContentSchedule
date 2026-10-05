# DynamicDave.Umbraco.ContentSchedule

A backoffice dashboard (Content section) that lists all scheduled publish and unpublish actions, with filters for Today, Next 7 days, Next 30 days and Overdue. Click a page name to open it.

## Install

    dotnet add package DynamicDave.Umbraco.ContentSchedule

Supported Umbraco version: **17.x** (net10.0). The backoffice UI is available in English and Dutch.

## Configuration

None. The dashboard appears in the Content section for users with content access.

## v1 limitations

- Read-only: you cannot reschedule or cancel from the dashboard.
- "Overdue" is a derived status: Umbraco keeps no failure status, so an entry is shown as overdue when its scheduled time has passed but the action has not been applied.
- "Today" uses the server time zone.
- Only the user's content start nodes are enforced; granular per-node permissions are not.

## License

MIT
