import { Component, inject, signal, computed, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ApiService, WarehouseAnalysisDto, TransferSuggestionDto } from '@shared/api';

export interface WarehousePolicyItem {
  locationId: string;
  productId: string;
  locationCode: string;
  productName: string;
  sku: string;
  dailyConsumptionRate: number;
  currentMinStock: number;
  suggestedMinStock: number;
  currentMaxStock: number;
  suggestedMaxStock: number;
  leadTimeDays?: number;
  targetCoverageDays?: number;
}

@Component({
  selector: 'page-bodegas',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  template: `
    <div class="max-w-7xl mx-auto px-6 lg:px-8 py-8 space-y-8 bg-white font-sans text-slate-800">
      <!-- Encabezado Principal Estilo Stripe (Patrón 2) -->
      <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-4 pb-4 border-b border-slate-100">
        <div>
          <div class="flex items-center gap-3">
            <h1 class="text-2xl sm:text-3xl font-bold text-[#1A2C44] tracking-tight font-sans">
              Gestión de Bodegas & Rebalanceo
            </h1>
            <span class="text-xs font-semibold px-2.5 py-0.5 rounded-full bg-slate-100 text-slate-600 border border-slate-200/70">
              {{ totalStockUnits() }} unidades en red
            </span>
          </div>
          <p class="text-xs sm:text-sm text-[#3C4F69] mt-1">
            Supervisa el consumo mensual, autonomía de cobertura y traslados inteligentes entre bodegas para evitar desabastecimiento.
          </p>
        </div>

        <div class="flex items-center gap-3">
          <button
            type="button"
            (click)="reloadAll()"
            class="inline-flex items-center gap-1.5 text-xs font-medium px-3.5 py-2 rounded-md border border-[#E3E8EE] bg-white text-[#30313D] hover:bg-slate-50 hover:border-slate-300 transition-all shadow-2xs cursor-pointer">
            <svg class="w-3.5 h-3.5 text-slate-400" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
              <path stroke-linecap="round" stroke-linejoin="round" d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15" />
            </svg>
            <span>Actualizar</span>
          </button>

          <button
            type="button"
            (click)="generateSuggestions()"
            [disabled]="loadingAction()"
            class="inline-flex items-center gap-1.5 text-xs font-medium px-4 py-2 rounded-md bg-[#635BFF] text-white hover:bg-[#5248E0] transition-all shadow-xs disabled:opacity-50 cursor-pointer">
            <svg class="w-3.5 h-3.5" [ngClass]="{'animate-spin': loadingAction()}" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
              <path stroke-linecap="round" stroke-linejoin="round" d="M19.428 15.428a2 2 0 00-1.022-.547l-2.387-.477a6 6 0 00-3.86.517l-.318.158a6 6 0 01-3.86.517L6.05 15.21a2 2 0 00-1.806.547M8 4h8l-1 1v5.172a2 2 0 00.586 1.414l5 5c1.26 1.26.367 3.414-1.415 3.414H4.828c-1.782 0-2.674-2.154-1.414-3.414l5-5A2 2 0 009 10.172V5L8 4z" />
            </svg>
            <span>{{ loadingAction() ? 'Analizando...' : 'Analizar y Sugerir Traslados' }}</span>
          </button>
        </div>
      </div>

      <!-- Banner Feedback -->
      @if (feedback()) {
        <div 
          class="p-3.5 rounded-lg text-xs font-medium flex items-center justify-between transition-all shadow-2xs"
          [ngClass]="feedback()!.type === 'success' ? 'bg-emerald-50 text-emerald-800 border border-emerald-200' : 'bg-rose-50 text-rose-800 border border-rose-200'">
          <div class="flex items-center gap-2">
            <span class="font-bold">{{ feedback()!.type === 'success' ? '✓' : '⚠' }}</span>
            <span>{{ feedback()!.text }}</span>
          </div>
          <button (click)="feedback.set(null)" class="text-slate-400 hover:text-slate-600 font-bold ml-2 cursor-pointer" aria-label="Cerrar notificación">✕</button>
        </div>
      }

      <!-- Fila de Saldos y Resumen Operativo (Patrón 2) -->
      <div class="grid grid-cols-1 sm:grid-cols-3 gap-4">
        <!-- Tarjeta 1: Stock Total -->
        <div class="bg-white border border-[#E3E8EE] rounded-lg p-5 shadow-2xs flex flex-col justify-between">
          <div>
            <div class="flex items-center justify-between text-xs text-[#8792A2]">
              <span class="font-medium text-[#30313D] text-[13px]">Existencias en Red</span>
              <span title="Volumen total de insumos disponibles en todas las bodegas">&#9432;</span>
            </div>
            <div class="mt-2 text-2xl font-semibold text-[#1A2C44] tracking-tight font-sans">
              {{ totalStockUnits() }} <span class="text-xs font-normal text-[#3C4F69]">unidades</span>
            </div>
            <p class="text-xs text-[#3C4F69] mt-1">
              Distribuidas en {{ uniqueLocationsCount() }} bodegas operativas.
            </p>
          </div>
          <div class="mt-4 pt-3 border-t border-[#F1F3F5] flex items-center justify-between text-[11px]">
            <span class="text-slate-500">Estado global</span>
            <span class="font-medium text-emerald-700">Inventario disponible</span>
          </div>
        </div>

        <!-- Tarjeta 2: Traslados Sugeridos -->
        <div class="bg-white border border-[#E3E8EE] rounded-lg p-5 shadow-2xs flex flex-col justify-between">
          <div>
            <div class="flex items-center justify-between text-xs text-[#8792A2]">
              <span class="font-medium text-[#30313D] text-[13px]">Traslados Preventivos</span>
              <span title="Sugerencias calculadas para rebalancear bodegas con exceso hacia bodegas con déficit">&#9432;</span>
            </div>
            <div class="mt-2 text-2xl font-semibold text-[#1A2C44] tracking-tight font-sans">
              {{ pendingSuggestionsCount() }} <span class="text-xs font-normal text-[#3C4F69]">pendientes</span>
            </div>
            <p class="text-xs text-[#3C4F69] mt-1">
              Rebalanceo inteligente entre almacenes.
            </p>
          </div>
          <div class="mt-4 pt-3 border-t border-[#F1F3F5] flex items-center justify-between text-[11px]">
            <span class="text-slate-500">Acción sugerida</span>
            <button type="button" (click)="activeTab.set('rebalancing')" class="text-[#635BFF] hover:underline font-medium cursor-pointer">
              Ver sugerencias &rarr;
            </button>
          </div>
        </div>

        <!-- Tarjeta 3: Coberturas Críticas -->
        <div class="bg-white border border-[#E3E8EE] rounded-lg p-5 shadow-2xs flex flex-col justify-between">
          <div>
            <div class="flex items-center justify-between text-xs text-[#8792A2]">
              <span class="font-medium text-[#30313D] text-[13px]">Productos en Riesgo</span>
              <span title="Líneas con menos de 15 días de cobertura según su consumo mensual">&#9432;</span>
            </div>
            <div class="mt-2 text-2xl font-semibold tracking-tight font-sans" [ngClass]="criticalStockCount() > 0 ? 'text-rose-600' : 'text-[#1A2C44]'">
              {{ criticalStockCount() }} <span class="text-xs font-normal text-[#3C4F69]">críticos</span>
            </div>
            <p class="text-xs text-[#3C4F69] mt-1">
              Menos de 15 días de autonomía estimada.
            </p>
          </div>
          <div class="mt-4 pt-3 border-t border-[#F1F3F5] flex items-center justify-between text-[11px]">
            <span class="text-slate-500">Umbral mínimo</span>
            <span class="font-medium" [ngClass]="criticalStockCount() > 0 ? 'text-rose-700' : 'text-emerald-700'">
              {{ criticalStockCount() > 0 ? 'Requiere reabastecimiento' : 'Cobertura segura' }}
            </span>
          </div>
        </div>
      </div>

      <!-- Navegación de pestañas Estilo Stripe (Patrón 2) -->
      <div class="border-b border-[#E3E8EE] flex gap-8 text-[13px] font-medium">
        <button
          type="button"
          (click)="activeTab.set('analysis')"
          class="pb-3 border-b-2 transition-colors cursor-pointer"
          [ngClass]="activeTab() === 'analysis' ? 'border-[#635BFF] text-[#1A2C44] font-semibold' : 'border-transparent text-[#4F5B66] hover:text-[#1A2C44]'">
          Análisis de Consumo & Cobertura
        </button>
        <button
          type="button"
          (click)="activeTab.set('rebalancing')"
          class="pb-3 border-b-2 transition-colors relative cursor-pointer"
          [ngClass]="activeTab() === 'rebalancing' ? 'border-[#635BFF] text-[#1A2C44] font-semibold' : 'border-transparent text-[#4F5B66] hover:text-[#1A2C44]'">
          <span>Traslados Sugeridos</span>
          @if (pendingSuggestionsCount() > 0) {
            <span class="ml-1.5 px-1.5 py-0.5 rounded-full text-[10px] bg-amber-50 text-amber-700 border border-amber-200/60 font-semibold font-sans">
              {{ pendingSuggestionsCount() }}
            </span>
          }
        </button>
        <button
          type="button"
          (click)="activeTab.set('policies')"
          class="pb-3 border-b-2 transition-colors cursor-pointer"
          [ngClass]="activeTab() === 'policies' ? 'border-[#635BFF] text-[#1A2C44] font-semibold' : 'border-transparent text-[#4F5B66] hover:text-[#1A2C44]'">
          Políticas de Stock Mínimo/Máximo
        </button>
      </div>

      <!-- PESTAÑA 1: ANÁLISIS DE CONSUMO -->
      @if (activeTab() === 'analysis') {
        <div class="bg-white border border-[#E3E8EE] rounded-lg overflow-hidden shadow-2xs">
          <div class="px-5 py-3.5 border-b border-[#E3E8EE] bg-white flex items-center justify-between">
            <div>
              <h2 class="text-sm font-semibold text-[#1A2C44]">
                Inventario de Bodegas y Autonomía de Consumo
              </h2>
              <p class="text-xs text-[#3C4F69] mt-0.5">
                Cálculo de cobertura en días según el consumo mensual observado.
              </p>
            </div>
            <button
              type="button"
              (click)="loadAnalysis()"
              class="text-xs text-[#635BFF] hover:underline font-medium inline-flex items-center gap-1 cursor-pointer">
              <span>Actualizar datos</span>
              <span>&rarr;</span>
            </button>
          </div>

          <div class="overflow-x-auto">
            <table class="w-full text-left border-collapse text-[13px]">
              <thead class="bg-[#F8FAFC] border-b border-[#E3E8EE] text-[12px] font-medium uppercase tracking-wider text-[#697386]">
                <tr>
                  <th scope="col" class="py-3 px-4 font-medium text-[#697386]">Bodega</th>
                  <th scope="col" class="py-3 px-4 font-medium text-[#697386]">Producto / SKU</th>
                  <th scope="col" class="py-3 px-4 text-right font-medium text-[#697386]">Stock Disp.</th>
                  <th scope="col" class="py-3 px-4 text-right font-medium text-[#697386]">Consumo Mensual</th>
                  <th scope="col" class="py-3 px-4 text-right font-medium text-[#697386]">Días Cobertura</th>
                  <th scope="col" class="py-3 px-4 text-center font-medium text-[#697386]">Estado</th>
                  <th scope="col" class="py-3 px-4 text-right font-medium text-[#697386]">Mín / Máx</th>
                </tr>
              </thead>
              <tbody class="divide-y divide-[#F1F3F5]">
                @for (item of analysisList(); track item.locationId + item.productId) {
                  <tr class="hover:bg-[#F8FAFC]/80 transition-colors">
                    <td class="py-3.5 px-4 font-semibold text-[#1A2C44]">
                      {{ item.locationCode }}
                    </td>
                    <td class="py-3.5 px-4">
                      <span class="font-medium text-[#1A2C44] block leading-tight">{{ item.productName }}</span>
                      <span class="text-[#8792A2] block text-[11px] font-sans mt-0.5">{{ item.sku }}</span>
                    </td>
                    <td class="py-3.5 px-4 text-right font-semibold text-[#1A2C44] font-sans">
                      {{ item.availableQuantity }} u
                    </td>
                    <td class="py-3.5 px-4 text-right text-[#3C4F69] font-sans">
                      {{ item.monthlyConsumptionRate }} u/mes
                    </td>
                    <td class="py-3.5 px-4 text-right">
                      <span class="inline-flex items-center px-2 py-0.5 rounded text-[11px] font-medium font-sans border"
                        [ngClass]="{
                          'bg-rose-50 text-rose-700 border-rose-200/60': item.coverageDays < 15,
                          'bg-amber-50 text-amber-700 border-amber-200/60': item.coverageDays >= 15 && item.coverageDays <= 30,
                          'bg-emerald-50 text-emerald-700 border-emerald-200/60': item.coverageDays > 30 && item.coverageDays <= 90,
                          'bg-purple-50 text-purple-700 border-purple-200/60': item.coverageDays > 90
                        }">
                        {{ item.coverageDays > 900 ? 'Sin consumo' : item.coverageDays + ' días' }}
                      </span>
                    </td>
                    <td class="py-3.5 px-4 text-center">
                      <span class="inline-flex items-center px-2 py-0.5 rounded text-[11px] font-medium uppercase font-sans border"
                        [ngClass]="{
                          'bg-rose-50 text-rose-700 border-rose-200/60': item.status === 'Critico' || item.status === 'SinStock',
                          'bg-emerald-50 text-emerald-700 border-emerald-200/60': item.status === 'Normal',
                          'bg-purple-50 text-purple-700 border-purple-200/60': item.status === 'Exceso'
                        }">
                        {{ item.status }}
                      </span>
                    </td>
                    <td class="py-3.5 px-4 text-right text-[#697386] text-[12px] font-sans">
                      {{ item.minStock }} / {{ item.maxStock }}
                    </td>
                  </tr>
                } @empty {
                  <tr>
                    <td colspan="7" class="py-12 text-center text-[#8792A2]">
                      <p class="text-sm font-medium text-[#30313D]">No hay registros de inventario disponibles</p>
                      <p class="text-xs text-[#3C4F69] mt-1">Verifica los datos cargados en el módulo de Datos Maestros.</p>
                    </td>
                  </tr>
                }
              </tbody>
            </table>
          </div>
        </div>
      }

      <!-- PESTAÑA 2: REBALANCEO Y TRASLADOS SUGERIDOS -->
      @if (activeTab() === 'rebalancing') {
        <div class="space-y-4">
          @if (suggestionsList().length === 0) {
            <div class="bg-white border border-[#E3E8EE] rounded-lg p-10 text-center shadow-2xs space-y-4">
              <div class="w-12 h-12 rounded-full bg-slate-100 flex items-center justify-center mx-auto text-slate-400">
                <svg class="w-6 h-6" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="1.5">
                  <path stroke-linecap="round" stroke-linejoin="round" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
                </svg>
              </div>
              <div>
                <h3 class="text-sm font-semibold text-[#1A2C44]">No hay traslados pendientes de revisión</h3>
                @if (uniqueLocationsCount() <= 1) {
                  <p class="text-xs text-[#3C4F69] max-w-lg mx-auto mt-1 leading-relaxed">
                    La red opera actualmente con 1 sola bodega activa ({{ analysisList()[0]?.locationCode || 'BOD-SUR' }}). Los traslados de rebalanceo inteligente requieren al menos dos bodegas operativas para transferir excedentes hacia almacenes con déficit de cobertura.
                  </p>
                } @else {
                  <p class="text-xs text-[#3C4F69] max-w-lg mx-auto mt-1 leading-relaxed">
                    La red de bodegas se encuentra equilibrada y con cobertura segura. Puedes reevaluar en cualquier momento con el histórico de consumos.
                  </p>
                }
              </div>
              <div class="flex items-center justify-center gap-3 pt-2">
                <button
                  type="button"
                  (click)="activeTab.set('analysis')"
                  class="inline-flex items-center text-xs font-medium px-3.5 py-2 rounded-md border border-[#E3E8EE] bg-white text-[#30313D] hover:bg-slate-50 transition-colors shadow-2xs cursor-pointer">
                  &larr; Ver Inventario de Bodega
                </button>
                <button
                  type="button"
                  (click)="generateSuggestions()"
                  [disabled]="loadingAction()"
                  class="inline-flex items-center text-xs font-medium px-4 py-2 rounded-md bg-[#635BFF] text-white hover:bg-[#5248E0] transition-colors shadow-xs cursor-pointer disabled:opacity-50">
                  <svg class="w-3.5 h-3.5 mr-1.5" [ngClass]="{'animate-spin': loadingAction()}" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
                    <path stroke-linecap="round" stroke-linejoin="round" d="M19.428 15.428a2 2 0 00-1.022-.547l-2.387-.477a6 6 0 00-3.86.517l-.318.158a6 6 0 01-3.86.517L6.05 15.21a2 2 0 00-1.806.547M8 4h8l-1 1v5.172a2 2 0 00.586 1.414l5 5c1.26 1.26.367 3.414-1.415 3.414H4.828c-1.782 0-2.674-2.154-1.414-3.414l5-5A2 2 0 009 10.172V5L8 4z" />
                  </svg>
                  <span>{{ loadingAction() ? 'Analizando...' : 'Ejecutar Análisis Ahora' }}</span>
                </button>
              </div>
            </div>
          } @else {
            <div class="grid grid-cols-1 lg:grid-cols-2 gap-4">
              @for (sug of suggestionsList(); track sug.id) {
                <div class="bg-white border border-[#E3E8EE] rounded-lg p-5 shadow-2xs flex flex-col justify-between space-y-4 hover:border-slate-300 transition-all">
                  <div>
                    <div class="flex items-center justify-between border-b border-[#F1F3F5] pb-3">
                      <span class="font-semibold text-sm text-[#1A2C44]">{{ sug.productName }}</span>
                      <span class="text-[11px] font-sans px-2 py-0.5 rounded uppercase font-medium border"
                        [ngClass]="sug.status === 'Suggested' ? 'bg-amber-50 text-amber-700 border-amber-200/60' : 'bg-emerald-50 text-emerald-700 border-emerald-200/60'">
                        {{ sug.status === 'Suggested' ? 'Sugerencia Pendiente' : 'Aceptada' }}
                      </span>
                    </div>

                    <div class="mt-3 text-xs text-[#30313D] space-y-2">
                      <div class="flex items-center justify-between bg-[#F8FAFC] border border-[#F1F3F5] p-2.5 rounded-md text-xs">
                        <div class="flex items-center gap-1.5">
                          <span class="text-[#8792A2] text-[11px]">Origen:</span>
                          <span class="font-semibold text-[#1A2C44]">{{ sug.sourceLocationCode }}</span>
                        </div>
                        <span class="text-[#635BFF] font-bold">&rarr;</span>
                        <div class="flex items-center gap-1.5">
                          <span class="text-[#8792A2] text-[11px]">Destino:</span>
                          <span class="font-semibold text-[#1A2C44]">{{ sug.targetLocationCode }}</span>
                        </div>
                      </div>
                      <p class="text-[12px] text-[#3C4F69] mt-2 leading-relaxed">
                        {{ sug.rationale }}
                      </p>
                    </div>

                    <div class="mt-3 grid grid-cols-2 gap-2 text-center text-xs font-sans">
                      <div class="bg-purple-50/60 p-2.5 rounded-md border border-purple-100">
                        <span class="text-[10px] text-purple-700 block font-medium uppercase tracking-wider">Origen (Antes &rarr; Después)</span>
                        <span class="font-semibold text-purple-900 text-[12px] font-sans">{{ sug.sourceCoverageDaysBefore }}d &rarr; {{ sug.sourceCoverageDaysAfter }}d</span>
                      </div>
                      <div class="bg-emerald-50/60 p-2.5 rounded-md border border-emerald-100">
                        <span class="text-[10px] text-emerald-700 block font-medium uppercase tracking-wider">Destino (Antes &rarr; Después)</span>
                        <span class="font-semibold text-emerald-900 text-[12px] font-sans">{{ sug.targetCoverageDaysBefore }}d &rarr; {{ sug.targetCoverageDaysAfter }}d</span>
                      </div>
                    </div>
                  </div>

                  @if (sug.status === 'Suggested') {
                    <div class="pt-3 border-t border-[#F1F3F5] flex flex-wrap items-center justify-between gap-3">
                      <div class="flex items-center gap-1.5">
                        <label class="text-[11px] text-[#697386] font-medium">Mover:</label>
                        <input
                          type="number"
                          [(ngModel)]="sug.suggestedQuantity"
                          class="w-20 h-8 px-2 text-xs border border-[#E3E8EE] rounded-md font-sans font-semibold text-[#1A2C44] focus:border-[#635BFF] focus:outline-none" />
                        <span class="text-[11px] text-[#8792A2]">unidades</span>
                      </div>

                      <div class="flex items-center gap-2">
                        <button
                          type="button"
                          (click)="dismissSuggestion(sug.id)"
                          class="px-3 py-1.5 text-xs font-medium text-rose-600 hover:bg-rose-50 rounded-md border border-transparent hover:border-rose-100 transition-colors cursor-pointer">
                          Descartar
                        </button>
                        <button
                          type="button"
                          (click)="acceptSuggestion(sug.id, sug.suggestedQuantity)"
                          class="px-3.5 py-1.5 text-xs font-medium bg-emerald-600 hover:bg-emerald-700 text-white rounded-md shadow-2xs transition-colors cursor-pointer">
                          Aceptar y Crear Pedido
                        </button>
                      </div>
                    </div>
                  } @else {
                    <div class="pt-3 border-t border-[#F1F3F5] flex items-center justify-between text-xs">
                      <span class="text-emerald-700 font-medium inline-flex items-center gap-1">
                        <span>✓</span> Pedido generado en cola de rutas
                      </span>
                      <a routerLink="/rutas" class="text-[#635BFF] hover:underline font-medium inline-flex items-center gap-1">
                        <span>Ir a Planificar Ruta</span>
                        <span>&rarr;</span>
                      </a>
                    </div>
                  }
                </div>
              }
            </div>
          }
        </div>
      }

      <!-- PESTAÑA 3: POLÍTICAS DE STOCK -->
      @if (activeTab() === 'policies') {
        <div class="bg-white border border-[#E3E8EE] rounded-lg overflow-hidden shadow-2xs">
          <div class="px-5 py-3.5 border-b border-[#E3E8EE] bg-white flex flex-col sm:flex-row sm:items-center justify-between gap-2">
            <div>
              <h2 class="text-sm font-semibold text-[#1A2C44]">
                Configuración de Mínimos y Máximos de Inventario
              </h2>
              <p class="text-xs text-[#3C4F69] mt-0.5">
                Parámetros de stock de seguridad y capacidad máxima por almacén.
              </p>
            </div>
            <span class="text-[11px] text-[#8792A2] font-normal">Valores sugeridos según consumo histórico</span>
          </div>

          <div class="overflow-x-auto">
            <table class="w-full text-left border-collapse text-[13px]">
              <thead class="bg-[#F8FAFC] border-b border-[#E3E8EE] text-[12px] font-medium uppercase tracking-wider text-[#697386]">
                <tr>
                  <th scope="col" class="py-3 px-4 font-medium text-[#697386]">Bodega</th>
                  <th scope="col" class="py-3 px-4 font-medium text-[#697386]">Producto</th>
                  <th scope="col" class="py-3 px-4 text-right font-medium text-[#697386]">Consumo Diario</th>
                  <th scope="col" class="py-3 px-4 text-right font-medium text-[#697386]">Mínimo Actual</th>
                  <th scope="col" class="py-3 px-4 text-right font-medium text-[#697386]">Sugerido Mín.</th>
                  <th scope="col" class="py-3 px-4 text-right font-medium text-[#697386]">Máximo Actual</th>
                  <th scope="col" class="py-3 px-4 text-right font-medium text-[#697386]">Sugerido Máx.</th>
                  <th scope="col" class="py-3 px-4 text-center font-medium text-[#697386]">Acciones</th>
                </tr>
              </thead>
              <tbody class="divide-y divide-[#F1F3F5]">
                @for (pol of policiesList(); track pol.locationId + pol.productId) {
                  <tr class="hover:bg-[#F8FAFC]/80 transition-colors">
                    <td class="py-3.5 px-4 font-semibold text-[#1A2C44]">
                      {{ pol.locationCode }}
                    </td>
                    <td class="py-3.5 px-4">
                      <span class="font-medium text-[#1A2C44] block leading-tight">{{ pol.productName }}</span>
                      <span class="text-[#8792A2] block text-[11px] font-sans mt-0.5">{{ pol.sku }}</span>
                    </td>
                    <td class="py-3.5 px-4 text-right text-[#3C4F69] font-sans">{{ pol.dailyConsumptionRate }} u/d</td>
                    <td class="py-3.5 px-4 text-right">
                      <input
                        type="number"
                        [(ngModel)]="pol.currentMinStock"
                        class="w-18 h-7 px-1.5 text-right font-sans border border-[#E3E8EE] rounded text-xs text-[#1A2C44] focus:border-[#635BFF] focus:outline-none" />
                    </td>
                    <td class="py-3.5 px-4 text-right text-emerald-700 font-semibold font-sans bg-emerald-50/30">
                      {{ pol.suggestedMinStock }}
                    </td>
                    <td class="py-3.5 px-4 text-right">
                      <input
                        type="number"
                        [(ngModel)]="pol.currentMaxStock"
                        class="w-18 h-7 px-1.5 text-right font-sans border border-[#E3E8EE] rounded text-xs text-[#1A2C44] focus:border-[#635BFF] focus:outline-none" />
                    </td>
                    <td class="py-3.5 px-4 text-right text-purple-700 font-semibold font-sans bg-purple-50/30">
                      {{ pol.suggestedMaxStock }}
                    </td>
                    <td class="py-3.5 px-4 text-center space-x-1.5 whitespace-nowrap">
                      <button
                        type="button"
                        (click)="applySuggestionToRow(pol)"
                        class="px-2.5 py-1 text-[11px] bg-slate-100 hover:bg-slate-200 text-[#30313D] font-medium rounded transition-colors cursor-pointer">
                        Usar Sugerido
                      </button>
                      <button
                        type="button"
                        (click)="savePolicy(pol)"
                        class="px-2.5 py-1 text-[11px] bg-[#635BFF] hover:bg-[#5248E0] text-white font-medium rounded shadow-2xs transition-colors cursor-pointer">
                        Guardar
                      </button>
                    </td>
                  </tr>
                } @empty {
                  <tr>
                    <td colspan="8" class="py-12 text-center text-[#8792A2]">
                      <p class="text-sm font-medium text-[#30313D]">No hay políticas de stock configuradas</p>
                      <p class="text-xs text-[#3C4F69] mt-1">Se configurarán automáticamente al inicializar bodegas y productos.</p>
                    </td>
                  </tr>
                }
              </tbody>
            </table>
          </div>
        </div>
      }
    </div>
  `
})
export class BodegasPage implements OnInit {
  private readonly apiService = inject(ApiService);

  readonly activeTab = signal<'analysis' | 'rebalancing' | 'policies'>('analysis');
  readonly analysisList = signal<WarehouseAnalysisDto[]>([]);
  readonly suggestionsList = signal<TransferSuggestionDto[]>([]);
  readonly policiesList = signal<WarehousePolicyItem[]>([]);
  readonly loadingAction = signal(false);
  readonly feedback = signal<{ type: 'success' | 'error'; text: string } | null>(null);

  readonly pendingSuggestionsCount = signal(0);

  readonly totalStockUnits = computed(() =>
    this.analysisList().reduce((sum, item) => sum + (item.availableQuantity || 0), 0)
  );

  readonly uniqueLocationsCount = computed(() =>
    new Set(this.analysisList().map(i => i.locationCode)).size
  );

  readonly criticalStockCount = computed(() =>
    this.analysisList().filter(i => i.status === 'Critico' || i.status === 'SinStock').length
  );

  ngOnInit(): void {
    this.reloadAll();
  }

  reloadAll(): void {
    this.loadAnalysis();
    this.loadSuggestions();
    this.loadPolicies();
  }

  loadAnalysis(): void {
    this.apiService.getWarehouseAnalysis().subscribe({
      next: (res) => {
        this.analysisList.set(res || []);
      },
      error: (err) => {
        console.error('Error cargando análisis de bodegas:', err);
      }
    });
  }

  loadSuggestions(): void {
    this.apiService.getTransferSuggestions().subscribe({
      next: (res) => {
        const list = res || [];
        this.suggestionsList.set(list);
        this.pendingSuggestionsCount.set(list.filter(s => s.status === 'Suggested').length);
      },
      error: (err) => {
        console.error('Error cargando sugerencias de traslado:', err);
      }
    });
  }

  loadPolicies(): void {
    this.apiService.getWarehousePolicies().subscribe({
      next: (res) => {
        this.policiesList.set(res || []);
      },
      error: (err) => {
        console.error('Error cargando políticas de stock:', err);
      }
    });
  }

  generateSuggestions(): void {
    this.loadingAction.set(true);
    this.feedback.set(null);
    this.apiService.generateRebalancingSuggestions().subscribe({
      next: () => {
        this.loadingAction.set(false);
        this.loadAnalysis();
        this.apiService.getTransferSuggestions().subscribe({
          next: (res) => {
            const list = res || [];
            this.suggestionsList.set(list);
            const pending = list.filter(s => s.status === 'Suggested').length;
            this.pendingSuggestionsCount.set(pending);

            if (pending > 0) {
              this.feedback.set({
                type: 'success',
                text: `Análisis finalizado: Se ${pending === 1 ? 'generó 1 sugerencia' : `generaron ${pending} sugerencias`} de traslado entre bodegas.`
              });
              this.activeTab.set('rebalancing');
            } else {
              const locCount = this.uniqueLocationsCount();
              const firstLoc = this.analysisList()[0]?.locationCode || 'BOD-SUR';
              const text = locCount <= 1
                ? `Análisis finalizado: La red opera actualmente con 1 sola bodega activa (${firstLoc}). Se requieren al menos dos bodegas operativas para realizar traslados de rebalanceo.`
                : 'Análisis finalizado: La red de bodegas se encuentra equilibrada y con cobertura segura. No se requieren traslados preventivos en este momento.';

              this.feedback.set({
                type: 'success',
                text
              });
              // Conserva la pestaña actual (análisis) para no ocultar la tabla de inventario
            }
          },
          error: () => {
            this.loadSuggestions();
          }
        });
      },
      error: (err) => {
        this.loadingAction.set(false);
        this.feedback.set({
          type: 'error',
          text: err?.error?.message || 'Error al ejecutar el análisis de rebalanceo.'
        });
      }
    });
  }

  acceptSuggestion(id: string, qty: number): void {
    this.apiService.acceptTransferSuggestion(id, qty).subscribe({
      next: () => {
        this.feedback.set({
          type: 'success',
          text: 'Traslado aceptado exitosamente. Se ha generado un pedido en la cola de planificación de rutas.'
        });
        this.loadSuggestions();
      },
      error: (err) => {
        this.feedback.set({
          type: 'error',
          text: err?.error?.message || 'Error al aceptar sugerencia.'
        });
      }
    });
  }

  dismissSuggestion(id: string): void {
    this.apiService.dismissTransferSuggestion(id, 'Descartado manualmente').subscribe({
      next: () => {
        this.feedback.set({
          type: 'success',
          text: 'Sugerencia descartada.'
        });
        this.loadSuggestions();
      }
    });
  }

  applySuggestionToRow(pol: WarehousePolicyItem): void {
    pol.currentMinStock = pol.suggestedMinStock;
    pol.currentMaxStock = pol.suggestedMaxStock;
  }

  savePolicy(pol: WarehousePolicyItem): void {
    this.apiService.updateWarehousePolicy(pol.locationId, pol.productId, {
      minStock: pol.currentMinStock,
      maxStock: pol.currentMaxStock,
      leadTimeDays: pol.leadTimeDays ?? 7,
      targetCoverageDays: pol.targetCoverageDays ?? 15,
      isManual: true
    }).subscribe({
      next: () => {
        this.feedback.set({
          type: 'success',
          text: `Política guardada para ${pol.productName} en ${pol.locationCode}.`
        });
      },
      error: (err) => {
        this.feedback.set({
          type: 'error',
          text: err?.error?.message || 'Error al guardar política.'
        });
      }
    });
  }
}
