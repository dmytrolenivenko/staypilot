// For tables whose whole row opens something (a listing, a valuation). A click that landed on
// one of the row's own controls - a link, a button, a checkbox - belongs to that control, and
// the row must not act on it as well.
export function clickedRowControl(event: MouseEvent): boolean {
  return (event.target as HTMLElement).closest('a, button, input, select, label') !== null;
}
