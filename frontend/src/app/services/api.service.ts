import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

// Portas deslocadas para não colidir com projeto Node.js/React (3000-3004)
const env = {
  weather:  (window as any).__ENV_WEATHER_URL  || 'http://localhost:5001',
  location: (window as any).__ENV_LOCATION_URL || 'http://localhost:5002',
  person:   (window as any).__ENV_PERSON_URL   || 'http://localhost:5003',
  orders:   (window as any).__ENV_ORDERS_URL   || 'http://localhost:5004'
};

@Injectable({ providedIn: 'root' })
export class ApiService {
  private http = inject(HttpClient);

  getWeather(city: string): Observable<any> {
    return this.http.get(`${env.weather}/weather?city=${encodeURIComponent(city)}`);
  }

  getForecast(city: string, days: number): Observable<any> {
    return this.http.get(`${env.weather}/forecast?city=${encodeURIComponent(city)}&days=${days}`);
  }

  getLocation(ip?: string): Observable<any> {
    const q = ip ? `?ip=${encodeURIComponent(ip)}` : '';
    return this.http.get(`${env.location}/location${q}`);
  }

  calculatePerson(name: string, birthdate: string): Observable<any> {
    return this.http.post(`${env.person}/person`, { name, birthdate });
  }

  getOrders(page: number, pageSize: number, status?: string): Observable<any> {
    let url = `${env.orders}/api/orders?page=${page}&pageSize=${pageSize}`;
    if (status) url += `&status=${status}`;
    return this.http.get(url);
  }

  getOrderStats(): Observable<any> {
    return this.http.get(`${env.orders}/api/orders/stats`);
  }

  processOrders(): Observable<any> {
    return this.http.post(`${env.orders}/api/orders/process`, {});
  }

  resetOrders(): Observable<any> {
    return this.http.post(`${env.orders}/api/orders/reset`, {});
  }
}
