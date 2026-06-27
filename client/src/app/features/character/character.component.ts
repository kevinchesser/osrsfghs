import { Component, input } from '@angular/core';

@Component({
	selector: "osrsfghs-character",
	templateUrl: './character.component.html',
	styleUrl: './character.component.css'
})
export class CharacterComponent {
	public name = input.required<string>();

	ngOnChanges() {
		// name is automatically populated from the :name route param
		console.log(this.name);
	}
}