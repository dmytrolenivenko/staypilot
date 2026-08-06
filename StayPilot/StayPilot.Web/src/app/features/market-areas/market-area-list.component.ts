import { Component, OnInit, signal, computed } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MarketAreaService } from '../../core/services/market-area.service';
import { MarketArea } from '../../core/models/market-area';

@Component({
  selector: 'app-market-area-list',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './market-area-list.component.html',
  styleUrl: './market-area-list.component.css'
})
export class MarketAreaListComponent implements OnInit {
  areas = signal<MarketArea[]>([]);
  loading = signal(true);
  error = signal<string | null>(null);
  filterText = signal('');

  filteredAreas = computed(() => {
    const term = this.filterText().trim().toLowerCase();
    if (!term) {
      return this.areas();
    }
    return this.areas().filter(a =>
      [a.district, a.municipality, a.town, a.zone ?? ''].some(field =>
        field.toLowerCase().includes(term)
      )
    );
  });

  constructor(private readonly marketAreaService: MarketAreaService) {}

  ngOnInit(): void {
    this.marketAreaService.getAll().subscribe({
      next: areas => {
        this.areas.set(areas);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Could not load market areas — the API request failed. Check the Network tab for the actual URL and status.');
        this.loading.set(false);
      }
    });
  }
}
