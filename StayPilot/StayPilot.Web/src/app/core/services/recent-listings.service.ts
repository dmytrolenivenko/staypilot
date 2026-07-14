import { Injectable, signal } from '@angular/core';

// Session-only convenience list (no backend "list listings" endpoint exists yet).
// Remembers ids the user has created or looked up in this browser tab, most-recent first.
@Injectable({ providedIn: 'root' })
export class RecentListingsService {
  private readonly maxEntries = 10;
  readonly ids = signal<number[]>([]);

  remember(id: number): void {
    const withoutDuplicate = this.ids().filter(existing => existing !== id);
    this.ids.set([id, ...withoutDuplicate].slice(0, this.maxEntries));
  }
}
