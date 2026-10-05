# DynamicDave.Umbraco.ContentSchedule

A backoffice dashboard (Content section) that lists all scheduled publish and unpublish actions, with filters for Today, Next 7 days, Next 30 days and Overdue, each showing the number of entries it contains. Click a page name to open it.

## Install

    dotnet add package DynamicDave.Umbraco.ContentSchedule

Supported Umbraco version: **17.3 or later 17.x** (net10.0). Umbraco 18 is not supported by this version. The backoffice UI is available in English, Dutch, German, French and Danish.

## Configuration

None. The dashboard appears in the Content section for users with content access.

## Limitations

- Read-only: you cannot reschedule or cancel from the dashboard.
- "Overdue" is a derived status: Umbraco keeps no failure status, so an entry is shown as overdue when its scheduled time has passed but the action has not been applied.
- "Today" uses the server time zone.

## Permissions

Users only see documents within their content start nodes that their user groups may browse, the same rule as the content tree.

## License

MIT
