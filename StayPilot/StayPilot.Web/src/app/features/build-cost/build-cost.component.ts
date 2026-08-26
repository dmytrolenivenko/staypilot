import { CommonModule } from '@angular/common';
import { Component, computed, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import {
  BuildCostBasis,
  BuildCostOption,
  DEFAULT_CONTINGENCY_PERCENT,
  DEFAULT_DESIGN_PERCENT,
  DEFAULT_LICENCE_PERCENT,
  ESTIMATE_HIGH_FACTOR,
  ESTIMATE_LOW_FACTOR,
  estimatedMonths,
  Region,
  REGIONS
} from '../../core/models/build-cost';
import { AreaLevel, MarketAreaStatsResponse } from '../../core/models/market-area-stats';
import { BuildCostService } from '../../core/services/build-cost.service';
import { MarketAreaStatsService } from '../../core/services/market-area-stats.service';
import { AreaScope, AreaScopePickerComponent, emptyScope } from '../../shared/area-scope-picker.component';
import { ExplainerComponent } from '../../shared/explainer.component';
import { PageHeaderComponent } from '../../shared/page-header.component';

// What a line of the receipt is made of. Every group produces these, so the table, the
// composition bar and the totals all read from one list instead of each doing the sums again.
type CostGroup = 'works' | 'soft' | 'tax' | 'grant' | 'land';

interface CostLine {
  key: string;
  group: CostGroup;
  label: string;
  // The arithmetic, spelled out: "1,343 €/m² × 150 m² × 1.15". A total nobody can take apart is
  // a number to argue with, not a number to plan on.
  working: string;
  amount: number;
}

interface CompositionSlice {
  group: CostGroup;
  label: string;
  amount: number;
  percent: number;
}

const GROUP_LABELS: Record<CostGroup, string> = {
  works: 'Works',
  soft: 'Design, licences & contingency',
  tax: 'VAT',
  grant: 'Grants',
  land: 'Land'
};

// The "no thanks" row every dropdown needs. The API only sends real choices — it has no opinion
// about a house without a pool, and a zero-priced row in a contract is UI leaking into data.
const NONE_KEY = '__none';

function noneOption(label: string): BuildCostOption {
  return { key: NONE_KEY, label, note: '' };
}

function euros(value: number): string {
  return `€${Math.round(value).toLocaleString('en-GB')}`;
}

function rate(value: number): string {
  return `€${Math.round(value).toLocaleString('en-GB')}/m²`;
}

// Lowercases only the first letter, so a label reads as part of the sentence around it
// ("Pool — concrete") without flattening the units inside it. A blanket toLowerCase() turned
// "6 kWp + 10 kWh battery" into "6 kwp + 10 kwh battery".
function sentenceCase(label: string): string {
  return label.charAt(0).toLowerCase() + label.slice(1);
}

// Build Cost — "what would it cost to build this from scratch, and would I be better off just
// buying one?"
//
// No price on this screen is stored. The API derives every rate from INE's construction cost
// index applied to a small set of anchors, so when INE publishes a new month the screen moves on
// its own. The arithmetic over your own choices stays here, so it recomputes as you type.
//
// Designed to be argued with: every line shows its working, the solar grant is a visible line
// rather than a quiet discount, and the headline carries a −15%/+25% band because a formula
// fitted to published mid-points is not a builder's quote.
@Component({
  selector: 'app-build-cost',
  standalone: true,
  imports: [CommonModule, FormsModule, PageHeaderComponent, ExplainerComponent, AreaScopePickerComponent],
  templateUrl: './build-cost.component.html',
  styleUrl: './build-cost.component.css'
})
export class BuildCostComponent implements OnInit {
  readonly regions = REGIONS;

  // Common water surfaces, so the m² box starts somewhere real. Sizes, not prices — there is
  // nothing here for an index to escalate, which is why these stayed in the browser.
  readonly poolPresets = [
    { label: 'Plunge 4×2', areaM2: 8 },
    { label: 'Small 6×3', areaM2: 18 },
    { label: 'Family 8×4', areaM2: 32 },
    { label: 'Large 10×5', areaM2: 50 }
  ];

  // --- Live rates ----------------------------------------------------------
  basis = signal<BuildCostBasis | null>(null);
  loading = signal(true);
  error = signal<string | null>(null);

  // --- The house -----------------------------------------------------------
  areaM2 = signal(150);
  tierKey = signal('standard');
  region = signal<Region>(REGIONS[1]); // Algarve — the market this tool is about

  // --- The extras ----------------------------------------------------------
  poolKey = signal(NONE_KEY);
  poolAreaM2 = signal(32); // a family 8×4, so switching the pool on shows something sane
  selectedPoolAddons = signal<Set<string>>(new Set());
  garageKey = signal(NONE_KEY);
  elevatorKey = signal(NONE_KEY);
  automationKey = signal(NONE_KEY);
  gardenKey = signal(NONE_KEY);
  solarKey = signal(NONE_KEY);
  selectedExtras = signal<Set<string>>(new Set());

  // --- Soft costs ----------------------------------------------------------
  designPct = signal(DEFAULT_DESIGN_PERCENT);
  licencePct = signal(DEFAULT_LICENCE_PERCENT);
  contingencyPct = signal(DEFAULT_CONTINGENCY_PERCENT);
  includeVat = signal(true);

  // --- The land ------------------------------------------------------------
  // Typed by hand today. Land listings are what eventually fill these in: a plot advert already
  // carries an area and an asking price, which is exactly this line.
  includeLand = signal(false);
  plotAreaM2 = signal(500);
  plotPricePerM2 = signal(250);

  // --- Build vs buy --------------------------------------------------------
  scope = signal<AreaScope>(emptyScope());
  comparison = signal<MarketAreaStatsResponse | null>(null);
  comparisonLoading = signal(false);

  constructor(
    private readonly buildCostService: BuildCostService,
    private readonly statsService: MarketAreaStatsService
  ) {}

  ngOnInit(): void {
    this.buildCostService.getBasis().subscribe({
      next: basis => {
        this.basis.set(basis);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Could not read the current build rates.');
        this.loading.set(false);
      }
    });
  }

  // --- The option lists ----------------------------------------------------
  //
  // Each prepends its own "none" row, because a house without a lift is a UI state rather than a
  // priced product.

  tiers = computed(() => this.basis()?.tiers ?? []);
  pools = computed(() => [noneOption('No pool'), ...(this.basis()?.pools ?? [])]);
  poolAddons = computed(() => this.basis()?.poolAddons ?? []);
  garages = computed(() => [noneOption('No garage'), ...(this.basis()?.garages ?? [])]);
  elevators = computed(() => [noneOption('No elevator'), ...(this.basis()?.elevators ?? [])]);
  automationLevels = computed(() => [noneOption('None'), ...(this.basis()?.automation ?? [])]);
  gardens = computed(() => [noneOption('No garden'), ...(this.basis()?.gardens ?? [])]);
  solarOptions = computed(() => [noneOption('No solar'), ...(this.basis()?.solar ?? [])]);
  extras = computed(() => this.basis()?.extras ?? []);

  tier = computed(() => this.pick(this.tiers(), this.tierKey()));
  pool = computed(() => this.pick(this.pools(), this.poolKey()));
  garage = computed(() => this.pick(this.garages(), this.garageKey()));
  elevator = computed(() => this.pick(this.elevators(), this.elevatorKey()));
  automation = computed(() => this.pick(this.automationLevels(), this.automationKey()));
  garden = computed(() => this.pick(this.gardens(), this.gardenKey()));
  solar = computed(() => this.pick(this.solarOptions(), this.solarKey()));

  // --- Works ---------------------------------------------------------------

  poolShellCost = computed(() => {
    const pool = this.pool();

    if (!pool?.ratePerM2) {
      return 0;
    }

    // The floor is why this is not just rate × area: excavation, plant room and filtration cost
    // roughly the same whatever size the water is.
    return Math.max(pool.minCost ?? 0, pool.ratePerM2 * Math.max(0, this.poolAreaM2() || 0));
  });

  landCost = computed(() =>
    this.includeLand() ? Math.max(0, this.plotAreaM2() || 0) * Math.max(0, this.plotPricePerM2() || 0) : 0
  );

  months = computed(() =>
    estimatedMonths(this.builtArea(), this.poolShellCost() > 0, (this.elevator()?.cost ?? 0) > 0)
  );

  // Every line, in the order the receipt reads. Amounts already carry the regional multiplier
  // where it applies, so nothing downstream has to remember which items it touches.
  lines = computed<CostLine[]>(() => {
    const basis = this.basis();

    if (!basis) {
      return [];
    }

    const area = this.builtArea();
    const site = this.region().multiplier;
    const rows: CostLine[] = [];
    const tier = this.tier();

    if (tier?.ratePerM2) {
      rows.push({
        key: 'shell',
        group: 'works',
        label: 'Construction (shell & finishes)',
        working: `${rate(tier.ratePerM2)} × ${area} m² × ${site} (${this.region().label})`,
        amount: tier.ratePerM2 * area * site
      });
    }

    if (this.poolShellCost() > 0) {
      const pool = this.pool()!;
      const surface = Math.max(0, this.poolAreaM2() || 0);
      const atFloor = (pool.ratePerM2 ?? 0) * surface < (pool.minCost ?? 0);

      rows.push({
        key: 'pool',
        group: 'works',
        label: `Pool — ${sentenceCase(pool.label)}`,
        working: atFloor
          ? `minimum ${euros(pool.minCost ?? 0)} (${surface} m² × ${rate(pool.ratePerM2 ?? 0)} is below it) × ${site}`
          : `${surface} m² water × ${rate(pool.ratePerM2 ?? 0)} × ${site}`,
        amount: this.poolShellCost() * site
      });

      const addons = this.poolAddons().filter(addon => this.selectedPoolAddons().has(addon.key));

      if (addons.length > 0) {
        rows.push({
          key: 'pool-addons',
          group: 'works',
          label: 'Pool equipment',
          working: addons.map(addon => addon.label).join(', '),
          amount: addons.reduce((sum, addon) => sum + (addon.cost ?? 0), 0)
        });
      }
    }

    this.pushFixed(rows, this.garage(), 'Garage', site);
    this.pushFixed(rows, this.garden(), 'Garden', site);

    // Equipment is bought at national prices, so the regional multiplier is left off it —
    // inflating a lift by a fifth for being in Lisboa would be inventing a number.
    this.pushFixed(rows, this.elevator(), 'Elevator', 1, 'equipment price, installed');
    this.pushFixed(rows, this.solar(), 'Solar', 1, 'turnkey, before the grant below');

    const automation = this.automation();

    if (automation?.ratePerM2) {
      rows.push({
        key: 'automation',
        group: 'works',
        label: `Home automation — ${automation.label}`,
        working: `${rate(automation.ratePerM2)} × ${area} m²`,
        amount: automation.ratePerM2 * area
      });
    }

    for (const extra of this.extras()) {
      if (!this.selectedExtras().has(extra.key)) {
        continue;
      }

      const isPerM2 = extra.ratePerM2 !== undefined;

      rows.push({
        key: extra.key,
        group: 'works',
        label: extra.label,
        working: isPerM2 ? `${rate(extra.ratePerM2!)} × ${area} m² × ${site}` : `${euros(extra.cost ?? 0)} × ${site}`,
        amount: (isPerM2 ? extra.ratePerM2! * area : (extra.cost ?? 0)) * site
      });
    }

    // Soft costs sit on the works total, which is how architects and câmaras actually bill.
    const works = rows.reduce((sum, row) => sum + row.amount, 0);

    rows.push({
      key: 'design',
      group: 'soft',
      label: 'Architecture & engineering',
      working: `${this.designPct() || 0}% of ${euros(works)} of works`,
      amount: works * this.percent(this.designPct())
    });

    rows.push({
      key: 'licences',
      group: 'soft',
      label: 'Licences & municipal fees',
      working: `${this.licencePct() || 0}% of works`,
      amount: works * this.percent(this.licencePct())
    });

    rows.push({
      key: 'contingency',
      group: 'soft',
      label: 'Contingency',
      working: `${this.contingencyPct() || 0}% of works`,
      amount: works * this.percent(this.contingencyPct())
    });

    if (this.includeVat()) {
      const taxable = rows.reduce((sum, row) => sum + row.amount, 0);

      rows.push({
        key: 'vat',
        group: 'tax',
        label: `VAT (IVA ${basis.vatPercent}%)`,
        working: `on ${euros(taxable)} of works and fees`,
        amount: taxable * (basis.vatPercent / 100)
      });
    }

    const solarGrant = this.solar()?.grant ?? 0;

    if (solarGrant > 0) {
      rows.push({
        key: 'grant',
        group: 'grant',
        label: 'Solar grant (comparticipação)',
        working: 'state support for panels and storage, 2026 ceiling',
        amount: -solarGrant
      });
    }

    if (this.landCost() > 0) {
      rows.push({
        key: 'land',
        group: 'land',
        label: 'Land',
        working: `${this.plotAreaM2() || 0} m² plot × ${rate(this.plotPricePerM2() || 0)}`,
        amount: this.landCost()
      });
    }

    return rows;
  });

  // --- Totals --------------------------------------------------------------

  worksTotal = computed(() => this.sumOf('works'));
  softTotal = computed(() => this.sumOf('soft'));
  vatTotal = computed(() => this.sumOf('tax'));

  total = computed(() => this.lines().reduce((sum, row) => sum + row.amount, 0));

  // Land is a purchase, not a build. Out of the €/m² compared against a builder's rate, back in
  // for the comparison against what a finished house asks.
  buildOnlyTotal = computed(() => this.total() - this.landCost());

  // The band is uncertainty in the BUILD - what a builder might actually quote against a formula.
  // Land carries none of that: it is a price you typed in because you already know it. Widening
  // it by -15/+25% invents doubt about the one figure here that has none, so band the
  // construction and add the plot back at face value.
  low = computed(() => this.buildOnlyTotal() * ESTIMATE_LOW_FACTOR + this.landCost());
  high = computed(() => this.buildOnlyTotal() * ESTIMATE_HIGH_FACTOR + this.landCost());

  allInRatePerM2 = computed(() => (this.builtArea() > 0 ? this.total() / this.builtArea() : 0));
  buildRatePerM2 = computed(() => (this.builtArea() > 0 ? this.buildOnlyTotal() / this.builtArea() : 0));

  // Grants are left out: a negative segment cannot be drawn, and drawing it positive would
  // misread the picture.
  composition = computed<CompositionSlice[]>(() => {
    const groups: CostGroup[] = ['works', 'soft', 'tax', 'land'];
    const amounts = groups.map(group => ({ group, amount: this.sumOf(group) })).filter(slice => slice.amount > 0);
    const drawn = amounts.reduce((sum, slice) => sum + slice.amount, 0);

    return amounts.map(slice => ({
      group: slice.group,
      label: GROUP_LABELS[slice.group],
      amount: slice.amount,
      percent: drawn > 0 ? (slice.amount / drawn) * 100 : 0
    }));
  });

  // --- Build vs buy --------------------------------------------------------

  marketValue = computed(() => {
    const median = this.comparison()?.medianPricePerM2;

    return median ? median * this.builtArea() : 0;
  });

  buildSaving = computed(() => (this.marketValue() > 0 ? this.marketValue() - this.total() : 0));

  buildSavingPercent = computed(() =>
    this.marketValue() > 0 ? (this.buildSaving() / this.marketValue()) * 100 : 0
  );

  // The comparison is only fair once the land is priced in — building excludes the ground.
  comparisonIsLandless = computed(() => this.marketValue() > 0 && this.landCost() === 0);

  changeScope(scope: AreaScope): void {
    this.scope.set(scope);

    if (!scope.district) {
      this.comparison.set(null);

      return;
    }

    const level: AreaLevel = scope.municipality ? 'Municipality' : 'District';

    this.comparisonLoading.set(true);

    this.statsService
      .getLeaderboard({ level, minListings: 3, district: scope.district, municipality: scope.municipality })
      .subscribe({
        next: response => {
          // One place at one level, so the first row is the row — but a place with too few
          // listings is filtered out server-side, and that has to read as "not measured".
          this.comparison.set(response.items[0] ?? null);
          this.comparisonLoading.set(false);
        },
        error: () => {
          this.comparison.set(null);
          this.comparisonLoading.set(false);
        }
      });
  }

  // --- Input plumbing ------------------------------------------------------

  isPoolAddonSelected(key: string): boolean {
    return this.selectedPoolAddons().has(key);
  }

  togglePoolAddon(key: string): void {
    this.selectedPoolAddons.set(toggled(this.selectedPoolAddons(), key));
  }

  isExtraSelected(key: string): boolean {
    return this.selectedExtras().has(key);
  }

  toggleExtra(key: string): void {
    this.selectedExtras.set(toggled(this.selectedExtras(), key));
  }

  // A shortcut into the m² box, not a separate mode — you can still type 27 afterwards.
  applyPoolPreset(areaM2: number): void {
    this.poolAreaM2.set(areaM2);
  }

  // --- Labels --------------------------------------------------------------
  //
  // A dropdown reads "Reinforced concrete — €1,102/m²": the price belongs next to the choice, not
  // three sections further down.

  optionLabel(option: BuildCostOption): string {
    if (option.ratePerM2) {
      return `${option.label} — ${rate(option.ratePerM2)}`;
    }

    return option.cost ? `${option.label} — ${euros(option.cost)}` : option.label;
  }

  extraPriceLabel(extra: BuildCostOption): string {
    return extra.ratePerM2 ? rate(extra.ratePerM2) : euros(extra.cost ?? 0);
  }

  // Grants arrive as negative amounts, and a minus in front of a euro figure reads better than a
  // bracketed one, so the sign is written here rather than fought with in the table.
  signed(amount: number): string {
    return amount < 0 ? `−${euros(-amount)}` : euros(amount);
  }

  // The direction is already in the words beside it ("cheaper to build" / "cheaper to buy"), so
  // the percentage is written unsigned rather than saying the same thing twice.
  absPercent(value: number): string {
    return `${Math.abs(Math.round(value))}%`;
  }

  // --- Internals -----------------------------------------------------------

  private builtArea = computed(() => Math.max(0, this.areaM2() || 0));

  private pick(options: BuildCostOption[], key: string): BuildCostOption | null {
    return options.find(option => option.key === key) ?? null;
  }

  private percent(value: number): number {
    return Math.max(0, value || 0) / 100;
  }

  private sumOf(group: CostGroup): number {
    return this.lines()
      .filter(row => row.group === group)
      .reduce((sum, row) => sum + row.amount, 0);
  }

  /** One flat-priced choice, skipped when it is the "none" row. */
  private pushFixed(
    rows: CostLine[],
    option: BuildCostOption | null,
    label: string,
    multiplier: number,
    working?: string
  ): void {
    if (!option?.cost) {
      return;
    }

    rows.push({
      key: option.key,
      group: 'works',
      label: `${label} — ${sentenceCase(option.label)}`,
      working: working ?? `${euros(option.cost)} × ${multiplier}`,
      amount: option.cost * multiplier
    });
  }
}

function toggled(source: Set<string>, key: string): Set<string> {
  const next = new Set(source);

  if (next.has(key)) {
    next.delete(key);
  } else {
    next.add(key);
  }

  return next;
}
