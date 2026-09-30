import {
  AfterViewInit,
  Component,
  ElementRef,
  EventEmitter,
  Input,
  OnChanges,
  OnDestroy,
  Output,
  ViewChild,
  ViewEncapsulation
} from '@angular/core';
import type * as Leaflet from 'leaflet';

export interface LatLng {
  lat: number;
  lng: number;
}

// Mainland Portugal, for when there are no coordinates yet.
const PORTUGAL_CENTER: Leaflet.LatLngTuple = [39.6, -8.0];
const PORTUGAL_ZOOM = 6;
const PLACED_ZOOM = 16;

/**
 * Click the map (or drag the pin) to pick coordinates, instead of copy-pasting them from a
 * maps site. OpenStreetMap tiles, no API key.
 *
 * Leaflet is loaded on demand, so it only costs bytes once someone actually opens the map.
 * The pin is a divIcon: Leaflet's default marker PNG does not survive the Angular build.
 */
@Component({
  selector: 'app-location-picker',
  standalone: true,
  // Leaflet builds its own DOM, so the pin styles cannot be view-encapsulated.
  encapsulation: ViewEncapsulation.None,
  template: `<div #mapHost class="location-picker"></div>`,
  styles: [
    `
      .location-picker {
        height: 420px;
        border: 1px solid var(--border-strong);
        border-radius: var(--r-sm);
        overflow: hidden;
      }

      .location-pin {
        width: 18px;
        height: 18px;
        border-radius: 50%;
        background: var(--accent);
        border: 3px solid #fff;
        box-shadow: 0 1px 4px rgba(0, 0, 0, 0.45);
      }
    `
  ]
})
export class LocationPickerComponent implements AfterViewInit, OnChanges, OnDestroy {
  @Input() lat: number | null | undefined = null;

  @Input() lng: number | null | undefined = null;

  /** Fired on every map click or pin drag, rounded to 6 decimals (~10 cm). */
  @Output() picked = new EventEmitter<LatLng>();

  @ViewChild('mapHost', { static: true }) private mapHost!: ElementRef<HTMLDivElement>;

  private L?: typeof Leaflet;
  private map?: Leaflet.Map;
  private marker?: Leaflet.Marker;
  private destroyed = false;

  async ngAfterViewInit(): Promise<void> {
    const module = await import('leaflet');
    const L = ((module as any).default ?? module) as typeof Leaflet;

    if (this.destroyed) {
      return;
    }

    this.L = L;

    const start = this.currentPoint();
    this.map = L.map(this.mapHost.nativeElement).setView(
      start ?? PORTUGAL_CENTER,
      start ? PLACED_ZOOM : PORTUGAL_ZOOM
    );

    L.tileLayer('https://tile.openstreetmap.org/{z}/{x}/{y}.png', {
      maxZoom: 19,
      attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>'
    }).addTo(this.map);

    this.map.on('click', event => this.pick(event.latlng));

    if (start) {
      this.placeMarker(start);
    }

    // The host may have just become visible; let Leaflet re-measure it.
    setTimeout(() => this.map?.invalidateSize());
  }

  // Typed coordinates move the pin too, so the map and the inputs never disagree.
  ngOnChanges(): void {
    const point = this.currentPoint();

    if (!this.map || !point) {
      return;
    }

    this.placeMarker(point);

    if (!this.map.getBounds().contains(point)) {
      this.map.setView(point, Math.max(this.map.getZoom(), PLACED_ZOOM));
    }
  }

  ngOnDestroy(): void {
    this.destroyed = true;
    this.map?.remove();
  }

  private pick(latlng: Leaflet.LatLng): void {
    const point: LatLng = {
      lat: Number(latlng.lat.toFixed(6)),
      lng: Number(latlng.lng.toFixed(6))
    };

    this.placeMarker([point.lat, point.lng]);
    this.picked.emit(point);
  }

  private placeMarker(point: Leaflet.LatLngTuple): void {
    if (this.marker) {
      this.marker.setLatLng(point);
      return;
    }

    const L = this.L!;
    const icon = L.divIcon({ className: 'location-pin', iconSize: [18, 18] });

    this.marker = L.marker(point, { icon, draggable: true }).addTo(this.map!);
    this.marker.on('dragend', () => this.pick(this.marker!.getLatLng()));
  }

  // The inputs as a point, or null while either is empty or out of range.
  private currentPoint(): Leaflet.LatLngTuple | null {
    const lat = this.lat;
    const lng = this.lng;

    if (lat == null || lng == null || !Number.isFinite(lat) || !Number.isFinite(lng)) {
      return null;
    }

    if (Math.abs(lat) > 90 || Math.abs(lng) > 180) {
      return null;
    }

    return [lat, lng];
  }
}
