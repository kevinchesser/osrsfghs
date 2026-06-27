import { Component, inject, WritableSignal, signal } from '@angular/core';
import { HighScoreApiService } from '../../shared/services/apis/highscores.api';
import { TimeSpanLeaderboard } from '../../shared/models/high-scores.models';

@Component({
	selector: "osrsfghs-home",
	templateUrl: './home.component.html',
	styleUrl: './home.component.css'
})
export class HomeComponent {
  private readonly highScoreApiService: HighScoreApiService = inject(HighScoreApiService);
  public timeSpanLeaderBoard: WritableSignal<TimeSpanLeaderboard | null> = signal<TimeSpanLeaderboard | null>(null);

	ngOnInit(){
    this.highScoreApiService.getLast24HourLeaderboard().subscribe({
      next: (response: TimeSpanLeaderboard) => {
        this.timeSpanLeaderBoard.set(response);
      },
      error: (err) => {
        console.error("Error fetching 24 hr leaderboard:", err)
      }
    })
	}
}
