import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../services/api.service';

@Component({
  selector: 'app-location',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="page">
      <h1 class="page-title">📍 Localização por IP</h1>
      <div class="card" style="margin-bottom:24px;">
        <div style="display:flex;gap:12px;align-items:flex-end;">
          <div style="flex:1;">
            <label style="display:block;font-size:12px;color:#94a3b8;margin-bottom:6px;">IP (deixe vazio para IP automático)</label>
            <input [(ngModel)]="ip" placeholder="8.8.8.8" style="width:100%;" (keyup.enter)="lookup()" />
          </div>
          <button class="btn btn-primary" (click)="lookup()" [disabled]="loading">
            {{ loading ? 'Buscando...' : 'Consultar' }}
          </button>
        </div>
      </div>

      @if (location) {
        <div class="card">
          <div class="stat-grid">
            <div class="stat-card">
              <div class="stat-value" style="font-size:16px;color:#60a5fa;">{{ location.city }}</div>
              <div class="stat-label">Cidade</div>
            </div>
            <div class="stat-card">
              <div class="stat-value" style="font-size:16px;color:#34d399;">{{ location.country }}</div>
              <div class="stat-label">País</div>
            </div>
            <div class="stat-card">
              <div class="stat-value" style="font-size:16px;color:#a78bfa;">{{ location.region }}</div>
              <div class="stat-label">Região</div>
            </div>
            <div class="stat-card">
              <div class="stat-value" style="font-size:14px;color:#fbbf24;">{{ location.timezone }}</div>
              <div class="stat-label">Timezone</div>
            </div>
            <div class="stat-card">
              <div class="stat-value" style="font-size:14px;color:#f97316;">{{ location.latitude | number:'1.4-4' }}</div>
              <div class="stat-label">Latitude</div>
            </div>
            <div class="stat-card">
              <div class="stat-value" style="font-size:14px;color:#f97316;">{{ location.longitude | number:'1.4-4' }}</div>
              <div class="stat-label">Longitude</div>
            </div>
          </div>
          <div style="color:#64748b;font-size:12px;margin-top:12px;">IP consultado: {{ location.ip }}</div>
        </div>
      }
    </div>
  `
})
export class LocationComponent implements OnInit {
  private api = inject(ApiService);
  ip = '';
  loading = false;
  location: any = null;

  ngOnInit() { this.lookup(); }

  lookup() {
    this.loading = true;
    this.api.getLocation(this.ip || undefined).subscribe({
      next: l => { this.location = l; this.loading = false; },
      error: () => { this.loading = false; }
    });
  }
}
