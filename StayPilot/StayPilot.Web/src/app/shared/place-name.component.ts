import { Component, Input } from '@angular/core';
import { AreaLevel } from '../core/models/market-area-stats';

/** Anything that carries a place at a known grain — leaderboard rows, budget rows, gap halves. */
export interface PlaceParts {
  level: AreaLevel;
  district: string;
  municipality: string;
  town: string;
}

/** Portuguese administrative names, because that is what the source data is. */
export function placeLevelLabel(level: AreaLevel): string {
  switch (level) {
    case 'District':
      return 'Distrito';

    case 'Municipality':
      return 'Município';

    default:
      return 'Freguesia';
  }
}

export function placeLevelHint(level: AreaLevel): string {
  switch (level) {
    case 'District':
      return 'Distrito — the broadest grain, e.g. Faro';

    case 'Municipality':
      return 'Município — a council inside a distrito, e.g. Albufeira';

    default:
      return 'Freguesia — a parish/town inside a município, e.g. Guia';
  }
}

/** The place's own name, without any parent. What a column of these should sort on. */
export function placeOwnName(place: PlaceParts): string {
  switch (place.level) {
    case 'District':
      return place.district;

    case 'Municipality':
      return place.municipality;

    default:
      return place.town;
  }
}

/**
 * Everything the place sits inside, biggest last — the way an address is read aloud rather than
 * the way a database sorts it. Empty for a distrito, which sits inside nothing we hold.
 */
export function placeParents(place: PlaceParts): string {
  switch (place.level) {
    case 'District':
      return '';

    case 'Municipality':
      return place.district;

    default:
      return [place.municipality, place.district].filter(part => part).join(' · ');
  }
}

/**
 * One place, named so you can tell what kind of place it is.
 *
 * The `displayName` off the API reads "Guia (Albufeira)" and leaves you guessing whether the
 * bracket holds a município, a distrito, or something else — and whether "Guia" itself is a town
 * or a municipality. Here the row says which grain it measures and spells the parents out:
 *
 *   Guia            [Freguesia]
 *   Albufeira · Faro
 *
 * `displayName` is still what a one-line context uses (a chart label, a meta line). This is for
 * tables, where there is room to be unambiguous.
 */
@Component({
  selector: 'app-place-name',
  standalone: true,
  template: `
    <span class="place">
      <span class="place-line">
        <strong class="place-own">{{ own }}</strong>
        <span class="tag tag-quiet" [title]="levelHint">{{ levelLabel }}</span>
        <ng-content />
      </span>
      @if (parents) {
        <span class="place-parents">{{ parents }}</span>
      }
    </span>
  `,
  styles: [
    `
      .place {
        display: flex;
        flex-direction: column;
        gap: 0.1rem;
      }

      .place-line {
        display: flex;
        align-items: center;
        gap: var(--sp-2);
        flex-wrap: wrap;
      }

      .place-own {
        font-weight: 600;
      }

      /* The address above it, quiet enough that a column of these still scans on the names. */
      .place-parents {
        color: var(--text-faint);
        font-size: var(--fs-xs);
      }
    `
  ]
})
export class PlaceNameComponent {
  /** Which grain this row measures. Decides both the chip and which parents are shown. */
  @Input({ required: true }) level: AreaLevel = 'Municipality';

  @Input() district = '';

  @Input() municipality = '';

  @Input() town = '';

  private get parts(): PlaceParts {
    return {
      level: this.level,
      district: this.district,
      municipality: this.municipality,
      town: this.town
    };
  }

  get own(): string {
    return placeOwnName(this.parts);
  }

  get parents(): string {
    return placeParents(this.parts);
  }

  get levelLabel(): string {
    return placeLevelLabel(this.level);
  }

  get levelHint(): string {
    return placeLevelHint(this.level);
  }
}
