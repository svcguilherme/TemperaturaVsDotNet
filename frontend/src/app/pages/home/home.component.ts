import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [RouterLink],
  template: `
    <div class="page">
      <h1 class="page-title">🌡️ TemperaturaVS — Plataforma de Microsserviços</h1>
      <p style="color:#94a3b8;margin-bottom:32px;">
        Plataforma distribuída com .NET 8, Angular 17, Kubernetes (Minikube), RabbitMQ, Redis, MongoDB e OpenTelemetry.
      </p>
      <div class="service-grid">
        @for (s of services; track s.path) {
          <a [routerLink]="s.path" class="service-card">
            <div class="service-icon">{{ s.icon }}</div>
            <div class="service-name">{{ s.name }}</div>
            <div class="service-desc">{{ s.desc }}</div>
            <div class="service-tech">{{ s.tech }}</div>
          </a>
        }
      </div>
      <div class="arch-note card" style="margin-top:32px;">
        <h3 style="margin-bottom:12px;font-size:16px;">Arquitetura</h3>
        <div class="arch-items">
          @for (item of arch; track item.label) {
            <div class="arch-item">
              <span class="arch-icon">{{ item.icon }}</span>
              <div>
                <div style="font-weight:600;font-size:14px;">{{ item.label }}</div>
                <div style="color:#94a3b8;font-size:12px;">{{ item.desc }}</div>
              </div>
            </div>
          }
        </div>
      </div>
    </div>
  `,
  styles: [`
    .service-grid { display:grid; grid-template-columns:repeat(auto-fill,minmax(220px,1fr)); gap:16px; }
    .service-card {
      background:#1e293b; border:1px solid #334155; border-radius:12px;
      padding:24px; cursor:pointer; transition:all 0.2s; display:block;
    }
    .service-card:hover { border-color:#3b82f6; transform:translateY(-2px); }
    .service-icon { font-size:32px; margin-bottom:12px; }
    .service-name { font-size:16px; font-weight:700; margin-bottom:6px; }
    .service-desc { font-size:13px; color:#94a3b8; margin-bottom:8px; }
    .service-tech { font-size:11px; color:#3b82f6; font-weight:600; }
    .arch-items { display:grid; grid-template-columns:repeat(auto-fill,minmax(200px,1fr)); gap:12px; }
    .arch-item { display:flex; gap:12px; align-items:flex-start; }
    .arch-icon { font-size:20px; flex-shrink:0; }
  `]
})
export class HomeComponent {
  services = [
    { path: '/weather', icon: '☁️', name: 'Clima', desc: 'Temperatura atual e previsão por cidade', tech: 'ASP.NET Core 8 · Redis · OpenWeatherMap' },
    { path: '/location', icon: '📍', name: 'Localização', desc: 'Geolocalização por IP do cliente', tech: 'ASP.NET Core 8 · Redis · ip-api.com' },
    { path: '/person', icon: '👤', name: 'Pessoa', desc: 'Calculadora de idade e signo zodiacal', tech: 'ASP.NET Core 8 · MongoDB' },
    { path: '/orders', icon: '📦', name: 'Pedidos', desc: 'Fila de pedidos com Service Bus + Event Hub', tech: 'ASP.NET Core 8 · SQLite · RabbitMQ' }
  ];

  arch = [
    { icon: '🐳', label: 'Kubernetes', desc: 'Minikube — orquestração local' },
    { icon: '🐇', label: 'RabbitMQ', desc: 'Service Bus + Event Hub local' },
    { icon: '📊', label: 'OpenTelemetry', desc: 'Traces distribuídos via OTLP' },
    { icon: '📈', label: 'Prometheus', desc: 'Métricas de performance' },
    { icon: '💾', label: 'Redis', desc: 'Cache L2 para clima/localização' },
    { icon: '🍃', label: 'MongoDB', desc: 'Persistência JSON das consultas' }
  ];
}
