import { formatReason } from '../utils/formatters';

type StatusBadgeProps = {
  value: string | null | undefined;
};

export function StatusBadge({ value }: StatusBadgeProps) {
  const safeValue = value?.trim() || 'Unknown';

  const normalizedValue = safeValue
    .replace(/([a-z])([A-Z])/g, '$1-$2')
    .replace(/_/g, '-')
    .toLowerCase();

  return (
    <span className={`status-badge status-badge--${normalizedValue}`}>
      {formatReason(safeValue)}
    </span>
  );
}