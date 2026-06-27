// chart.component.ts
import { Component, ElementRef, input, viewChild, AfterViewInit, OnDestroy, effect } from '@angular/core';
import { Chart, ChartConfiguration, ChartType, registerables } from 'chart.js';
import 'chartjs-adapter-date-fns';

Chart.register(...registerables);

@Component({
  selector: 'osrsfghs-chart',
  templateUrl: './chart.component.html',
  styleUrl: './chart.component.css'
})
export class ChartComponent implements AfterViewInit, OnDestroy {
  readonly canvasRef = viewChild.required<ElementRef<HTMLCanvasElement>>('canvas');

  type = input.required<ChartType>();
  data = input.required<ChartConfiguration['data']>();
  options = input<ChartConfiguration['options']>();

  private chart?: Chart;

  constructor() {
    effect(() => {
      const data = this.data();
      const options = this.options();
      if (this.chart) {
        this.chart.data = data;
        this.chart.options = options ?? {};
        this.chart.update();
      }
    });
  }

  ngAfterViewInit() {
    this.chart = new Chart(this.canvasRef().nativeElement, {
      type: this.type(),
      data: this.data(),
      options: this.options() ?? {},
    });
  }

  ngOnDestroy() {
    this.chart?.destroy();
  }
}
