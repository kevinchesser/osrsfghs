// character.component.ts
import { Component, input, inject, signal, computed, WritableSignal } from '@angular/core';
import { ChartConfiguration, ChartData } from 'chart.js';
import { ChartComponent } from '../../shared/components/chart/chart.component';
import { HighScoreApiService } from '../../shared/services/apis/highscores.api';
import { CharacterOverview, XpDrop } from '../../shared/models/high-scores.models';
import { Skill } from '../../shared/enums/Skill';
import { toLocalDate } from '../../shared/utils/date.utils';

@Component({
  selector: 'osrsfghs-character',
  templateUrl: './character.component.html',
  styleUrl: './character.component.css',
  imports: [ChartComponent],
})
export class CharacterComponent {
  protected readonly name = input.required<string>();
  private readonly highScoreApiService = inject(HighScoreApiService);
  public characterOverview: WritableSignal<CharacterOverview | null> = signal(null);

  // defaults to Overall (0) on load
  public selectedSkillId: WritableSignal<number> = signal(Skill.Overall);

  public xpDropsBySkillMap = computed<Map<number, XpDrop[]>>(() => {
    const overview = this.characterOverview();
    if (!overview) return new Map();
    return new Map(
      Object.entries(overview.xpDropsBySkill).map(([key, value]) => [+key, value])
    );
  });

  public selectedSkillDrops = computed<XpDrop[]>(() => {
    return this.xpDropsBySkillMap().get(this.selectedSkillId()) ?? [];
  });

  public chartData = computed<ChartData<'line', { x: Date; y: number }[]>>(() => {
    const sorted = [...this.selectedSkillDrops()].sort((a, b) =>
      new Date(a.timeStamp).getTime() - new Date(b.timeStamp).getTime()
    );

    return {
      datasets: [{
        label: this.getSkillName(this.selectedSkillId()),
        data: sorted.map(d => ({
          x: toLocalDate(d.timeStamp as unknown as string),
          y: d.xp,
        })),
        borderColor: 'rgb(99, 102, 241)',
        backgroundColor: 'rgba(99, 102, 241, 0.1)',
        tension: 0.2,
        fill: true,
      }],
    };
  });
/*
  public chartData = computed<ChartConfiguration['data']>(() => {
    const sorted = [...this.selectedSkillDrops()].sort((a, b) =>
      new Date(a.timeStamp).getTime() - new Date(b.timeStamp).getTime()
    );

    return {
      datasets: [{
        label: this.getSkillName(this.selectedSkillId()),
        data: sorted.map(d => ({
          x: toLocalDate(d.timeStamp as unknown as string),
          y: d.xp,
        })),
        borderColor: 'rgb(99, 102, 241)', // indigo-500
        backgroundColor: 'rgba(99, 102, 241, 0.1)',
        tension: 0.2,
        fill: true,
      }],
    };
  });
*/

  public chartOptions: ChartConfiguration['options'] = {
    responsive: true,
    maintainAspectRatio: false,
    scales: {
      x: {
        type: 'time',
        time: { unit: 'hour' },
        ticks: { color: '#9ca3af' },
        grid: { color: 'rgba(255,255,255,0.1)' },
      },
      y: {
        ticks: { color: '#9ca3af' },
        grid: { color: 'rgba(255,255,255,0.1)' },
      },
    },
    plugins: {
      legend: { display: false }, // skill name is shown in the header above the chart instead
    },
  };

  ngOnInit() {
    this.highScoreApiService.getCharacterOverview(this.name()).subscribe({
      next: (response) => this.characterOverview.set(response),
      error: (err) => console.error('Error fetching character overview:', err),
    });
  }

  selectSkill(skillId: number) {
    this.selectedSkillId.set(skillId);
  }

  getSkillIconPath(skillId: number) {
    return `/assets/skill_icon_${this.getSkillName(skillId)?.toLowerCase()}.gif`;
  }

  getSkillName(skillId: number) {
    return Skill[skillId];
  }
}
