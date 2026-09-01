import { Component, OnDestroy, OnInit, effect, signal } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { MsalBroadcastService, MsalService } from '@azure/msal-angular';
import { AccountInfo, InteractionStatus } from '@azure/msal-browser';
import { Subject, filter, takeUntil } from 'rxjs';

type Theme = 'light' | 'dark';

const THEME_STORAGE_KEY = 'staypilot-theme';

// Same scope the interceptor requests - keeping the account signed in and the
// first API call authorized use the same permission, so they stay in sync.
const LOGIN_SCOPES = ['api://c447c11c-f8a9-4bf5-a9b1-6d176064370c/access_as_user'];

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
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

  private readonly destroyed = new Subject<void>();

  constructor(
    private readonly msal: MsalService,
    private readonly msalBroadcast: MsalBroadcastService
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
  }

  ngOnDestroy(): void {
    this.destroyed.next();
    this.destroyed.complete();
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
