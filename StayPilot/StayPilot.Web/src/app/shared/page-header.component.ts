import { Component, Input } from '@angular/core';

/**
 * The top of every screen: title, one-line subtitle, optional count/timestamp,
 * and a slot for the screen's primary actions.
 *
 * Anything longer than one line belongs in <app-explainer>, not in `sub`.
 *
 *   <app-page-header title="Leaderboard" sub="Places ranked on median €/m²." [meta]="'153 places'">
 *     <button actions type="button">Recalculate</button>
 *   </app-page-header>
 */
@Component({
  selector: 'app-page-header',
  standalone: true,
  template: `
    <header class="page-head">
      <div class="page-head-text">
        <h1>{{ title }}</h1>
        @if (sub) {
          <p class="page-sub">{{ sub }}</p>
        }
        @if (meta) {
          <span class="meta">{{ meta }}</span>
        }
      </div>
      <div class="page-head-actions">
        <ng-content select="[actions]" />
      </div>
    </header>
  `
})
export class PageHeaderComponent {
  @Input({ required: true }) title = '';
  @Input() sub?: string;
  @Input() meta?: string;
}
