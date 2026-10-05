export const manifests: Array<UmbExtensionManifest> = [
  {
    type: 'dashboard',
    alias: 'DynamicDave.ContentSchedule.Dashboard',
    name: 'Scheduled Content Dashboard',
    element: () => import('./schedule-dashboard.element.js'),
    weight: -10,
    meta: { label: '#ddContentSchedule_title', pathname: 'scheduled-content' },
    conditions: [{ alias: 'Umb.Condition.SectionAlias', match: 'Umb.Section.Content' }],
  },
  {
    type: 'localization',
    alias: 'DynamicDave.ContentSchedule.Localization.En',
    name: 'Content Schedule English',
    weight: -100,
    meta: { culture: 'en' },
    js: () => import('./localization/en.js'),
  },
  {
    type: 'localization',
    alias: 'DynamicDave.ContentSchedule.Localization.Nl',
    name: 'Content Schedule Dutch',
    weight: -100,
    meta: { culture: 'nl' },
    js: () => import('./localization/nl.js'),
  },
];
