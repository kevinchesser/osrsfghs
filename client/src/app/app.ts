import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { NavBarComponent } from "./features/nav-bar/nav-bar.component";

@Component({
  selector: 'osrsfghs-root',
  imports: [RouterOutlet, NavBarComponent],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {
}
