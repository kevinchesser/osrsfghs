import { Component, input, inject, WritableSignal, signal, computed} from '@angular/core';
import { HighScoreApiService } from '../../shared/services/apis/highscores.api';
import { CharacterOverview, XpDrop } from '../../shared/models/high-scores.models';
import { Skill } from '../../shared/enums/Skill';

@Component({
	selector: "osrsfghs-character",
	templateUrl: './character.component.html',
	styleUrl: './character.component.css'
})
export class CharacterComponent {
	protected readonly name = input.required<string>();
  private readonly highScoreApiService: HighScoreApiService = inject(HighScoreApiService);
  public characterOverview: WritableSignal<CharacterOverview | null> = signal<CharacterOverview | null>(null);
  public xpDropsBySkillMap = computed<Map<number, XpDrop[]>>(() => {
    const overview = this.characterOverview();
    if (!overview) {
      return new Map<number, XpDrop[]>();
    }

    return new Map(
      Object.entries(overview.xpDropsBySkill).map(([key, value]) => [+key, value])
    );
  });

	ngOnInit(){
    this.highScoreApiService.getCharacterOverview(this.name()).subscribe({
      next: (response: CharacterOverview) => {
        this.characterOverview.set(response);
      },
      error: (err) => {
        console.error("Error fetching character overview:", err)
      }
    })
	}

  getSkillIconPath(skillId: number) {
    const skillName = this.getSkillName(skillId)?.toLowerCase();
      return `/assets/skill_icon_${skillName}.gif`;
  }

  getSkillName(skillId: number) {
    return Skill[skillId];
  }
}
