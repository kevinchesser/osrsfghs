import { Component, inject } from '@angular/core';
import { RouterLink, Router } from "@angular/router";

@Component({
	selector: "osrsfghs-nav-bar",
	templateUrl: './nav-bar.component.html',
	styleUrl: './nav-bar.component.css',
 	imports: [RouterLink]
})
export class NavBarComponent {
	private readonly router: Router = inject(Router);

	onSearch(searchValue: string) {
		const trimmed = searchValue.trim();
		if (!trimmed) {
			return; // don't navigate on empty input
		}

		this.router.navigate(['/character', encodeURIComponent(searchValue)]);
	}
}