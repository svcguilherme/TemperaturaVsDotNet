import { Component } from '@angular/core';
import { RouterOutlet, RouterLink, RouterLinkActive } from '@angular/router';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  template: `
    <nav class="navbar">
      <div class="nav-brand">
        <span class="brand-icon">🌡️</span>
        <span>TemperaturaVS</span>
      </div>
      <div class="nav-links">
        <a routerLink="/" routerLinkActive="active" [routerLinkActiveOptions]="{exact:true}">Home</a>
        <a routerLink="/weather" routerLinkActive="active">Clima</a>
        <a routerLink="/location" routerLinkActive="active">Localização</a>
        <a routerLink="/person" routerLinkActive="active">Pessoa</a>
        <a routerLink="/orders" routerLinkActive="active">Pedidos</a>
      </div>
    </nav>
    <main>
      <router-outlet />
    </main>
  `,
  styles: [`
    .navbar {
      display: flex;
      align-items: center;
      justify-content: space-between;
      padding: 0 24px;
      height: 60px;
      background: #1e293b;
      border-bottom: 1px solid #334155;
      position: sticky;
      top: 0;
      z-index: 100;
    }
    .nav-brand {
      display: flex;
      align-items: center;
      gap: 8px;
      font-size: 18px;
      font-weight: 700;
      color: #f1f5f9;
    }
    .brand-icon { font-size: 22px; }
    .nav-links {
      display: flex;
      gap: 4px;
    }
    .nav-links a {
      padding: 6px 14px;
      border-radius: 8px;
      font-size: 14px;
      font-weight: 500;
      color: #94a3b8;
      transition: all 0.15s;
    }
    .nav-links a:hover { background: #334155; color: #e2e8f0; }
    .nav-links a.active { background: #3b82f620; color: #60a5fa; }
  `]
})
export class AppComponent {}
