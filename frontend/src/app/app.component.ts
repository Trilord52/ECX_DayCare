import { Component, OnInit, inject } from '@angular/core';
import { Router, RouterOutlet, NavigationEnd } from '@angular/router';
import { filter } from 'rxjs/operators';
import { ToastComponent } from './shared/components/toast/toast.component';
import { AuthService } from './core/services/auth.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, ToastComponent],
  template: `
    <app-toast></app-toast>
    <router-outlet></router-outlet>
  `
})
export class AppComponent implements OnInit {
  private authService = inject(AuthService);
  private router = inject(Router);

  ngOnInit(): void {
    // Attempt restoring session and validating current user on app start
    if (this.authService.isLoggedIn()) {
      this.authService.loadCurrentUser().subscribe();
    }

    // Move focus to main heading after navigation (WCAG 2.2 AA)
    this.router.events.pipe(
      filter((event): event is NavigationEnd => event instanceof NavigationEnd)
    ).subscribe(() => {
      if (typeof document !== 'undefined') {
        setTimeout(() => {
          const heading = (document.querySelector('main h1') || document.querySelector('h1')) as HTMLElement;
          if (heading) {
            heading.setAttribute('tabindex', '-1');
            heading.focus({ preventScroll: true });
          }
        }, 50);
      }
    });
  }
}
