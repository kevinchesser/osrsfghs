import { Component, inject, signal, computed, WritableSignal } from '@angular/core';
import { Router } from '@angular/router';
import { ChartData, ChartConfiguration } from 'chart.js';
import { ChartComponent } from '../../shared/components/chart/chart.component';
import { HighScoreApiService } from '../../shared/services/apis/highscores.api';
import { TimeSpanLeaderboard } from '../../shared/models/high-scores.models';

@Component({
  selector: 'osrsfghs-home',
  templateUrl: './home.component.html',
  styleUrl: './home.component.css',
  imports: [ChartComponent],
})
export class HomeComponent {
  private readonly highScoreApiService: HighScoreApiService = inject(HighScoreApiService);
  private readonly router: Router = inject(Router);

  public timeSpanLeaderBoard: WritableSignal<TimeSpanLeaderboard | null> = signal(null);

  public chartData = computed<ChartConfiguration['data']>(() => {
    const items = this.timeSpanLeaderBoard()?.timeSpanLeaderboardItems ?? [];

    // ascending by xp so the highest-xp character lands on the right
    const sorted = [...items].sort((a, b) => a.timeSpanOverallXp - b.timeSpanOverallXp);

    const data: ChartData<'bar', number[], string> = {
      labels: sorted.map(i => i.character.name),
      datasets: [{
        label: 'XP gained',
        data: sorted.map(i => i.timeSpanOverallXp),
        backgroundColor: 'rgba(99, 102, 241, 0.6)', // indigo-500
        borderColor: 'rgb(99, 102, 241)',
        borderWidth: 1,
      }],
    };

    return data as unknown as ChartConfiguration['data'];
  });

  public chartOptions: ChartConfiguration['options'] = {
    responsive: true,
    maintainAspectRatio: false,
    scales: {
      x: {
        ticks: { color: '#9ca3af' },
        grid: { display: false },
      },
      y: {
        ticks: {
          color: '#9ca3af',
          callback: (value) => Number(value).toLocaleString(),
        },
        grid: { color: 'rgba(255,255,255,0.1)' },
      },
    },
    plugins: {
      legend: { display: false },
    },
  };

  ngOnInit() {
    this.highScoreApiService.getLast24HourLeaderboard().subscribe({
      next: (response: TimeSpanLeaderboard) => {
        this.timeSpanLeaderBoard.set(response);
      },
      error: (err) => {
        console.error('Error fetching 24 hr leaderboard:', err);
      },
    });
  }

  goToCharacter(characterName: string) {
    this.router.navigate(['/character', encodeURIComponent(characterName)]);
  }
}
