import { Directive } from '@angular/core';

/** Estilo único para input / select / textarea de todos los formularios. Uso: <input uiInput ... /> */
@Directive({
  selector: 'input[uiInput], select[uiInput], textarea[uiInput]',
  host: {
    class:
      'mt-1 w-full rounded-lg border border-[#D5DDF3] bg-white px-3 py-2 text-[13px] text-slate-700 outline-none transition focus:border-[#6B8DF5] focus:ring-1 focus:ring-[#6B8DF5]',
  },
})
export class UiInputDirective {}
