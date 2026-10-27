import { summarizeAttendance, sortByStart, timeOf, lowStockItems } from './home-summary.util';

describe('home-summary.util', () => {
  it('summarizeAttendance suma personas y anónimos solo del día pedido', () => {
    const all = [
      { activity: 'a', date: '2026-10-26', persons: [{}, {}], anonymousCount: 1, justification: null },
      { activity: 'b', date: '2026-10-26', persons: [], anonymousCount: 0, justification: null },
      { activity: 'a', date: '2026-10-25', persons: [{}], anonymousCount: 0, justification: null },
    ] as never[];
    expect(summarizeAttendance(all, '2026-10-26')).toEqual({ total: 3, activities: 1 });
    expect(summarizeAttendance([], '2026-10-26')).toEqual({ total: 0, activities: 0 });
  });

  it('sortByStart ordena por inicio sin mutar', () => {
    const src = [{ start: '2026-10-26T15:00' }, { start: '2026-10-26T09:00' }];
    expect(sortByStart(src).map((x) => x.start)).toEqual(['2026-10-26T09:00', '2026-10-26T15:00']);
    expect(src[0].start).toBe('2026-10-26T15:00');
  });

  it('timeOf devuelve HH:mm o vacío', () => {
    expect(timeOf('2026-10-26T14:30:00')).toBe('14:30');
    expect(timeOf('2026-10-26')).toBe('');
  });

  it('lowStockItems deja los sin stock primero', () => {
    const r = lowStockItems([
      { id: '1', name: 'A', quantity: 3, lowStockThreshold: 5 },
      { id: '2', name: 'B', quantity: 0, lowStockThreshold: 5 },
      { id: '3', name: 'C', quantity: 50, lowStockThreshold: 5 },
    ]);
    expect(r.map((i) => i.id)).toEqual(['2', '1']);
  });
});
