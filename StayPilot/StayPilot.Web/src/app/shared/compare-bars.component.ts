import { Component, Input } from '@angular/core';

/** One side of a two-way comparison. */
export interface CompareSide {
  /** What this side is: a place name, a condition, a scenario. */
  label: string;
  /** What it is worth, driving the bar length. */
  value: number;
  /** That value formatted for a human — "€2,140/m²". */
  valueText: string;
  /** The qualifier under the label: sample size, typology. */
  note?: string;
  /** Which role this side plays, so the two bars are told apart by colour. */
  tone?: 'low' | 'high';
}

/**
 * Two measured values held against each other, with the gap between them
 * named rather than left to be worked out.
 *
 * This exists because "cheaper place vs dearer place" and "as a project vs
 * finished" are the same shape of finding, and were being drawn twice with
 * two sets of local CSS that had already drifted apart. The important part is
 * that both bars are scaled to the larger of the pair: normalise each one
 * independently and a 5% difference and a 500% one look identical, which is
 * the one thing this chart exists to prevent.
 *
 *   <app-compare-bars [low]="cheaper" [high]="dearer" deltaText="−32% · 8.4 km apart" />
 */
@Component({
  selector: 'app-compare-bars',
  standalone: true,
  template: `
    <figure class="compare" [attr.aria-label]="caption || null">
      @if (caption || deltaText) {
        <figcaption class="compare-head">
          @if (caption) {
            <span class="compare-caption">{{ caption }}</span>
          }
          @if (deltaText) {
            <span class="compare-delta">{{ deltaText }}</span>
          }
        </figcaption>
      }

      <div class="compare-rows" role="list">
        @for (side of sides(); track side.label) {
          <div class="compare-row" role="listitem">
            <div class="compare-label">
              <span class="compare-name" [title]="side.label">{{ side.label }}</span>
              @if (side.note) {
                <span class="compare-note">{{ side.note }}</span>
              }
            </div>

            <div class="compare-track">
              <div
                class="compare-bar"
                [class.is-high]="side.tone === 'high'"
                [style.width.%]="widthPercent(side.value)"
              ></div>
            </div>

            <span class="compare-value">{{ side.valueText }}</span>
          </div>
        }
      </div>
    </figure>
  `,
  styles: [
    `
      .compare {
        margin: 0;
        display: flex;
        flex-direction: column;
        gap: var(--sp-4);
      }

      .compare-head {
        display: flex;
        align-items: baseline;
        justify-content: space-between;
        gap: var(--sp-3);
        flex-wrap: wrap;
      }

      .compare-caption {
        font-size: var(--fs-sm);
        font-weight: 700;
      }

      /* The gap is the finding, so it is set as a value and badged, not run in
         as a caption. --accent-text, not --accent: the bright teal that fills
         a bar is about 2.3:1 on white and unreadable as small text. */
      .compare-delta {
        padding: 0.18rem 0.6rem;
        border-radius: var(--r-xs);
        background: var(--accent-soft);
        color: var(--accent-text);
        font-size: var(--fs-xs);
        font-weight: 700;
        font-variant-numeric: tabular-nums;
        white-space: nowrap;
      }

      .compare-rows {
        display: flex;
        flex-direction: column;
        gap: var(--sp-4);
      }

      .compare-row {
        display: grid;
        grid-template-columns: minmax(6rem, 10rem) 1fr auto;
        align-items: center;
        gap: var(--sp-3);
      }

      .compare-label {
        display: flex;
        flex-direction: column;
        min-width: 0;
      }

      .compare-name {
        font-size: var(--fs-sm);
        font-weight: 500;
        white-space: nowrap;
        overflow: hidden;
        text-overflow: ellipsis;
      }

      .compare-note {
        font-size: var(--fs-xs);
        color: var(--text-faint);
      }

      .compare-track {
        position: relative;
        height: 1.75rem;
        border-radius: 4px;
        background: var(--chart-track);
        overflow: hidden;
      }

      .compare-bar {
        height: 100%;
        border-radius: 4px;
        background: var(--chart-2);
        transition: width 0.5s var(--ease);
      }

      .compare-bar.is-high {
        background: var(--chart-1);
      }

      .compare-value {
        font-size: var(--fs-md);
        font-weight: 800;
        font-variant-numeric: tabular-nums;
        white-space: nowrap;
      }

      @media (max-width: 560px) {
        .compare-row {
          grid-template-columns: 1fr auto;
          grid-template-areas:
            'label value'
            'track track';
          gap: var(--sp-2) var(--sp-3);
        }

        .compare-label {
          grid-area: label;
        }

        .compare-value {
          grid-area: value;
        }

        .compare-track {
          grid-area: track;
        }
      }
    `
  ]
})
export class CompareBarsComponent {
  /** The smaller side. Drawn first, so the pair always reads low then high. */
  @Input({ required: true }) low!: CompareSide;

  @Input({ required: true }) high!: CompareSide;

  @Input() caption?: string;

  /** The gap, already worded — "−32% · 8.4 km apart", "+€480/m² finished". */
  @Input() deltaText?: string;

  /**
   * A getter, not a computed(): these are plain @Inputs, not signals, so a
   * computed() would read them once, cache, and never notice them change.
   */
  sides(): CompareSide[] {
    return [
      { ...this.low, tone: 'low' },
      { ...this.high, tone: 'high' }
    ];
  }

  /**
   * Both bars share one scale: the larger of the pair. That is the whole
   * point — scaled separately, every comparison would look the same.
   */
  widthPercent(value: number): number {
    const max = Math.max(this.low?.value ?? 0, this.high?.value ?? 0);

    if (!(max > 0) || !Number.isFinite(value) || value <= 0) {
      return 0;
    }

    return Math.max(2, (value / max) * 100);
  }
}
