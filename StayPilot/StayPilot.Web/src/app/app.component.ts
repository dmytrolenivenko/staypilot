import { Component, OnDestroy, OnInit, computed, effect, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { NavigationEnd, Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { MsalBroadcastService, MsalService } from '@azure/msal-angular';
import { AccountInfo, InteractionStatus } from '@azure/msal-browser';
import { Subject, filter, takeUntil } from 'rxjs';
import { NAV_LINKS, NavLink } from './core/models/nav-groups';
import { MarketAreaService } from './core/services/market-area.service';
import { RELIABLE_LISTINGS } from './core/models/market-area-stats';

type Theme = 'light' | 'dark';

const THEME_STORAGE_KEY = 'staypilot-theme';

// Same scope the interceptor requests - keeping the account signed in and the
// first API call authorized use the same permission, so they stay in sync.
const LOGIN_SCOPES = ['api://c447c11c-f8a9-4bf5-a9b1-6d176064370c/access_as_user'];

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive, FormsModule],
  templateUrl: './app.component.html',
  styleUrl: './app.component.css'
})
export class AppComponent implements OnInit, OnDestroy {
  theme = signal<Theme>(localStorage.getItem(THEME_STORAGE_KEY) === 'dark' ? 'dark' : 'light');

  // Null means signed out. Re-read whenever MSAL finishes any login/redirect
  // work, not just once at startup - the account only exists after a
  // loginRedirect() round trip completes.
  isSignedIn = signal(false);
  accountName = signal<string | null>(null);
  accountEmail = signal<string | null>(null);

  navLinks = NAV_LINKS;

  // The nav collapses behind a button below 900px. Held here rather than in CSS
  // because the same state closes it again on navigation and on Escape.
  menuOpen = signal(false);

  // Whether the account menu (name, email, sign out) is showing. On a phone
  // there is no room to print an email address in the bar itself.
  accountOpen = signal(false);

  // The current URL, updated on every completed navigation. The sub-nav reads
  // it to know which destination is active; routerLinkActive cannot help here
  // because the strip has to exist before any of its own links are active.
  // Seeded in the constructor, not here: a field initializer runs before the
  // constructor's parameter properties are assigned, so `this.router` would
  // still be undefined at this point.
  private readonly url = signal('');

  /**
   * Home only. It is the one screen that draws its own full-bleed bands — a
   * dark CTA, a grey trust strip — so the shell drops its content column and
   * its padding and lets the screen run to the window's edges.
   */
  isHome = computed(() => this.url().split('?')[0] === '/');

  // What was typed in the header box: a place name, or a listing id.
  searchText = '';
  searchError = signal<string | null>(null);
  searching = signal(false);

  // Surfaced only for the footer's disclaimer line, so the "15+ listings" figure it states
  // can never drift from the actual reliability floor used across the market-area screens.
  readonly reliableListings = RELIABLE_LISTINGS;
  readonly currentYear = new Date().getFullYear();

  private readonly destroyed = new Subject<void>();

  constructor(
    private readonly msal: MsalService,
    private readonly msalBroadcast: MsalBroadcastService,
    private readonly router: Router,
    private readonly marketAreas: MarketAreaService
  ) {
    this.url.set(this.router.url);

    effect(() => {
      const theme = this.theme();
      document.documentElement.setAttribute('data-theme', theme);
      localStorage.setItem(THEME_STORAGE_KEY, theme);
    });

    // An open menu must not survive the navigation it triggered, or a phone
    // lands on the new screen with the menu still covering it.
    effect(() => {
      this.url();
      this.menuOpen.set(false);
      this.accountOpen.set(false);
    });
  }

  ngOnInit(): void {
    this.msalBroadcast.inProgress$
      .pipe(
        filter(status => status === InteractionStatus.None),
        takeUntil(this.destroyed)
      )
      .subscribe(() => this.refreshAccount());

    this.router.events
      .pipe(
        filter((event): event is NavigationEnd => event instanceof NavigationEnd),
        takeUntil(this.destroyed)
      )
      .subscribe(event => this.url.set(event.urlAfterRedirects));

    this.refreshAccount();
  }

  ngOnDestroy(): void {
    this.destroyed.next();
    this.destroyed.complete();
  }

  toggleTheme(): void {
    this.theme.set(this.theme() === 'light' ? 'dark' : 'light');
  }

  toggleMenu(): void {
    this.menuOpen.set(!this.menuOpen());
  }

  toggleAccount(): void {
    this.accountOpen.set(!this.accountOpen());
  }

  /** Escape closes whatever is open, from anywhere in the bar. */
  closeOverlays(): void {
    this.menuOpen.set(false);
    this.accountOpen.set(false);
  }

  login(): void {
    this.msal.instance.loginRedirect({ scopes: LOGIN_SCOPES });
  }

  logout(): void {
    this.msal.instance.logoutRedirect();
  }

  /** The initials shown in the account button when there is no room for a name. */
  accountInitials(): string {
    const name = this.accountName() ?? this.accountEmail() ?? '';
    const parts = name.split(/[\s@.]+/).filter(Boolean);

    if (parts.length === 0) {
      return '?';
    }

    return (parts[0][0] + (parts[1]?.[0] ?? '')).toUpperCase();
  }

  /**
   * The header search. A place name, or a listing id.
   *
   * It used to take a listing id and nothing else, which is a number nobody has for a listing
   * they have not found yet — the one thing people type into a search box on a property site is
   * where they are looking. A number is still understood, because the tables print ids and
   * pasting one back in is the fastest way to reopen a listing.
   */
  runSearch(): void {
    const text = this.searchText.trim();

    if (!text) {
      return;
    }

    this.searchError.set(null);

    // All digits: a listing id. Anything else is a place.
    if (/^\d+$/.test(text)) {
      this.router.navigate(['/listings', Number(text)]);
      this.searchText = '';

      return;
    }

    this.searching.set(true);

    // One match is all a search box needs: it takes you somewhere, and every screen it lands on
    // has its own place picker to move from there.
    this.marketAreas.getPage({ search: text, pageNumber: 1, pageSize: 1 }).subscribe({
      next: page => {
        this.searching.set(false);

        const match = page.items[0];

        if (!match) {
          this.searchError.set(`Nothing found for “${text}”.`);

          return;
        }

        this.searchText = '';
        this.router.navigate(['/places/overview'], {
          queryParams: {
            district: match.district,
            municipality: match.municipality,
            town: match.town
          }
        });
      },
      error: () => {
        this.searching.set(false);
        this.searchError.set('Could not search right now.');
      }
    });
  }

  // MSAL can hold several accounts (e.g. leftover from a previous tenant's
  // login) but has no active one selected until we pick it - without this,
  // acquireTokenSilent in the interceptor has no account to renew a token for.
  private refreshAccount(): void {
    const account = this.msal.instance.getActiveAccount() ?? this.msal.instance.getAllAccounts()[0] ?? null;

    if (account && !this.msal.instance.getActiveAccount()) {
      this.msal.instance.setActiveAccount(account);
    }

    this.isSignedIn.set(!!account);
    this.accountName.set(account?.name ?? null);
    this.accountEmail.set(account ? this.extractEmail(account) : null);
  }

  // External ID's local (email+password) accounts don't reliably put an email
  // in the username field the way workforce accounts do - some come through
  // in the ID token's "emails" claim instead. Try username first since it's
  // the normal case, fall back to that claim.
  private extractEmail(account: AccountInfo): string | null {
    if (account.username?.includes('@')) {
      return account.username;
    }

    const claims = account.idTokenClaims as Record<string, unknown> | undefined;
    const emails = claims?.['emails'];

    return Array.isArray(emails) && emails.length > 0 ? String(emails[0]) : null;
  }
}
