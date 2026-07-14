import { Component } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { ComingSoonInfo } from '../../core/models/coming-soon-info';

@Component({
  selector: 'app-coming-soon',
  standalone: true,
  templateUrl: './coming-soon.component.html',
  styleUrl: './coming-soon.component.css'
})
export class ComingSoonComponent {
  info: ComingSoonInfo;

  constructor(route: ActivatedRoute) {
    this.info = route.snapshot.data['info'];
  }
}
