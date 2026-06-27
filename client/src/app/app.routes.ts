import { Routes } from '@angular/router';
import { HomeComponent } from './features/home/home.component';
import { CharacterComponent } from './features/character/character.component';
import { HighScoresComponent } from './features/highscores/highscores.component';

export const routes: Routes = [
	{
		path: '',
		component: HomeComponent,
		title: ''
	},
	{
		path: 'character/:name',
		component: CharacterComponent,
		title: ''
	},
	{
		path: 'highScores',
		component: HighScoresComponent,
		title: 'HighScores'
	}
];
