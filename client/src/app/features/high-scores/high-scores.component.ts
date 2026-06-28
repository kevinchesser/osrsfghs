import { Component, inject, signal, computed, WritableSignal } from '@angular/core';
import { Router } from '@angular/router';
import { HighScoreApiService } from '../../shared/services/apis/highscores.api';
import { HighScoresViewModel } from '../../shared/models/high-scores.models';
import { Skill } from '../../shared/enums/Skill';

@Component({
  selector: 'osrsfghs-leaderboard',
  templateUrl: './high-scores.component.html',
  styleUrl: './high-scores.component.css',
})
export class HighScoresComponent {
  private readonly highScoreApiService = inject(HighScoreApiService);
  private readonly router = inject(Router);

  public highScoresViewModel: WritableSignal<HighScoresViewModel | null> = signal(null);
  public selectedSkillId: WritableSignal<number> = signal(Skill.Overall);

  // all enum values, in numeric order, for the left-hand skill list
  public skillIds = Object.values(Skill).filter((v): v is number => typeof v === 'number');

  public currentHighScores = computed(() => {
    const viewModel = this.highScoresViewModel();
    if (!viewModel) return [];
    return viewModel.highScoresBySkill[this.selectedSkillId()] ?? [];
  });

  ngOnInit() {
    this.highScoreApiService.getHighScoresBySkill().subscribe({
      next: (response) => this.highScoresViewModel.set(response),
      error: (err) => console.error('Error fetching high scores:', err),
    });
  }

  selectSkill(skillId: number) {
    this.selectedSkillId.set(skillId);
  }

  goToCharacter(characterName: string) {
    this.router.navigate(['/character', encodeURIComponent(characterName)]);
  }

  getSkillName(skillId: number): string {
    return Skill[skillId];
  }

  getSkillIconPath(skillId: number): string {
    return `/assets/skill_icon_${this.getSkillName(skillId)?.toLowerCase()}.gif`;
  }
}
