import { CommonModule } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MarketAreaStatsService } from '../../core/services/market-area-stats.service';
import { AreaLevel, NeighbourGapResponse } from '../../core/models/market-area-stats';

// Neighbour gaps — pairs of places close enough to be a real choice, priced far enough apart to
// be worth the move. "Live 3km further and pay 37% less."
//
// Paired on the server, not here: it is pairwise work across the whole level, and the middle
// points it needs are an internal detail rather than something a screen should hold.
@Component({
  selector: 'app-market-area-neighbours',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './market-area-neighbours.component.html',
  styleUrl: './market-area-neighbours.component.css'
})
export class MarketAreaNeighboursComponent implements OnInit {
  readonly levels: AreaLevel[] = ['District', 'Municipality', 'Town'];

  gaps = signal<NeighbourGapResponse[]>([]);
  calculatedAtUtc = signal<string | null>(null);
  loading = signal(true);
  error = signal<string | null>(null);

  level = signal<AreaLevel>('Municipality');
  minListings = signal(5);
  maxDistanceKm = signal(25);
  minGapPercent = signal(20);

  constructor(private readonly service: MarketAreaStatsService) {}

  ngOnInit(): void {
    this.load();
  }

  changeLevel(level: AreaLevel): void {
    this.level.set(level);
    this.load();
  }

  changeMinListings(minListings: number): void {
    this.minListings.set(Number(minListings));
    this.load();
  }

  changeMaxDistance(maxDistanceKm: number): void {
    this.maxDistanceKm.set(Number(maxDistanceKm));
    this.load();
  }

  changeMinGap(minGapPercent: number): void {
    this.minGapPercent.set(Number(minGapPercent));
    this.load();
  }

  // What you would save per m² by moving to the cheaper side.
  saving(gap: NeighbourGapResponse): number {
    return gap.expensivePricePerM2 - gap.cheaperPricePerM2;
  }

  private load(): void {
    this.loading.set(true);
    this.error.set(null);

    this.service
      .getNeighbourGaps({
        level: this.level(),
        minListings: this.minListings(),
        maxDistanceKm: this.maxDistanceKm(),
        minGapPercent: this.minGapPercent()
      })
      .subscribe({
        next: response => {
          this.gaps.set(response.items);
          this.calculatedAtUtc.set(response.calculatedAtUtc);
          this.loading.set(false);
        },
        error: () => {
          this.error.set('Could not load the neighbour gaps. Check the API is running.');
          this.loading.set(false);
        }
      });
  }
}
