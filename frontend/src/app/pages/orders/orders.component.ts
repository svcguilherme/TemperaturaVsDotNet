import { Component, inject, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../services/api.service';

interface Order {
  id: number;
  orderNumber: number;
  totalValue: number;
  status: string;
  createdAt: string;
  processedAt: string | null;
  errorMessage: string | null;
}

@Component({
  selector: 'app-orders',
  standalone: true,
  imports: [CommonModule, FormsModule],
  styles: [`
    .orders-table-wrap {
      overflow-y: auto;
      max-height: 62vh;
      border-radius: 0 0 10px 10px;
    }
    table { width: 100%; border-collapse: collapse; font-size: 13px; }
    thead { position: sticky; top: 0; z-index: 2; }
    thead th {
      background: #0a1628;
      color: #64748b;
      text-align: left;
      padding: 10px 12px;
      font-weight: 600;
      font-size: 11px;
      text-transform: uppercase;
      letter-spacing: .06em;
      border-bottom: 1px solid #1e293b;
    }
    tbody td { padding: 8px 12px; border-bottom: 1px solid #0f172a55; vertical-align: middle; }
    tbody tr:hover { background: #1e293b66; }
    .num { font-family: monospace; font-weight: 700; color: #e2e8f0; }
    .val { color: #34d399; font-weight: 700; }
    .err { color: #f87171; font-size: 12px; font-style: italic; max-width: 220px; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
    .ts  { color: #475569; font-size: 11px; }
    .badge { display:inline-flex; align-items:center; padding:2px 9px; border-radius:9999px; font-size:11px; font-weight:700; letter-spacing:.05em; text-transform:uppercase; }
    .badge-pending    { background:#1e3a5f; color:#93c5fd; }
    .badge-processing { background:#3b1f00; color:#fbbf24; }
    .badge-completed  { background:#052e16; color:#86efac; }
    .badge-failed     { background:#3f0d0d; color:#fca5a5; }
    .stat-pill { display:inline-flex; align-items:center; gap:6px; padding:6px 14px; border-radius:9999px; font-size:13px; font-weight:600; }
    .pill-total      { background:#1e293b; color:#e2e8f0; }
    .pill-pending    { background:#1e3a5f; color:#93c5fd; }
    .pill-processing { background:#3b1f00; color:#fbbf24; }
    .pill-completed  { background:#052e16; color:#86efac; }
    .pill-failed     { background:#3f0d0d; color:#fca5a5; }
  `],
  template: `
    <div class="page">
      <h1 class="page-title">📦 Fila de Pedidos</h1>
      <p style="color:#94a3b8;margin-bottom:16px;font-size:13px;">
        <strong>Service Bus</strong> (RabbitMQ direct) + <strong>Event Hub</strong> (RabbitMQ fanout).
        Worker .NET processa com latência 10–200ms e 10% de falha aleatória.
      </p>

      <!-- Stats pills -->
      <div style="display:flex;flex-wrap:wrap;gap:8px;margin-bottom:16px;">
        <span class="stat-pill pill-total">Total: {{ stats?.total | number }}</span>
        <span class="stat-pill pill-pending">Pending: {{ getCount('Pending') | number }}</span>
        <span class="stat-pill pill-processing">Processing: {{ getCount('Processing') | number }}</span>
        <span class="stat-pill pill-completed">Completed: {{ getCount('Completed') | number }}</span>
        <span class="stat-pill pill-failed">Failed: {{ getCount('Failed') | number }}</span>
        @if (processingRate > 0) {
          <span class="stat-pill" style="background:#1e293b;color:#f97316;">
            ⚡ {{ processingRate | number:'1.0-0' }} pedidos/s
          </span>
        }
      </div>

      <!-- Controles -->
      <div class="card" style="margin-bottom:16px;padding:14px 20px;">
        <div style="display:flex;flex-wrap:wrap;gap:10px;align-items:center;">

          <button class="btn btn-success" (click)="startProcessing()" [disabled]="actionLoading">
            ▶ Processar Pendentes
          </button>
          <button class="btn btn-danger" (click)="reset()" [disabled]="actionLoading">
            ↺ Resetar para Pending
          </button>

          <div style="display:flex;gap:8px;align-items:center;margin-left:auto;">
            <label style="font-size:12px;color:#94a3b8;">Filtrar:</label>
            <select [(ngModel)]="filterStatus" (change)="applyFilter()" style="width:130px;">
              <option value="">Todos</option>
              <option value="Pending">Pending</option>
              <option value="Processing">Processing</option>
              <option value="Completed">Completed</option>
              <option value="Failed">Failed</option>
            </select>

            <label style="font-size:12px;color:#94a3b8;margin-left:6px;">Exibir:</label>
            <select [(ngModel)]="pageSize" (change)="applyFilter()" style="width:90px;">
              <option value="200">200</option>
              <option value="500">500</option>
              <option value="1000">1000</option>
              <option value="5000">5000</option>
              <option value="10000">Todos</option>
            </select>
          </div>
        </div>

        @if (actionMessage) {
          <div style="margin-top:10px;padding:8px 14px;background:#1e3a5f;border-radius:8px;font-size:13px;color:#93c5fd;">
            {{ actionMessage }}
          </div>
        }
      </div>

      <!-- Tabela -->
      <div class="card" style="padding:0;overflow:hidden;">
        <div style="display:flex;justify-content:space-between;align-items:center;padding:12px 16px;border-bottom:1px solid #1e293b;background:#0f172a;">
          <span style="font-size:13px;font-weight:600;color:#94a3b8;">
            {{ filtered.length | number }} pedidos exibidos
            @if (autoRefresh) { <span style="color:#22c55e;margin-left:8px;">● LIVE</span> }
          </span>
          <label style="display:flex;gap:6px;align-items:center;font-size:12px;cursor:pointer;color:#94a3b8;">
            <input type="checkbox" [(ngModel)]="autoRefresh" (change)="toggleAutoRefresh()" />
            Auto-refresh 2s
          </label>
        </div>

        <div class="orders-table-wrap">
          <table>
            <thead>
              <tr>
                <th>Nº Pedido</th>
                <th>Valor Total</th>
                <th>Status</th>
                <th>Motivo da Falha</th>
                <th>Criado em</th>
                <th>Processado em</th>
              </tr>
            </thead>
            <tbody>
              @for (o of filtered; track o.id) {
                <tr>
                  <td class="num">#{{ o.orderNumber }}</td>
                  <td class="val">R$&nbsp;{{ o.totalValue | number:'1.2-2' }}</td>
                  <td>
                    <span class="badge badge-{{ o.status.toLowerCase() }}">{{ o.status }}</span>
                  </td>
                  <td>
                    @if (o.errorMessage) {
                      <span class="err" [title]="o.errorMessage">{{ o.errorMessage }}</span>
                    } @else {
                      <span class="ts">—</span>
                    }
                  </td>
                  <td class="ts">{{ o.createdAt | date:'dd/MM HH:mm:ss' }}</td>
                  <td class="ts">{{ o.processedAt ? (o.processedAt | date:'dd/MM HH:mm:ss') : '—' }}</td>
                </tr>
              }
            </tbody>
          </table>
        </div>

        <div style="padding:8px 16px;border-top:1px solid #1e293b;font-size:11px;color:#475569;">
          {{ filtered.length | number }} de {{ allOrders.length | number }} pedidos carregados
          @if (filtered.length < (stats?.total || 0)) {
            — aumente "Exibir" para ver mais
          }
        </div>
      </div>
    </div>
  `
})
export class OrdersComponent implements OnInit, OnDestroy {
  private api = inject(ApiService);
  private pollInterval: any;
  private lastCompleted = 0;
  private lastPollTime = 0;

  allOrders: Order[] = [];
  filtered: Order[] = [];
  stats: any = null;
  filterStatus = '';
  pageSize = 500;
  actionLoading = false;
  actionMessage = '';
  autoRefresh = false;
  processingRate = 0;

  ngOnInit() { this.load(); }
  ngOnDestroy() { this.stopAutoRefresh(); }

  getCount(status: string): number {
    return this.stats?.byStatus?.find((s: any) => s.status === status)?.count ?? 0;
  }

  load() {
    this.api.getOrders(1, this.pageSize, this.filterStatus || undefined).subscribe({
      next: res => {
        this.allOrders = res.data;
        this.applyFilterLocal();
      }
    });
    this.loadStats();
  }

  loadStats() {
    const prevCompleted = this.getCount('Completed');
    const now = Date.now();
    this.api.getOrderStats().subscribe({
      next: s => {
        this.stats = s;
        const elapsed = (now - this.lastPollTime) / 1000;
        if (this.lastPollTime > 0 && elapsed > 0) {
          this.processingRate = Math.max(0, (this.getCount('Completed') - prevCompleted) / elapsed);
        }
        this.lastPollTime = now;
      }
    });
  }

  applyFilter() {
    this.api.getOrders(1, this.pageSize, this.filterStatus || undefined).subscribe({
      next: res => { this.allOrders = res.data; this.applyFilterLocal(); }
    });
  }

  applyFilterLocal() {
    this.filtered = this.filterStatus
      ? this.allOrders.filter(o => o.status === this.filterStatus)
      : this.allOrders;
  }

  startProcessing() {
    this.actionLoading = true;
    this.api.processOrders().subscribe({
      next: r => {
        this.actionMessage = r.message;
        this.actionLoading = false;
        if (!this.autoRefresh) { this.autoRefresh = true; this.toggleAutoRefresh(); }
      },
      error: () => { this.actionLoading = false; }
    });
  }

  reset() {
    this.actionLoading = true;
    this.api.resetOrders().subscribe({
      next: r => {
        this.actionMessage = r.message;
        this.actionLoading = false;
        this.processingRate = 0;
        this.stopAutoRefresh();
        this.load();
      },
      error: () => { this.actionLoading = false; }
    });
  }

  toggleAutoRefresh() {
    if (this.autoRefresh) {
      this.pollInterval = setInterval(() => { this.load(); }, 2000);
    } else {
      this.stopAutoRefresh();
    }
  }

  stopAutoRefresh() {
    clearInterval(this.pollInterval);
    this.autoRefresh = false;
  }
}
