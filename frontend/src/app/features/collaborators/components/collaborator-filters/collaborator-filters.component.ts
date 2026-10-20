import { Component, inject, output } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { CollaboratorFilters } from '../../interfaces/collaborator-filters.interface';
import { CollaboratorType } from '../../interfaces/collaborator.interface';

type TypeOption = 'all' | 'volunteer' | 'employee';

/**
 * SCRUM-19/SCRUM-206: filtro por tipo del listado de colaboradores. Mismo patrón que
 * SocialRecordFiltersComponent (form con "Aplicar"/"Limpiar", en vez de aplicar en
 * cada tecla), pero con las 3 opciones como checkboxes en línea en vez de un select
 * — se comportan como mutuamente excluyentes (como un radio), tal como se pidió.
 */
@Component({
  selector: 'app-collaborator-filters',
  imports: [ReactiveFormsModule],
  templateUrl: './collaborator-filters.component.html',
})
export class CollaboratorFiltersComponent {
  private fb = inject(FormBuilder);

  filtersApplied = output<CollaboratorFilters>();
  filtersCleared = output<void>();

  form = this.fb.nonNullable.group({
    all: [true],
    volunteer: [false],
    employee: [false],
  });

  // tildar una opción destilda las otras dos, para que quede solo una marcada
  selectOption(option: TypeOption): void {
    this.form.setValue({
      all: option === 'all',
      volunteer: option === 'volunteer',
      employee: option === 'employee',
    });
  }

  applyFilters(): void {
    const value = this.form.getRawValue();
    const type = value.volunteer
      ? CollaboratorType.Volunteer
      : value.employee
        ? CollaboratorType.Employee
        : null;

    this.filtersApplied.emit({ type });
  }

  clearFilters(): void {
    this.form.setValue({ all: true, volunteer: false, employee: false });
    this.filtersCleared.emit();
  }
}
