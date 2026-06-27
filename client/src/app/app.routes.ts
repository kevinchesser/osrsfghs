import { Routes } from '@angular/router';
import { HomeComponent } from './features/home/home.component';
import { CharacterComponent } from './features/character/character.component';
import { LeaderboardComponent } from './features/leaderboard/leaderboard.component';

export const routes: Routes = [
	{
		path: '',
		component: HomeComponent,
		title: ''
	},
	{
		path: '',
		component: CharacterComponent,
		title: ''
	},
	{
		path: '',
		component: LeaderboardComponent,
		title: ''
	}
];
