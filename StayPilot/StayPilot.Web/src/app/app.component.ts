import { Component, OnDestroy, OnInit, effect, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { MsalBroadcastService, MsalService } from '@azure/msal-angular';
import { AccountInfo, InteractionStatus } from '@azure/msal-browser';
import { Subject, filter, takeUntil } from 'rxjs';
import { NAV_GROUPS } from './core/models/nav-groups';
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

  navGroups = NAV_GROUPS;
  openGroup = signal<string | null>(null);
  searchId = '';

  // Surfaced only for the footer's disclaimer line, so the "15+ listings" figure it states
  // can never drift from the actual reliability floor used across the market-area screens.
  readonly reliableListings = RELIABLE_LISTINGS;
  readonly currentYear = new Date().getFullYear();

  private readonly destroyed = new Subject<void>();
  private readonly onDocumentClick = () => this.openGroup.set(null);
  private readonly onDocumentKeydown = (e: KeyboardEvent) => {
    if (e.key === 'Escape') {
      this.openGroup.set(null);
    }
  };

  // Hover-intent for the dropdowns: opening on mouseenter is immediate (so adjacent
  // triggers swap with no flicker), but closing on mouseleave waits a beat so crossing
  // the visual gap between the trigger and the panel below it doesn't close the menu.
  private hoverCloseTimer: ReturnType<typeof setTimeout> | null = null;

  constructor(
    private readonly msal: MsalService,
    private readonly msalBroadcast: MsalBroadcastService,
    private readonly router: Router
  ) {
    effect(() => {
      const theme = this.theme();
      document.documentElement.setAttribute('data-theme', theme);
      localStorage.setItem(THEME_STORAGE_KEY, theme);
    });
  }

  ngOnInit(): void {
    this.msalBroadcast.inProgress$
      .pipe(
        filter(status => status === InteractionStatus.None),
        takeUntil(this.destroyed)
      )
      .subscribe(() => this.refreshAccount());

    this.refreshAccount();

    document.addEventListener('click', this.onDocumentClick);
    document.addEventListener('keydown', this.onDocumentKeydown);
  }

  ngOnDestroy(): void {
    this.destroyed.next();
    this.destroyed.complete();
    document.removeEventListener('click', this.onDocumentClick);
    document.removeEventListener('keydown', this.onDocumentKeydown);

    if (this.hoverCloseTimer !== null) {
      clearTimeout(this.hoverCloseTimer);
      this.hoverCloseTimer = null;
    }
  }

  toggleTheme(): void {
    this.theme.set(this.theme() === 'light' ? 'dark' : 'light');
  }

  login(): void {
    this.msal.instance.loginRedirect({ scopes: LOGIN_SCOPES });
  }

  logout(): void {
    this.msal.instance.logoutRedirect();
  }

  closeGroup(): void {
    this.openGroup.set(null);
  }

  // Entering a trigger or its panel (both live inside the same .nav-item) opens that
  // group right away and cancels any pending close from a group we just left - that's
  // what makes moving straight from one open trigger to the next feel instant.
  onGroupHoverEnter(title: string): void {
    if (this.hoverCloseTimer !== null) {
      clearTimeout(this.hoverCloseTimer);
      this.hoverCloseTimer = null;
    }

    this.openGroup.set(title);
  }

  // Delayed close, not immediate - crossing the gap between the trigger and the panel
  // below it fires a leave/enter pair, and closing immediately here would flicker.
  onGroupHoverLeave(): void {
    if (this.hoverCloseTimer !== null) {
      clearTimeout(this.hoverCloseTimer);
    }

    this.hoverCloseTimer = setTimeout(() => {
      this.openGroup.set(null);
      this.hoverCloseTimer = null;
    }, 150);
  }

  // The header search only understands a listing id for now - free-text place search would need
  // matching against MarketArea names, which is a real feature, not a header afterthought.
  runSearch(): void {
    const id = Number(this.searchId);
    if (!this.searchId || !Number.isInteger(id) || id <= 0) {
      return;
    }

    this.router.navigate(['/listings/lookup'], { queryParams: { id } });
    this.searchId = '';
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
