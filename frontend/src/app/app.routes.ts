import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', loadComponent: () => import('./pages/home/home.component').then(m => m.HomeComponent) },
  { path: 'weather', loadComponent: () => import('./pages/weather/weather.component').then(m => m.WeatherComponent) },
  { path: 'location', loadComponent: () => import('./pages/location/location.component').then(m => m.LocationComponent) },
  { path: 'person', loadComponent: () => import('./pages/person/person.component').then(m => m.PersonComponent) },
  { path: 'orders', loadComponent: () => import('./pages/orders/orders.component').then(m => m.OrdersComponent) },
  { path: '**', redirectTo: '' }
];
