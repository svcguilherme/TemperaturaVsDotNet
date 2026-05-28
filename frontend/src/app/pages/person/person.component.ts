import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../services/api.service';

@Component({
  selector: 'app-person',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="page">
      <h1 class="page-title">👤 Calculadora de Idade</h1>
      <div class="card" style="margin-bottom:24px;">
        <div style="display:grid;grid-template-columns:1fr 1fr auto;gap:12px;align-items:flex-end;">
          <div>
            <label style="display:block;font-size:12px;color:#94a3b8;margin-bottom:6px;">NOME</label>
            <input [(ngModel)]="name" placeholder="João Silva" style="width:100%;" />
          </div>
          <div>
            <label style="display:block;font-size:12px;color:#94a3b8;margin-bottom:6px;">DATA DE NASCIMENTO</label>
            <input [(ngModel)]="birthdate" type="date" style="width:100%;" />
          </div>
          <button class="btn btn-primary" (click)="calculate()" [disabled]="loading">
            {{ loading ? 'Calculando...' : 'Calcular' }}
          </button>
        </div>
      </div>

      @if (result) {
        <div class="card">
          <h2 style="font-size:22px;margin-bottom:20px;">{{ result.name }}</h2>
          <div class="stat-grid">
            <div class="stat-card">
              <div class="stat-value" style="color:#60a5fa;">{{ result.ageYears }}</div>
              <div class="stat-label">Anos</div>
            </div>
            <div class="stat-card">
              <div class="stat-value" style="color:#a78bfa;">{{ result.ageMonths }}</div>
              <div class="stat-label">Meses</div>
            </div>
            <div class="stat-card">
              <div class="stat-value" style="color:#34d399;">{{ result.ageDays }}</div>
              <div class="stat-label">Dias</div>
            </div>
            <div class="stat-card">
              <div class="stat-value" style="font-size:16px;color:#fbbf24;">{{ result.zodiacSign }}</div>
              <div class="stat-label">Signo</div>
            </div>
          </div>
          <div style="color:#64748b;font-size:12px;margin-top:12px;">
            Nascimento: {{ result.birthdate }} · Calculado em: {{ result.timestamp | date:'medium' }}
          </div>
        </div>
      }
    </div>
  `
})
export class PersonComponent {
  private api = inject(ApiService);
  name = '';
  birthdate = '';
  loading = false;
  result: any = null;

  calculate() {
    if (!this.name || !this.birthdate) return;
    this.loading = true;
    this.api.calculatePerson(this.name, this.birthdate).subscribe({
      next: r => { this.result = r; this.loading = false; },
      error: () => { this.loading = false; }
    });
  }
}
