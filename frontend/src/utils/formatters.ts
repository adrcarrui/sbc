export function formatDateTime(value: string | null | undefined) {
  if (!value) {
    return 'Not available';
  }

  return new Intl.DateTimeFormat('es-ES', {
    dateStyle: 'short',
    timeStyle: 'medium',
  }).format(new Date(value));
}

export function formatReason(value: string | null | undefined) {
  if (!value) {
    return 'Unknown';
  }

  return value
    .replace(/([a-z])([A-Z])/g, '$1 $2')
    .replace(/([A-Z])/g, ' $1')
    .replace(/_/g, ' ')
    .replace(/^./, (firstLetter) => firstLetter.toUpperCase())
    .trim();
}