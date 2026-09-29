import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';

/** One choice in a <app-segmented>. */
export interface SegmentedOption<T extends string> {
  value: T;
  label: string;
  /** The question this choice answers, shown under the label. Optional — narrow strips omit it. */
  hint?: string;
}

/**
 * The strip that picks which question a screen is answering.
 *
 * Six screens were merged into one each because they were four renderings of one table, or two
 * halves of one object. What used to be a navigation choice is this control: the filters above it
 * stay put, and only the answer below it changes. That is the whole point of the merge — picking
 * a different question should not mean setting the same three filters again.
 *
 * Tabs semantically, so a keyboard and a screen reader read it as one control with one choice
 * made, not as four unrelated buttons.
 *
 *   <app-segmented [options]="lenses" [value]="lens()" (valueChange)="pickLens($event)" />
 */
@Component({
  selector: 'app-segmented',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="segmented" role="tablist" [attr.aria-label]="label">
      @for (option of options; track option.value) {
        <button
          type="button"
          role="tab"
          class="segment"
          [class.active]="option.value === value"
          [attr.aria-selected]="option.value === value"
          (click)="pick(option.value)"
        >
          <span class="segment-label">{{ option.label }}</span>
          @if (option.hint) {
            <span class="segment-hint">{{ option.hint }}</span>
          }
        </button>
      }
    </div>
  `
})
export class SegmentedComponent<T extends string> {
  @Input({ required: true }) options: SegmentedOption<T>[] = [];
  @Input({ required: true }) value!: T;

  /** What the strip as a whole is choosing between, for screen readers. */
  @Input() label = '';

  @Output() valueChange = new EventEmitter<T>();

  pick(value: T): void {
    if (value !== this.value) {
      this.valueChange.emit(value);
    }
  }
}
