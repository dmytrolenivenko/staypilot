import { Component, Input } from '@angular/core';

/**
 * Collapsed background reading. The analysis screens each carry a few
 * paragraphs of "how to read this" — true, and worth keeping, but they used to
 * sit between the title and the controls and pushed the actual answer off the
 * screen. Here they are one click away instead.
 *
 *   <app-explainer>
 *     <p>Ranked on median €/m², not median asking price…</p>
 *   </app-explainer>
 */
@Component({
  selector: 'app-explainer',
  standalone: true,
  template: `
    <details class="explainer">
      <summary>{{ label }}</summary>
      <div class="note">
        <ng-content />
      </div>
    </details>
  `,
  styles: [
    `
      .explainer {
        border: 1px solid var(--border);
        border-radius: var(--r-sm);
        background: var(--surface-2);
      }

      summary {
        padding: var(--sp-2) var(--sp-4);
        color: var(--text-muted);
        font-size: var(--fs-sm);
        font-weight: 550;
        cursor: pointer;
        list-style: none;
      }

      summary::-webkit-details-marker {
        display: none;
      }

      /* A caret that rotates open, so the row reads as expandable. */
      summary::before {
        content: '›';
        display: inline-block;
        margin-right: var(--sp-2);
        transition: transform 0.15s ease;
      }

      details[open] summary::before {
        transform: rotate(90deg);
      }

      summary:hover {
        color: var(--text);
      }

      .note {
        border-left: none;
        padding: 0 var(--sp-4) var(--sp-4) var(--sp-5);
      }
    `
  ]
})
export class ExplainerComponent {
  @Input() label = 'How to read this';
}
