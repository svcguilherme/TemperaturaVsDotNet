import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../services/api.service';

@Component({
  selector: 'app-weather',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="page">
      <h1 class="page-title">☁️ Consulta de Clima</h1>
      <div class="card" style="margin-bottom:24px;">
        <div style="display:flex;gap:12px;align-items:flex-end;">
          <div style="flex:1;">
            <label style="display:block;font-size:12px;color:#94a3b8;margin-bottom:6px;">CIDADE</label>
            <input [(ngModel)]="city" placeholder="São Paulo" style="width:100%;" (keyup.enter)="search()" />
          </div>
          <div>
            <label style="display:block;font-size:12px;color:#94a3b8;margin-bottom:6px;">DIAS PREVISÃO</label>
            <select [(ngModel)]="forecastDays" style="width:100px;">
              <option value="3">3 dias</option>
              <option value="5">5 dias</option>
              <option value="7">7 dias</option>
            </select>
          </div>
          <button class="btn btn-primary" (click)="search()" [disabled]="loading">
            {{ loading ? 'Buscando...' : 'Buscar' }}
          </button>
        </div>
      </div>

      @if (error) {
        <div style="background:#3f0d0d;border:1px solid #7f1d1d;border-radius:8px;padding:16px;color:#fca5a5;margin-bottom:16px;">
          {{ error }}
        </div>
      }

      @if (weather) {
        <div class="card" style="margin-bottom:16px;">
          <h2 style="font-size:20px;margin-bottom:16px;">{{ weather.city }}</h2>
          <div class="stat-grid">
            <div class="stat-card">
              <div class="stat-value" style="color:#60a5fa;">{{ weather.temperature | number:'1.1-1' }}°C</div>
              <div class="stat-label">Temperatura</div>
            </div>
            <div class="stat-card">
              <div class="stat-value" style="color:#a78bfa;">{{ weather.feelsLike | number:'1.1-1' }}°C</div>
              <div class="stat-label">Sensação</div>
            </div>
            <div class="stat-card">
              <div class="stat-value" style="color:#34d399;">{{ weather.humidity }}%</div>
              <div class="stat-label">Umidade</div>
            </div>
            <div class="stat-card">
              <div class="stat-value" style="color:#fbbf24;">{{ weather.windSpeed | number:'1.1-1' }}</div>
              <div class="stat-label">Vento (m/s)</div>
            </div>
          </div>
          <div style="color:#94a3b8;font-size:14px;margin-top:12px;text-transform:capitalize;">
            {{ weather.description }} · Fonte: {{ weather.source }}
          </div>
        </div>
      }

      @if (forecast) {
        <div class="card">
          <h3 style="font-size:16px;margin-bottom:16px;">Previsão {{ forecast.days }} dias — {{ forecast.city }}</h3>
          <div style="display:grid;grid-template-columns:repeat(auto-fill,minmax(140px,1fr));gap:12px;">
            @for (d of forecast.forecast; track d.date) {
              <div style="background:#0f172a;border-radius:8px;padding:14px;text-align:center;">
                <div style="font-size:12px;color:#94a3b8;margin-bottom:8px;">{{ d.date }}</div>
                <div style="font-size:20px;font-weight:700;color:#f97316;">{{ d.tempMax | number:'1.0-0' }}°</div>
                <div style="font-size:14px;color:#60a5fa;">{{ d.tempMin | number:'1.0-0' }}°</div>
                <div style="font-size:11px;color:#64748b;margin-top:6px;">{{ d.description }}</div>
              </div>
            }
          </div>
        </div>
      }
    </div>
  `
})
export class WeatherComponent {
  private api = inject(ApiService);
  city = 'São Paulo';
  forecastDays = 5;
  loading = false;
  error = '';
  weather: any = null;
  forecast: any = null;

  search() {
    if (!this.city.trim()) return;
    this.loading = true;
    this.error = '';

    this.api.getWeather(this.city).subscribe({
      next: w => { this.weather = w; },
      error: e => { this.error = 'Erro ao buscar clima: ' + e.message; }
    });

    this.api.getForecast(this.city, this.forecastDays).subscribe({
      next: f => { this.forecast = f; this.loading = false; },
      error: () => { this.loading = false; }
    });
  }
}
