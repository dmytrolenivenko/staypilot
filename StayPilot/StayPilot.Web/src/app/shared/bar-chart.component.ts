import { Component, Input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { APP_LOCALE } from '../core/locale';

/** One bar. `value` drives the length; `valueText` is what a human reads. */
export interface BarChartItem {
  label: string;
  value: number;
  /** The value already formatted — "€2,140", "86 m²". Falls back to the raw number. */
  valueText?: string;
  /** A second line under the label: sample size, typology, whatever qualifies it. */
  note?: string;
  /** Makes the row a link to somewhere that explains it. */
  link?: string;
  linkQuery?: Record<string, string>;
}

/**
 * A ranked list of measured values, drawn as horizontal bars.
 *
 * Horizontal rather than vertical on purpose: the labels here are Portuguese
 * place names, and vertical bars force them to be rotated, truncated to four
 * characters, or dropped. Laid out as rows, a long name simply takes its line.
 *
 * Drawn in CSS rather than SVG for the same reason — text in an SVG cannot
 * wrap or reflow, so a responsive SVG either scales its labels down to
 * illegibility on a phone or clips them. The axis and the gridlines are real
 * (ticks computed from the scale, not decorative), the bars are true to
 * length, and every row reads as text to a screen reader without a parallel
 * table to maintain.
 *
 *   <app-bar-chart [items]="rows()" unit="listings" caption="Most listed areas" />
 */
@Component({
  selector: 'app-bar-chart',
  standalone: true,
  imports: [RouterLink],
  template: `
    <figure class="chart" [attr.aria-label]="caption || null">
      @if (caption || subCaption) {
        <figcaption class="chart-head">
          @if (caption) {
            <span class="chart-caption">{{ caption }}</span>
          }
          @if (subCaption) {
            <span class="chart-sub">{{ subCaption }}</span>
          }
        </figcaption>
      }

      <div class="chart-rows" role="list">
        @for (item of items; track item.label) {
          <div class="chart-row" role="listitem">
            <div class="chart-label">
              <span class="chart-name" [title]="item.label">
                @if (item.link) {
                  <a [routerLink]="item.link" [queryParams]="item.linkQuery ?? null">{{ item.label }}</a>
                } @else {
                  {{ item.label }}
                }
              </span>
              @if (item.note) {
                <span class="chart-note">{{ item.note }}</span>
              }
            </div>

            <div class="chart-track">
              <div
                class="chart-bar"
                [style.width.%]="widthPercent(item.value)"
                [title]="item.label + ' — ' + text(item)"
              ></div>
            </div>

            <span class="chart-value">{{ text(item) }}</span>
          </div>
        }
      </div>

      <!-- The axis. Ticks are computed from the same scale the bars use, so a
           bar reaching the third tick really is at three-quarters of the max. -->
      @if (showAxis && scaleMax() > 0) {
        <div class="chart-axis" aria-hidden="true">
          <div class="chart-axis-track">
            @for (tick of ticks(); track tick) {
              <span class="chart-tick" [style.left.%]="widthPercent(tick)">{{ tickText(tick) }}</span>
            }
          </div>
        </div>
      }
    </figure>
  `,
  styles: [
    `
      .chart {
        margin: 0;
        display: flex;
        flex-direction: column;
        gap: var(--sp-4);
      }

      .chart-head {
        display: flex;
        align-items: baseline;
        justify-content: space-between;
        gap: var(--sp-3);
        flex-wrap: wrap;
      }

      .chart-caption {
        font-size: var(--fs-sm);
        font-weight: 700;
      }

      .chart-sub {
        font-size: var(--fs-xs);
        color: var(--text-faint);
        font-variant-numeric: tabular-nums;
      }

      .chart-rows {
        display: flex;
        flex-direction: column;
        gap: var(--sp-3);
      }

      /* Label | bar | value. The label column is capped rather than sized to
         content so every bar in the chart starts at the same x — bars that
         each begin somewhere different cannot be compared by eye. */
      .chart-row {
        display: grid;
        grid-template-columns: minmax(5rem, 9rem) 1fr auto;
        align-items: center;
        gap: var(--sp-3);
      }

      .chart-label {
        display: flex;
        flex-direction: column;
        min-width: 0;
      }

      .chart-name {
        font-size: var(--fs-sm);
        font-weight: 500;
        white-space: nowrap;
        overflow: hidden;
        text-overflow: ellipsis;
      }

      .chart-name a {
        color: inherit;
        text-decoration: none;
      }

      /* --accent-text, not --accent: the fill teal is about 2.3:1 on white and
         fails as small text. --accent is for bars and buttons only. */
      .chart-name a:hover {
        color: var(--accent-text);
        text-decoration: underline;
      }

      .chart-note {
        font-size: var(--fs-xs);
        color: var(--text-faint);
        white-space: nowrap;
        overflow: hidden;
        text-overflow: ellipsis;
      }

      /* The track is the full scale; the bar is the value within it. Drawing
         the empty remainder is what makes "half as much" visible at a glance. */
      .chart-track {
        position: relative;
        height: 0.625rem;
        border-radius: 3px;
        background: var(--chart-track);
        overflow: hidden;
      }

      .chart-bar {
        height: 100%;
        border-radius: 3px;
        background: var(--chart-1);
        transition: width 0.5s var(--ease);
      }

      /* Past the top three the bar steps down a tone. The ranking is the whole
         point of this chart, and a flat wall of one colour hides where the
         values stop being close to the leader. */
      .chart-row:nth-child(n + 4) .chart-bar {
        background: var(--chart-2);
      }

      .chart-value {
        font-size: var(--fs-sm);
        font-weight: 700;
        font-variant-numeric: tabular-nums;
        white-space: nowrap;
      }

      /* The axis repeats the row grid rather than guessing at an offset, so
         its ticks sit under the track column however wide the labels are. */
      .chart-axis {
        display: grid;
        grid-template-columns: minmax(5rem, 9rem) 1fr auto;
        gap: var(--sp-3);
        margin-top: calc(-1 * var(--sp-2));
      }

      .chart-axis-track {
        position: relative;
        grid-column: 2;
        height: 1rem;
        border-top: 1px solid var(--chart-grid);
      }

      .chart-tick {
        position: absolute;
        top: var(--sp-1);
        transform: translateX(-50%);
        font-size: var(--fs-xs);
        color: var(--text-faint);
        font-variant-numeric: tabular-nums;
        white-space: nowrap;
      }

      /* The end ticks are pulled inside the track rather than centred on it,
         so neither hangs off the edge of the chart. */
      .chart-tick:first-child {
        transform: none;
      }

      .chart-tick:last-child {
        transform: translateX(-100%);
      }

      @media (max-width: 560px) {
        .chart-row {
          grid-template-columns: 1fr auto;
          grid-template-areas:
            'label value'
            'track track';
          gap: var(--sp-2) var(--sp-3);
        }

        .chart-label {
          grid-area: label;
        }

        .chart-value {
          grid-area: value;
        }

        .chart-track {
          grid-area: track;
        }

        .chart-name {
          white-space: normal;
        }

        /* The label column is gone on a phone, so the axis spans the row. */
        .chart-axis {
          grid-template-columns: 1fr;
        }

        .chart-axis-track {
          grid-column: 1;
        }
      }
    `
  ]
})
export class BarChartComponent {
  @Input({ required: true }) items: BarChartItem[] = [];

  /** The chart's title, shown above it. */
  @Input() caption?: string;

  /** A qualifier for the title — the total, the budget, the date. */
  @Input() subCaption?: string;

  /**
   * The top of the scale. Left unset, the largest value is the top, which is
   * right for a ranking. Set it when several charts must share one scale.
   */
  @Input() max?: number;

  @Input() showAxis = true;

  /** Appended to the axis ticks. Values carry their own units via valueText. */
  @Input() unit = '';

  /**
   * A method, not a computed(): `items` and `max` are plain @Inputs rather
   * than signals, so a computed() would read them once, cache, and never
   * notice the chart's data change underneath it.
   */
  scaleMax(): number {
    if (this.max !== undefined && this.max > 0) {
      return this.max;
    }

    return Math.max(0, ...this.items.map(item => item.value));
  }

  /**
   * Bars are floored at 1.5% rather than 0 so a real-but-tiny value still
   * shows as a sliver. A true zero stays at zero — "too small to see" and
   * "none" are different findings and must not look the same.
   */
  widthPercent(value: number): number {
    const max = this.scaleMax();

    if (!(max > 0) || !Number.isFinite(value) || value <= 0) {
      return 0;
    }

    return Math.max(1.5, (value / max) * 100);
  }

  text(item: BarChartItem): string {
    return item.valueText ?? item.value.toLocaleString(APP_LOCALE);
  }

  /** Nothing, half, all — three ticks is enough to read a bar against. */
  ticks(): number[] {
    const max = this.scaleMax();

    return max > 0 ? [0, max / 2, max] : [];
  }

  tickText(tick: number): string {
    const rounded = Math.round(tick).toLocaleString(APP_LOCALE);

    return this.unit ? `${rounded} ${this.unit}` : rounded;
  }
}
