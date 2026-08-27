import { CommonModule } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { TopDealsService } from '../../core/services/top-deals.service';
import { TopDealResponse } from '../../core/models/top-deals';
import { PROPERTY_CONDITION_OPTIONS } from '../../core/models/enums';
import { AreaScope, AreaScopePickerComponent, emptyScope } from '../../shared/area-scope-picker.component';
import { PageHeaderComponent } from '../../shared/page-header.component';
import { apiErrorMessage } from '../../core/api-error';

// Top deals — the active listings asking the most below their own town's median €/m².
// Ranked on the server (it needs the market area stats table), so there is nothing to
// sort client-side here, unlike the listing browser.
@Component({
  selector: 'app-top-deals',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, AreaScopePickerComponent, PageHeaderComponent],
  templateUrl: './top-deals.component.html',
  styleUrl: './top-deals.component.css'
})
export class TopDealsComponent implements OnInit {
  readonly conditions = PROPERTY_CONDITION_OPTIONS;

  deals = signal<TopDealResponse[]>([]);
  calculatedAtUtc = signal<string | null>(null);
  loading = signal(true);
  error = signal<string | null>(null);

  scope = signal<AreaScope>(emptyScope());
  count = signal(10);

  // Renovation projects and move-in-ready homes are different markets - a fixer-upper's low
  // €/m² reflects the work it needs, not a bargain. The API already grades each listing against
  // its own bucket's median, but this narrows the list itself to one condition when set.
  condition = signal('');

  constructor(private readonly service: TopDealsService) {}

  ngOnInit(): void {
    this.load();
  }

  changeScope(scope: AreaScope): void {
    this.scope.set(scope);
    this.load();
  }

  changeCount(count: number): void {
    this.count.set(Number(count));
    this.load();
  }

  changeCondition(condition: string): void {
    this.condition.set(condition);
    this.load();
  }

  private load(): void {
    this.loading.set(true);
    this.error.set(null);

    this.service
      .getTopDeals({
        district: this.scope().district || undefined,
        municipality: this.scope().municipality || undefined,
        condition: (this.condition() || undefined) as any,
        count: this.count()
      })
      .subscribe({
        next: response => {
          this.deals.set(response.items);
          this.calculatedAtUtc.set(response.calculatedAtUtc);
          this.loading.set(false);
        },
        error: (err: HttpErrorResponse) => {
          this.deals.set([]);
          this.calculatedAtUtc.set(null);
          this.error.set(apiErrorMessage(err, 'Could not load the top deals.'));
          this.loading.set(false);
        }
      });
  }
}
