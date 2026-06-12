type DetailItemProps = {
  label: string;
  value: string | number | boolean | null | undefined;
};

export function DetailItem({ label, value }: DetailItemProps) {
  return (
    <div className="detail-item">
      <span>{label}</span>
      <strong>{formatValue(value)}</strong>
    </div>
  );
}

function formatValue(value: string | number | boolean | null | undefined) {
  if (value === null || value === undefined || value === '') {
    return 'Not available';
  }

  if (typeof value === 'boolean') {
    return value ? 'Yes' : 'No';
  }

  return value;
}