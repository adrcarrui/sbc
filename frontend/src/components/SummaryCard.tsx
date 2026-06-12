type SummaryCardProps = {
  title: string;
  value: string | number | null | undefined;
  description?: string;
  variant?: 'default' | 'warning' | 'danger';
};

export function SummaryCard({
  title,
  value,
  description,
  variant = 'default',
}: SummaryCardProps) {
  const displayValue = value ?? 'N/A';

  return (
    <article className={`summary-card summary-card--${variant}`}>
      <span>{title}</span>

      <strong className={shouldUseCompactText(displayValue) ? 'summary-date' : undefined}>
        {displayValue}
      </strong>

      <p>{description ?? title}</p>
    </article>
  );
}

function shouldUseCompactText(value: string | number) {
  return typeof value === 'string' && value.length > 8;
}