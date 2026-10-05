import { css, html, customElement, state, repeat } from '@umbraco-cms/backoffice/external/lit';
import { UmbLitElement } from '@umbraco-cms/backoffice/lit-element';
import { ScheduleItemsService } from './api/index.js';
import type { ScheduleItemsResponse, ScheduleRange } from './api/index.js';

type Item = ScheduleItemsResponse['items'][number];

const FILTERS: Array<{ range: ScheduleRange; key: string }> = [
  { range: 'Today', key: 'ddContentSchedule_filterToday' },
  { range: 'Next7Days', key: 'ddContentSchedule_filter7' },
  { range: 'Next30Days', key: 'ddContentSchedule_filter30' },
  { range: 'Overdue', key: 'ddContentSchedule_filterOverdue' },
];

@customElement('dd-schedule-dashboard')
export class DdScheduleDashboardElement extends UmbLitElement {
  @state() private _range: ScheduleRange = 'Next7Days';
  @state() private _items: Item[] = [];
  @state() private _loading = true;
  @state() private _error = false;

  /** Incremented per request so responses of superseded requests are ignored. */
  #requestId = 0;

  override connectedCallback() {
    super.connectedCallback();
    void this.#load();
  }

  override disconnectedCallback() {
    super.disconnectedCallback();
    // Invalidate any in-flight request so it does not update a detached element.
    this.#requestId++;
  }

  async #load() {
    const requestId = ++this.#requestId;
    this._loading = true;
    this._error = false;
    let items: Item[] = [];
    let failed = false;
    try {
      const { data, error } = await ScheduleItemsService.getScheduleItems({ query: { range: this._range } });
      failed = error !== undefined || data === undefined;
      items = data?.items ?? [];
    } catch {
      failed = true;
    }
    if (requestId !== this.#requestId) return; // stale response
    this._items = failed ? [] : items;
    this._error = failed;
    this._loading = false;
  }

  #select(range: ScheduleRange) {
    if (range === this._range) return;
    this._range = range;
    void this.#load();
  }

  #href(key: string) {
    return `/umbraco/section/content/workspace/document/edit/${key}`;
  }

  override render() {
    return html`
      <uui-box headline=${this.localize.term('ddContentSchedule_title')}>
        <uui-tab-group slot="header-actions">
          ${FILTERS.map(
            (f) => html`<uui-tab
              label=${this.localize.term(f.key)}
              ?active=${this._range === f.range}
              @click=${() => this.#select(f.range)}
              >${this.localize.term(f.key)}</uui-tab
            >`,
          )}
        </uui-tab-group>
        ${this.#renderBody()}
      </uui-box>
    `;
  }

  #renderBody() {
    if (this._loading) return html`<uui-loader></uui-loader>`;
    if (this._error) return html`<p>${this.localize.term('ddContentSchedule_loadFailed')}</p>`;
    if (this._items.length === 0) return html`<p>${this.localize.term('ddContentSchedule_empty')}</p>`;
    return html`
      <uui-table>
        <uui-table-head>
          <uui-table-head-cell>${this.localize.term('ddContentSchedule_page')}</uui-table-head-cell>
          <uui-table-head-cell>${this.localize.term('ddContentSchedule_action')}</uui-table-head-cell>
          <uui-table-head-cell>${this.localize.term('ddContentSchedule_when')}</uui-table-head-cell>
          <uui-table-head-cell>${this.localize.term('ddContentSchedule_language')}</uui-table-head-cell>
          <uui-table-head-cell>${this.localize.term('ddContentSchedule_status')}</uui-table-head-cell>
        </uui-table-head>
        ${repeat(
          this._items,
          (i) => `${i.key}|${i.action}|${i.culture ?? ''}|${i.scheduledAt}`,
          (i) => html`
            <uui-table-row>
              <uui-table-cell><a class="page-link" href=${this.#href(i.key)}>${i.name}</a></uui-table-cell>
              <uui-table-cell>${this.localize.term(`ddContentSchedule_${i.action}`)}</uui-table-cell>
              <uui-table-cell>${this.localize.date(i.scheduledAt, { dateStyle: 'medium', timeStyle: 'short' })}</uui-table-cell>
              <uui-table-cell>${i.culture ?? '—'}</uui-table-cell>
              <uui-table-cell>
                <uui-tag
                  color=${i.status === 'overdue' ? 'danger' : 'default'}
                  title=${i.status === 'overdue' ? this.localize.term('ddContentSchedule_overdueHint') : ''}>
                  ${this.localize.term(`ddContentSchedule_${i.status}`)}
                </uui-tag>
              </uui-table-cell>
            </uui-table-row>`,
        )}
      </uui-table>
    `;
  }

  static override styles = css`
    :host { display: block; padding: var(--uui-size-layout-1); }
    .page-link { color: var(--uui-color-interactive); font-weight: bold; text-decoration: none; }
    .page-link:hover { text-decoration: underline; }
  `;
}

export default DdScheduleDashboardElement;
